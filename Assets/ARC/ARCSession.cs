using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

namespace ARC
{
    [HelpURL("https://www.example.com")]
    [AddComponentMenu("ARC/ARC Session")]
    [DisallowMultipleComponent]
    public class ARCSession : NetworkBehaviour
    {
        public GameObject[] SharedSessionPrefabs;

        public static ARCSession session { get; private set; }
        public static Transform origin;
        public static ARCSessionState sessionState { get { return session == null ? ARCSessionState.Unavailable : session._sessionState; } }

        public Vector3 sessionSpaceDisplacement { get { return _sessionSpaceDisplacement; } }
        public float sessionSpaceOrientation { get { return _sessionSpaceOrientation; } }

        [SyncVar] ARCSessionState _sessionState;
        [SyncVar (hook = "OnSessionDisplacement")] Vector3 _sessionSpaceDisplacement;
        [SyncVar (hook = "OnSessionOrientation")] float _sessionSpaceOrientation;

        ARAnchorManager _anchorManager;
        int _targetsReady;
        List<ARCImageContainer> _encodedMarkers;
        List<ARCUser> _sessionUsers;
        
        void OnEnable()
        {
            if (_anchorManager == null) { _anchorManager = FindObjectOfType<ARAnchorManager>(); }
            if (_anchorManager) { _anchorManager.anchorsChanged += OnAnchorsChanged; }
        }

        void OnDisable()
        {
            if (_anchorManager) { _anchorManager.anchorsChanged -= OnAnchorsChanged; }
        }

        void Start()
        {
            _targetsReady = 0;

            foreach (GameObject go in SharedSessionPrefabs) { ClientScene.RegisterPrefab(go); }

            FindObjectOfType<ARCSessionListener>().sharedSession = this;
            _anchorManager = FindObjectOfType<ARAnchorManager>();
        }

        // CaptainsMess callback
        [Server]
        public override void OnStartServer()
        {
            _sessionState = ARCSessionState.Connecting;
        }

        // CaptainsMess callback
        [Server]
        public void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
        {
            _sessionState = ARCSessionState.Playing;
            
            _sessionUsers = aStartingPlayers.Select(p => p as ARCUser).ToList();
            foreach (ARCUser user in _sessionUsers) { user.RpcOnStartedGame(); }

            // Start playing
            RpcOnStartedGame();
        }

        // Set markers for the current session and propagate them (called by host client)
        [Server]
        public void SetSessionMarkers(List<ARCImageContainer> markers)
        {
            _sessionState = ARCSessionState.Lobby;

            _encodedMarkers = markers;
            _targetsReady = markers.Count;

            // Propagate targets to current clients
            CaptainsMess mess = FindObjectOfType<CaptainsMess>();
            ARCUser localUser = mess.LocalPlayer() as ARCUser;
            List<ARCUser> currentClients = mess.Players()
                .Select(u => u as ARCUser)
                .Where(u => u != localUser).ToList();
            StartCoroutine(PropagateMarkersCoroutine(currentClients));
        }

        // Set position and Y-axis rotation offset for the shared scene (called by host client)
        [Server]
        public void SetSessionSpacePose(Vector3 displacement, float orientation)
        {
            _sessionSpaceDisplacement = displacement;
            _sessionSpaceOrientation = orientation;
        }

        // Request session markers, and download them if already available (called by guest clients)
        [Server]
        public void PollMarkers(ARCUser user)
        {
            if (_encodedMarkers == null) { return; }

            List<ARCUser> newUser = new List<ARCUser>();
            newUser.Add(user);
            StartCoroutine(PropagateMarkersCoroutine(newUser));
        }

        // Send markers to currently connected users (and later joiners, with 1-dimensional lists)
        [Server]
        IEnumerator PropagateMarkersCoroutine(List<ARCUser> clients)
        {
            int[] manifest = new int[_encodedMarkers.Count];

            // Clients
            for (int i = 0; i < _encodedMarkers.Count; ++i)
            {
                manifest[i] = _encodedMarkers[i].chunks;
            }
            foreach (ARCUser client in clients)
            {
                TargetSendManifest(client.connectionToClient, manifest);
                yield return null;
            }

            for (int i = 0; i < _encodedMarkers.Count; ++i)
            {
                for (int j = 0; j < _encodedMarkers[i].chunks; ++j)
                {
                    foreach (ARCUser client in clients)
                    {
                        TargetSendChunk(client.connectionToClient, i, j, _encodedMarkers[i].content[j]);
                    }
                    yield return null;
                }
            }
        }

        // Send markers manifest to a single connection
        [TargetRpc]
        void TargetSendManifest(NetworkConnection target, int[] manifest)
        {
            _encodedMarkers = new List<ARCImageContainer>();
            _targetsReady = 0;

            for (int i = 0; i < manifest.Length; ++i)
            {
                _encodedMarkers.Add(new ARCImageContainer(manifest[i]));
            }
            ARCSetupManager.manager.networkEvents.manifestReceived.Invoke(manifest);
        }

        // Send marker chunk to a single connection
        [TargetRpc]
        void TargetSendChunk(NetworkConnection target, int index, int chunk, string content)
        {
            float prog = PutChunk(index, chunk, content);
            if (prog == 1f)
            {
                // New target received
                ARCSetupManager.manager.AddMarker(index, _encodedMarkers[index].DecodeImage());
                _targetsReady++;
                if (_targetsReady == _encodedMarkers.Count)
                {
                    // Create runtime library
                    ARCSetupManager.manager.FinalizeMarkers();
                }
            }
            else
            {
                ARCSetupManager.manager.networkEvents.chunkReceived.Invoke(index, prog);
            }
        }

