using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARC
{
    [HelpURL("https://www.example.com")]
    [AddComponentMenu("ARC/ARC Setup Manager")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CaptainsMess))]
    public class ARCSetupManager : MonoBehaviour
    {
        public static ARCSetupManager manager { get; private set; }

        [SerializeField] int _minimumCount = 2;
        [SerializeField] float _scalingFactor = 1f;
        [SerializeField] float _scanningTime = 2f;
        [SerializeField] bool _autocreateSession = true;
        [SerializeField] float _autocreateDelayTime = 1f;
        [SerializeField] ARPlaneManager _planeManager;
        [SerializeField] ARRaycastManager _raycastManager;
        [SerializeField] ARTrackedImageManager _trackedImageManager;
        [SerializeField] GameObject _scanningPrefab;
        [SerializeField] GameObject _markerPendingPrefab;
        [SerializeField] GameObject _markerDonePrefab;
        [SerializeField] GameObject _staticScenePrefab;
        public ARCCapturePhaseEvents captureEvents;
        public ARCReceivingPhaseEvents networkEvents;
        public ARCScanningPhaseEvents scanEvents;
        public ARCSessionEvents sessionEvents;

        CaptainsMess _captainsMess;
        Camera _cam;
        List<Texture2D> _capturedImages;
        ARCRuntimeLibraryBuilder _runtimeLibrary;
        Dictionary<string, GameObject> _gizmosScanning;
        SortedList<string, GameObject> _gizmosScanned;
        List<string> _scannedImages;
        SortedList<string, float> _scanningTimes;
        GameObject _staticScene;

        void Awake()
        {
            manager = this;
            _planeManager.enabled = false;
            _raycastManager.enabled = false;
            _trackedImageManager.enabled = false;
        }

        void OnEnable()
        {
            if (_trackedImageManager) { _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged; }
        }

        void OnDisable()
        {
            if (_trackedImageManager) { _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged; }
        }

        void OnValidate()
        {
            _minimumCount = (int) Mathf.Max(_minimumCount, 2f);
            _scalingFactor = Mathf.Clamp01(_scalingFactor);
            _scanningTime = Mathf.Clamp(_scanningTime, 0f, float.PositiveInfinity);
            _autocreateDelayTime = Mathf.Clamp(_autocreateDelayTime, 0f, float.PositiveInfinity);
        }

        void Start()
        {
            _cam = Camera.main;
            _captainsMess = GetComponent<CaptainsMess>();

            _capturedImages = new List<Texture2D>();
            _runtimeLibrary = new ARCRuntimeLibraryBuilder();
            _gizmosScanned = new SortedList<string, GameObject>();

            _scannedImages = new List<string>();
            _scanningTimes = new SortedList<string, float>();
            _gizmosScanning = new Dictionary<string, GameObject>();
        }

        public void CaptureImage()
        {
            // Store current render texture
            RenderTexture activeTexture = RenderTexture.active;

            // Copy camera frame to our render texture
            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            ARCameraBackground arCameraBackground = FindObjectOfType<ARCameraBackground>();
            try
            {
                Graphics.Blit(null, renderTexture, arCameraBackground.material);
            }

            catch
            {
                Debug.LogError("No camera input available.");
                return;
            }

            // Crop centred square area from camera frame to our texture
            RenderTexture.active = renderTexture;
            Texture2D capture = new Texture2D(renderTexture.width, renderTexture.width);
            Rect cropRegion = new Rect(0, renderTexture.height/2 - renderTexture.width/2, renderTexture.width, renderTexture.width);
            capture.ReadPixels(cropRegion, 0, 0);
            capture.Apply();

            // Restore previous render texture
            RenderTexture.active = activeTexture;
            Destroy(renderTexture);

            // Resize our texture and save it
            int scaledSize = Mathf.RoundToInt(renderTexture.width * _scalingFactor);
            TextureScale.Bilinear(capture, scaledSize, scaledSize);
            _capturedImages.Add(capture);

            // Trigger event
            captureEvents.imageCaptured.Invoke(capture);
        }

        public void ClearCapturedImages()
        {
            foreach (Texture2D image in _capturedImages) { Destroy(image); }
            _capturedImages.Clear();
        }

        public void RetryScanning()
        {
            _trackedImageManager.enabled = true;
            _planeManager.enabled = true;
            _raycastManager.enabled = true;

            foreach (GameObject go in _gizmosScanned.Values) Destroy(go);
            foreach (GameObject go in _gizmosScanning.Values) Destroy(go);
            _gizmosScanned.Clear();
            _gizmosScanning.Clear();
            _scannedImages.Clear();
            _scanningTimes.Clear();
        }

        public void CancelScanning()
        {
            _trackedImageManager.enabled = false;
            _planeManager.enabled = false;
            _raycastManager.enabled = false;
            _trackedImageManager.referenceLibrary = null;

            foreach (GameObject go in _gizmosScanned.Values) Destroy(go);
            foreach (GameObject go in _gizmosScanning.Values) Destroy(go);
            _gizmosScanned.Clear();
            _gizmosScanning.Clear();
            _scannedImages.Clear();
            _scanningTimes.Clear();
            _runtimeLibrary.ClearReferenceImages();
            ClearCapturedImages();
        }

        public void SubmitCapturedImages()
        {
            if (_capturedImages.Count < _minimumCount) { return; }
            
            // Send encoded images for session sync
            List<ARCImageContainer> encodedMarkers = new List<ARCImageContainer>();
            foreach (Texture2D image in _capturedImages)
            {
                encodedMarkers.Add(new ARCImageContainer(image));
            }
            ARCSession.session.SetSessionMarkers(encodedMarkers);
            captureEvents.captureCompleted.Invoke();

            // Autosend local (host) markers
            networkEvents.manifestReceived.Invoke(new int[_capturedImages.Count]);
            for (int i = 0; i < _capturedImages.Count; ++i)
            {
                AddMarker(i, _capturedImages[i]);
            }
            FinalizeMarkers();
            _capturedImages.Clear();
        }

        public void AddMarker(int index, Texture2D marker)
        {
            marker.name = $"Marker{index}";
            _runtimeLibrary.AddReferenceImage(marker);
            networkEvents.markerReceived.Invoke(index, marker);
            Debug.Log($"Marker added: {marker.name}");
        }

        public void FinalizeMarkers()
        {
            if (_runtimeLibrary.referenceImages.Count < _minimumCount) { return; }

            try
            {
                if (_runtimeLibrary.BuildLibrary(_trackedImageManager))
                {
                    // Library created, now tracking images
                    _trackedImageManager.enabled = true;
                    _planeManager.enabled = true;
                    _planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                    _raycastManager.enabled = true;
                    networkEvents.receivingCompleted.Invoke();
                    Debug.Log("Markers finalized");
                }
            }
            
            catch
            {
                // This code is executed if no AR subsystems work,
                // e.g. when the client is running on a desktop/laptop,
                // and is intended for testing purposes only.
                networkEvents.receivingCompleted.Invoke();
                Debug.Log("Markers finalized (view only device)");
                
                scanEvents.scanningComplete.Invoke();

                CreateAnchor(Vector3.zero, Quaternion.identity);

                if (_staticScenePrefab != null)
                {
                    _staticScene = Instantiate(_staticScenePrefab);
                    _staticScene.transform.SetParent(ARCSession.origin);
                    _staticScene.transform.localPosition = Vector3.zero;
                    _staticScene.transform.localRotation = Quaternion.identity;
                }
                
                sessionEvents.sessionAnchorCreated.Invoke();

                ((ARCUser) _captainsMess.LocalPlayer()).SendReadyToBeginMessage();
            }
        }

        void RegisterMarkerPosition(Vector3 position, string name)
        {
            GameObject newAnchor;
            if (_markerPendingPrefab == null)
            {
                newAnchor = new GameObject();
                newAnchor.transform.position = position;
            }
            else
                newAnchor = Instantiate(_markerPendingPrefab, position, Quaternion.identity);
            
            _gizmosScanned.Add(name, newAnchor);
            
            int markerIndex = Convert.ToInt32(name.Substring(name.Length - 1));
            scanEvents.markerScanned.Invoke(markerIndex);
        }

        void CreateSessionAnchor()
        {
            Vector3 centre = Vector3.zero;
            List<Vector3> gizmosFinalisedPositions = new List<Vector3>();
            foreach (GameObject go in _gizmosScanned.Values)
            {
                centre += go.transform.position;
                gizmosFinalisedPositions.Add(go.transform.position);
            }
            centre /= _gizmosScanned.Values.Count;

            Vector3 origin = _gizmosScanned.Values[0].transform.position;
            Vector3 lookAt = centre - origin;
            lookAt.y = 0f;
            Quaternion orientation = Quaternion.LookRotation(lookAt);
            
            foreach (Vector3 v in gizmosFinalisedPositions)
            {
                GameObject gizmoGo;
                if (_markerDonePrefab == null)
                {
                    gizmoGo = new GameObject();
                    gizmoGo.transform.position = v;
                }
                else
                    gizmoGo = Instantiate(_markerDonePrefab, v, Quaternion.identity);
            }

            for (int i = 0; i < _gizmosScanned.Values.Count; ++i) { Destroy(_gizmosScanned.Values[i]); }
            _gizmosScanned.Clear();

            CreateAnchor(origin, orientation);

            if (_staticScenePrefab != null)
            {
                _staticScene = Instantiate(_staticScenePrefab);
                _staticScene.transform.SetParent(ARCSession.origin);
                _staticScene.transform.localPosition = Vector3.zero;
                _staticScene.transform.localRotation = Quaternion.identity;
            }

            sessionEvents.sessionAnchorCreated.Invoke();
        }

        public void ClearSession()
        {
            GameObject currentAnchor = GameObject.Find("ARCSessionAnchor");
            if (currentAnchor != null)
                Destroy(currentAnchor);
        }

        void CreateAnchor(Vector3 origin, Quaternion orientation)
        {
            // The session anchor is controlled by the AR Anchor component
            Transform sessionAnchor = new GameObject("ARCSessionAnchor").transform;
            sessionAnchor.position = origin;
            sessionAnchor.rotation = orientation;
            sessionAnchor.gameObject.AddComponent<ARAnchor>();
            
            // The session space is parented to the anchor object, so that
            // it updates with it but can also be moved and rotated locally
            // via the ARCSession.SetSessionSpacePose() function
            Transform sessionSpace = new GameObject("ARCSessionSpace").transform;
            sessionSpace.SetParent(sessionAnchor);
            sessionSpace.localPosition = Vector3.zero;
            sessionSpace.localRotation = Quaternion.identity;

            ARCSession.origin = sessionSpace;
        }

        void HaltScanning()
        {
            _trackedImageManager.enabled = false;
            _planeManager.enabled = false;
            _raycastManager.enabled = false;

            if (_autocreateSession)
                Invoke("CreateSessionAnchor", _autocreateDelayTime);
        }

        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            ARTrackedImage trackedImage = null;
            List<ARRaycastHit> hits = new List<ARRaycastHit>();

            for (int i = 0; i < eventArgs.added.Count; i++)
            {
                // instantiate AR object, set trackedImage.transform
                // use a Dictionary, the key could be the trackedImage, or the name of the reference image -> trackedImage.referenceImage.name
                // the value of the Dictionary is the AR object you instantiate.
                if (_scannedImages.Contains(eventArgs.added[i].referenceImage.name)) { continue; }
                trackedImage = eventArgs.added[i];
                
                Vector2 screenPosition = _cam.WorldToScreenPoint(trackedImage.transform.position);
                if (_raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    GameObject instance;
                    if (_scanningPrefab == null)
                    {
                        instance = new GameObject();
                        instance.transform.position = hits[0].pose.position;
                    }
                    else
                        instance = Instantiate(_scanningPrefab, hits[0].pose.position, Quaternion.identity);
                    instance.name = trackedImage.referenceImage.name;
                    _gizmosScanning.Add(trackedImage.referenceImage.name, instance);
                    _scanningTimes.Add(trackedImage.referenceImage.name, 0f);
                }
            }

            for (int i = 0; i < eventArgs.updated.Count; i++)
            {
                // set AR object to active, use Dictionary to get AR object based on trackedImage
                if (_scannedImages.Contains(eventArgs.updated[i].referenceImage.name)) { continue; }
                trackedImage = eventArgs.updated[i];
                
                if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    Vector2 screenPosition = _cam.WorldToScreenPoint(trackedImage.transform.position);
                    if (_raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
                    {
                        if (!_gizmosScanning.ContainsKey(trackedImage.referenceImage.name))
                        {
                            GameObject instance = Instantiate(_scanningPrefab, hits[0].pose.position, Quaternion.identity);
                            instance.name = trackedImage.referenceImage.name;
                            _gizmosScanning.Add(trackedImage.referenceImage.name, instance);
                            _scanningTimes.Add(trackedImage.referenceImage.name, 0f);
                        }
                        _gizmosScanning[trackedImage.referenceImage.name].transform.position = hits[0].pose.position;
                        _scanningTimes[trackedImage.referenceImage.name] += Time.unscaledDeltaTime;
                        
                        if (_scanningTimes[trackedImage.referenceImage.name] >= _scanningTime)
                        {
                            // Marker scanned successfully
                            RegisterMarkerPosition(hits[0].pose.position, trackedImage.referenceImage.name);

                            _scannedImages.Add(trackedImage.referenceImage.name);
                            Destroy(_gizmosScanning[trackedImage.referenceImage.name]);
                            _gizmosScanning.Remove(trackedImage.referenceImage.name);
                            _scanningTimes.Remove(trackedImage.referenceImage.name);

                            _trackedImageManager.requestedMaxNumberOfMovingImages = _trackedImageManager.requestedMaxNumberOfMovingImages - 1;
                            if (_scannedImages.Count == _runtimeLibrary.referenceImages.Count)
                            {
                                HaltScanning();
                                scanEvents.scanningComplete.Invoke();
                            }
                        }
                        continue;
                    }
                }

                if (_gizmosScanning.ContainsKey(trackedImage.referenceImage.name))
                {
                    Destroy(_gizmosScanning[trackedImage.referenceImage.name]);
                    _gizmosScanning.Remove(trackedImage.referenceImage.name);
                    _scanningTimes.Remove(trackedImage.referenceImage.name);
                }
            }

            for (int i = 0; i < eventArgs.removed.Count; i++)
            {
                // destroy AR object, or set active to false. Use Dictionary.
                if (_scannedImages.Contains(eventArgs.removed[i].referenceImage.name)) { continue; }
                trackedImage = eventArgs.removed[i];

                if (_gizmosScanning.ContainsKey(trackedImage.referenceImage.name))
                {
                    Destroy(_gizmosScanning[trackedImage.referenceImage.name]);
                    _gizmosScanning.Remove(trackedImage.referenceImage.name);
                    _scanningTimes.Remove(trackedImage.referenceImage.name);
                }
            }
        }
    }
}
