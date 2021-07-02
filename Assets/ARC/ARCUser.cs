using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

namespace ARC
{
    [HelpURL("https://www.example.com")]
    [AddComponentMenu("ARC/ARC User")]
    [DisallowMultipleComponent]
    public class ARCUser : CaptainsMessPlayer
    {
        [SyncVar] public string username;
        [Range(0, 20)]
        [SerializeField] int _updateRate = 20;
        [SerializeField] float _positionThreshold = 0.1f;
        [SerializeField] float _rotationThreshold = 10f;
        [SerializeField] UnityEvent _clientEnterLobby;
        [SerializeField] UnityEvent _clientExitLobby;
        [SerializeField] ARCBoolEvent _clientReady;
        [SerializeField] UnityEvent _gameStarted;
        [SerializeField] UnityEvent _grabEvent;
        [SerializeField] UnityEvent _releaseEvent;

        public Vector3 localCameraPosition
        {
            get { return ARCSession.origin.InverseTransformPoint(_cam.transform.position); }
        }
        public Quaternion localCameraRotation
        {
            get { return Quaternion.Inverse(ARCSession.origin.rotation) * _cam.transform.rotation; }
        }
        public Vector3 localCameraForward
        {
            get { return ARCSession.origin.InverseTransformDirection(_cam.transform.forward); }
        }
        public ARCEntity interactingEntity { get; private set; }

        float _updateInterval;
        double _relativeTick;
        [SyncVar] Vector3 _position;
        [SyncVar] Quaternion _rotation;
        float _sqrPositionThreshold;
        Camera _cam;
        float _arScale;

        void Start()
        {
            _arScale = FindObjectOfType<ARSessionOrigin>().transform.localScale.x;

            if (isLocalPlayer)
            {
                _cam = Camera.main;
                CmdPollMarkers();
            }

            if (ARCSession.origin == null)
                ARCSetupManager.manager.sessionEvents.sessionAnchorCreated.AddListener(OnSessionAnchorCreated);
            else
                OnSessionAnchorCreated();
            
            _position = Vector3.zero;
            _rotation = Quaternion.identity;
        }

        void Update()
        {
            if (_updateInterval > 0)
                UpdatePose();
        }

        // Parent self to shared session origin, start coroutine for local user
        void OnSessionAnchorCreated()
        {
            transform.SetParent(ARCSession.origin);

            if (_updateRate > 0)
            {
                _updateInterval = 1f / _updateRate;
                _sqrPositionThreshold = _positionThreshold * _positionThreshold;
                StartCoroutine(PoseCoroutine(_updateRate <= 5));
            }
        }

        // Apply remote position and rotation with smoothing
        void UpdatePose()
        {
            Vector3 syncLocalPosition = _position;
            Quaternion syncLocalRotation = _rotation;

            float lerpPos = Mathf.Clamp01( Vector3.Distance(transform.localPosition, syncLocalPosition) / _arScale );
            float lerpRot = Mathf.Clamp01( Quaternion.Angle(transform.localRotation, syncLocalRotation) / 180f );
            if (lerpPos < 0.001f) lerpPos = 0f;
            if (lerpRot < 0.001f) lerpRot = 0f;

            transform.localPosition = Vector3.Lerp(transform.localPosition, syncLocalPosition, lerpPos);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, syncLocalRotation, lerpRot);
        }

        // Send position and rotation updates when appropriate
        IEnumerator PoseCoroutine(bool reliable)
        {
            float sqrPositionDelta;
            float rotationDelta;

            while (isLocalPlayer)
            {
                sqrPositionDelta = (localCameraPosition - _position).sqrMagnitude;
                rotationDelta = Quaternion.Angle(localCameraRotation, _rotation);

                if ((sqrPositionDelta > _sqrPositionThreshold) || (rotationDelta > _rotationThreshold))
                {
                    if (reliable)
                        CmdSubmitPose(++_relativeTick, localCameraPosition, localCameraRotation);
                    else
                        CmdSubmitPoseUnreliable(++_relativeTick, localCameraPosition, localCameraRotation);
                    yield return new WaitForSeconds(_updateInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        // CaptainsMess callback
        public override void OnClientEnterLobby()
        {
            base.OnClientEnterLobby();
            _clientEnterLobby.Invoke();
        }

        // CaptainsMess callback
        public override void OnClientReady(bool readyState)
        {
            base.OnClientReady(readyState);
            _clientReady.Invoke(readyState);
        }

        // CaptainsMess callback
        public override void OnClientExitLobby()
        {
            base.OnClientExitLobby();
            StopAllCoroutines();
            _clientExitLobby.Invoke();
        }

        // Request session markers, and download them if already available
        [Command]
        void CmdPollMarkers()
        {
            ARCSession.session.PollMarkers(this);
        }

        // Send client position and rotation to server
        [Command]
        void CmdSubmitPose(double tick, Vector3 pos, Quaternion rot)
        {
            if (tick < _relativeTick) return;

            _relativeTick = tick;
            _position = pos;
            _rotation = rot;
        }

        // Send client position and rotation to server (unreliable channel)
        [Command (channel = 1)]
        void CmdSubmitPoseUnreliable(double tick, Vector3 pos, Quaternion rot)
        {
            if (tick < _relativeTick) return;

            _relativeTick = tick;
            _position = pos;
            _rotation = rot;
        }

        // Set friendly username
        [Command]
        public void CmdSetUsername(string name)
        {
            username = name;
        }

        // RPCs
        [ClientRpc]
        public void RpcOnStartedGame()
        {
            _gameStarted.Invoke();
        }

        public void OnGrabEntity(ARCEntity entity)
        {
            interactingEntity = entity;
            _grabEvent.Invoke();
        }

        public void OnReleaseEntity()
        {
            interactingEntity = null;
            _releaseEvent.Invoke();
        }
    }
}