        // Support function for TargetSendChunk()
        float PutChunk(int index, int chunk, string content)
        {
            _encodedMarkers[index].content[chunk] = content;
            return _encodedMarkers[index].progress;
        }

        // CaptainsMess callback
        [Server]
        public void OnAbortGame()
        {
            RpcOnAbortedGame();
        }

        // CaptainsMess callback
        [Client]
        public override void OnStartClient()
        {
            if (session)
            {
                Debug.LogError("ERROR: Another ARSharedSession!");
            }
            session = this;
        }

        // CaptainsMess callback
        public void OnJoinedLobby()
        {
            _sessionState = ARCSessionState.Setup;
        }

        // CaptainsMess callback
        public void OnLeftLobby()
        {
            _sessionState = ARCSessionState.Offline;
            if (origin != null)
                Destroy(origin.root);
        }

        // CaptainsMess callback
        public void OnCountdownStarted()
        {
            _sessionState = ARCSessionState.Countdown;
        }

        // CaptainsMess callback
        public void OnCountdownCancelled()
        {
            _sessionState = ARCSessionState.Lobby;
        }

        // Spawn an entity in the shared session (ARCSession.origin local space)
        [Server]
        public static GameObject Spawn(GameObject entity, Vector3 position, Quaternion rotation)
        {
            if (session == null) throw new System.Exception("Attempting to spawn an entity before creating a session space.");

            GameObject go = Instantiate(entity);
            go.transform.SetParent(origin);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            NetworkServer.Spawn(go);
            session.RpcSpawn(go.GetComponent<NetworkIdentity>().netId, position, rotation);
            return go;
        }

        // Set an entity transform in the shared session, client side (ARCSession.origin local space)
        [ClientRpc]
        void RpcSpawn(NetworkInstanceId netId, Vector3 position, Quaternion rotation)
        {
            GameObject go = ClientScene.FindLocalObject(netId);
            go.transform.SetParent(origin);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
        }

        [Server]
        public static void Destroy(GameObject entity)
        {
            if (session == null) throw new System.Exception("Attempting to destroy an entity before creating a session space.");
            
            NetworkServer.Destroy(entity);
        }

        // Perform a raycast in the session local space, returning the ARCEntity hit
        public static ARCEntity RaycastEntity(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            Vector3 wOrigin = ARCSession.origin.TransformPoint(origin);
            Vector3 wDirection = ARCSession.origin.TransformDirection(direction);
            RaycastHit hitInfo;
            
            if (Physics.Raycast(wOrigin, wDirection, out hitInfo, maxDistance, layerMask))
                return hitInfo.transform.GetComponent<ARCEntity>();

            return null;
        }

        public static Vector3 RaycastPosition(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            Vector3 wOrigin = ARCSession.origin.TransformPoint(origin);
            Vector3 wDirection = ARCSession.origin.TransformDirection(direction);
            RaycastHit hitInfo;
            
            if (Physics.Raycast(wOrigin, wDirection, out hitInfo, maxDistance, layerMask))
                return ARCSession.origin.InverseTransformPoint(hitInfo.point);

            return Vector3.negativeInfinity;
        }

        // Adjust session scene orientation when anchor rotates too much
        void OnAnchorsChanged(ARAnchorsChangedEventArgs eventArgs)
        {
            ARAnchor anchor = null;

            for (int i = 0; i < eventArgs.added.Count; ++i)
            {
                anchor = eventArgs.added[i];

                Vector3 fwdProj = Vector3.ProjectOnPlane(anchor.transform.forward, Vector3.up);
                fwdProj = Quaternion.Euler(0f, _sessionSpaceOrientation, 0f) * fwdProj;
                Quaternion newRot = Quaternion.LookRotation(fwdProj, Vector3.up);
                ARCSession.origin.rotation = newRot;
            }

            for (int i = 0; i < eventArgs.updated.Count; ++i)
            {
                anchor = eventArgs.updated[i];
                Vector3 fwdProj = Vector3.ProjectOnPlane(anchor.transform.forward, Vector3.up);
                fwdProj = Quaternion.Euler(0f, _sessionSpaceOrientation, 0f) * fwdProj;
                Quaternion newRot = Quaternion.LookRotation(fwdProj, Vector3.up);
                ARCSession.origin.rotation = newRot;
            }

            for (int i = 0; i < eventArgs.removed.Count; ++i)
            {
                anchor = eventArgs.removed[i];
                Debug.Log($"Removed anchor {anchor.trackableId}");
            }
        }

        // Session scene position hook function
        void OnSessionDisplacement(Vector3 displacement)
        {
            _sessionSpaceDisplacement = displacement;
            ARCSession.origin.localPosition = _sessionSpaceDisplacement;
            ARCSetupManager.manager.sessionEvents.sessionAnchorCreated.Invoke();
        }

        // Session scene rotation hook function
        void OnSessionOrientation(float orientation)
        {
            _sessionSpaceOrientation = orientation;
            Vector3 fwdProj = Vector3.ProjectOnPlane(ARCSession.origin.parent.forward, Vector3.up);
            fwdProj = Quaternion.Euler(0f, _sessionSpaceOrientation, 0f) * fwdProj;
            Quaternion newRot = Quaternion.LookRotation(fwdProj, Vector3.up);
            ARCSession.origin.rotation = newRot;
        }

        // Client-side RPCs
        [ClientRpc]
        public void RpcOnStartedGame() {
            //gameRulesField.gameObject.SetActive(true);
        }

        [ClientRpc]
        public void RpcOnAbortedGame() {
            //gameRulesField.gameObject.SetActive(false);
        }
    }
}