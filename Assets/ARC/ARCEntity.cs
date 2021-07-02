using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace ARC
{
    [HelpURL("https://www.example.com")]
    [AddComponentMenu("ARC/ARC Entity")]
    [DisallowMultipleComponent]
    public class ARCEntity : NetworkBehaviour
    {
        [Range(1, 20)]
        [SerializeField] int _updateRate = 5;
        [SerializeField] float _positionThreshold = 0.1f;
        [SerializeField] float _positionSnap = 2f;
        [Range(0f, 1f)]
        [SerializeField] float _positionFactor = 0.1f;
        [SerializeField] float _rotationThreshold = 1f;
        [SerializeField] float _rotationSnap = 20f;
        [Range(0f, 1f)]
        [SerializeField] float _rotationFactor = 0.1f;
        [SerializeField] float _sleepThreshold = 2f;
        [SerializeField] float _minScale = 0.1f;
        [SerializeField] float _maxScale = 2f;
        [Range(0f, 1f)]
        [SerializeField] float _scaleFactor = 0.1f;
        [SerializeField] bool _isInteractable = true;
        [SerializeField] bool _exclusiveMode = true;
        [SerializeField] ARCPositionMode _positionMode = ARCPositionMode.CenterToView;
        [SerializeField] Vector3 _positionOffset = Vector3.forward;
        [SerializeField] ARCRotationMode _rotationMode = ARCRotationMode.KeepInitialRotation;
        [SerializeField] bool _lookForward = false;
        [SerializeField] float _forkliftFactor = 1f;
        [SerializeField] bool _snapToGrid = false;
        [SerializeField] float _gridSize = 1f;
        [SerializeField] UnityEvent _grabEvent;
        [SerializeField] UnityEvent _releaseEvent;

        public bool isMoving { get { return !_isAtRest; } }
        public bool isInteracting { get { return handler != null; } }
        public ARCUser handler { get; private set; }

        [SyncVar(hook = "OnNewPosition")] Vector3 _position;
        [SyncVar(hook = "OnNewRotation")] Quaternion _rotation;
        [SyncVar(hook = "OnNewLinVelocity")] Vector3 _linVelocity;
        [SyncVar(hook = "OnNewAngVelocity")] Vector3 _angVelocity;
        [SyncVar] bool _isAtRest;
        [SyncVar] float _scale;
        [SyncVar] Vector3 _handlePosition;
        [SyncVar] Quaternion _handleRotation;
        Vector3 _startingScale;
        Quaternion _smoothedGrabRotation;
        float _updateTime;
        float _fmLerpValue;
        float _sqrPositionThreshold;
        float _lastMotionTime;
        Rigidbody _rb;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();

            _updateTime = 1f / _updateRate;
            _sqrPositionThreshold = _positionThreshold * _positionThreshold;
            _startingScale = transform.localScale;

            if (_rb == null)
                _isAtRest = true;
            else {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _scale = 1f;
            _position = transform.localPosition;
            _rotation = transform.localRotation;
            _handlePosition = Vector3.positiveInfinity;
            _handleRotation = Quaternion.identity;

            if (isServer)
            {
                StartCoroutine("PoseCoroutine");
            }
        }

        void Update()
        {
            UpdatePose();
        }

        void OnDestroy()
        {
            if (handler != null)
                handler.OnReleaseEntity();
        }

        void OnValidate()
        {
            if (_positionSnap < 0f) _positionSnap = 0f;
            if (_positionThreshold < 0f) _positionThreshold = 0f;
            if (_positionThreshold > _positionSnap) _positionThreshold = _positionSnap;

            if (_rotationSnap < 0f) _rotationSnap = 0f;
            if (_rotationThreshold < 0f) _rotationThreshold = 0f;
            if (_rotationThreshold > _rotationSnap) _rotationThreshold = _rotationSnap;

            if (_maxScale < 0f) _maxScale = 0f;
            if (_minScale < 0f) _minScale = 0f;
            if (_minScale > _maxScale) _minScale = _maxScale;

            if (_sleepThreshold < 0f) _sleepThreshold = 0f;
        }

        // Apply remote position and rotation with smoothing
        void UpdatePose()
        {
            if (isInteracting && _isInteractable)
            {
                // Entity was grabbed
                Vector3 grabPosition;
                Quaternion grabRotation;
                Vector3 userForward;
                Quaternion userRotation;

                if (_handlePosition == Vector3.positiveInfinity) {
                    // "_handleXXX" values haven't propagated yet, predict them for the next few frames
                    switch (_positionMode) {
                        case ARCPositionMode.KeepInitialOffset:
                            grabPosition = transform.localPosition; break;
                        case ARCPositionMode.CenterToView:
                            grabPosition = _positionOffset; break;
                        default: grabPosition = Vector3.zero; break;
                    }
                    switch (_rotationMode) {
                        case ARCRotationMode.UseViewRotation:
                            grabRotation = _lookForward ? Quaternion.identity : transform.localRotation; break;
                        case ARCRotationMode.KeepInitialRotation:
                        case ARCRotationMode.ForkliftMode:
                        default:
                            grabRotation = Quaternion.identity; break;
                    }
                } else {
                    // _handleXXX values propagated to clients, use them instead
                    grabPosition = _handlePosition;
                    grabRotation = _handleRotation;
                }
                
                if (handler.isLocalPlayer) {
                    // Entity was grabbed by local user - Its parent is Camera.main
                    float sessionScale = transform.root.localScale.x;

                    // Scaling is applied in order to account for AR Session Origin scale
                    transform.localPosition = Vector3.Lerp(transform.localPosition, grabPosition / sessionScale, _positionFactor);
                    transform.localScale = Vector3.Lerp(transform.localScale, _startingScale * _scale / sessionScale, _scaleFactor);
                    
                    // Use local Camera.main forward/rotation
                    userForward = handler.localCameraForward;
                    userRotation = handler.localCameraRotation;
                } else {
                    // Entity was grabbed by any other user - Its parent is such ARCUser
                    transform.localPosition = Vector3.Lerp(transform.localPosition, grabPosition, _positionFactor);
                    transform.localScale = Vector3.Lerp(transform.localScale, _startingScale * _scale, _scaleFactor);

                    // Use interpolated ARCUser forward/rotation
                    userForward = ARCSession.origin.InverseTransformDirection(handler.transform.forward);
                    userRotation = handler.transform.localRotation;
                }

                switch (_rotationMode) {
                    case ARCRotationMode.KeepInitialRotation:
                        _smoothedGrabRotation = Quaternion.Lerp(_smoothedGrabRotation, grabRotation, _rotationFactor);
                        transform.localRotation = Quaternion.Inverse(userRotation) * _rotation * _smoothedGrabRotation; break;
                    case ARCRotationMode.UseViewRotation:
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, grabRotation, _rotationFactor); break;
                    case ARCRotationMode.ForkliftMode:
                    default:
                        Vector3 fwdProj = new Vector3(userForward.x, 0f, userForward.z);
                        Quaternion forkliftRotation = Quaternion.LookRotation(fwdProj, Vector3.up);
                        _smoothedGrabRotation = Quaternion.Lerp(_smoothedGrabRotation, grabRotation, _rotationFactor);
                        _fmLerpValue = Mathf.Clamp01(_fmLerpValue + Time.deltaTime * _forkliftFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Inverse(userRotation) * forkliftRotation * _smoothedGrabRotation, _fmLerpValue); break;
                }
            }
            else
            {
                // Entity is free - Its parent is ARCSession.origin
                _fmLerpValue = 0f;
                if (_scale >= _minScale && _scale <= _maxScale)
                    transform.localScale = Vector3.Lerp(transform.localScale, _startingScale * _scale, _scaleFactor);

                if (!isServer)
                {
                    // Interpolate on clients only, since hosts are authoritative
                    if (_rb != null) _rb.isKinematic = _isAtRest;

                    if (_isAtRest) {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, _position, _positionFactor);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, _rotation, _rotationFactor);
                    }
                }
            }
        }

        // Send position and rotation updates when appropriate
        [Server]
        IEnumerator PoseCoroutine()
        {
            _lastMotionTime = Time.time;
            
            while (true)
            {
                if (!isInteracting)
                {
                    bool hasMoved = (transform.localPosition - _position).sqrMagnitude > _sqrPositionThreshold;
                    bool hasRotated = Quaternion.Angle(transform.localRotation, _rotation) > _rotationThreshold;

                    if (hasMoved || hasRotated) {
                        // Started moving/still moving
                        _lastMotionTime = Time.time;
                        _isAtRest = false;
                    } else if (Time.time - _lastMotionTime > _sleepThreshold) {
                        // Has been still long enough, time to rest
                        _isAtRest = true;
                    }
                }
                else
                {
                    _isAtRest = true;
                }

                if (_isAtRest || isInteracting)
                {
                    yield return null;
                }
                else
                {
                    if (_rb != null) {
                        SetMotion(
                            ARCSession.origin.InverseTransformVector(_rb.velocity),
                            ARCSession.origin.InverseTransformVector(_rb.angularVelocity)
                        );
                    }
                    SetPose(transform.localPosition, transform.localRotation);
                    yield return new WaitForSeconds(_updateTime);
                }
            }
        }

        // Set current position and rotation - Implies entity was release if interacting
        [Server]
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _handlePosition = Vector3.positiveInfinity;
            SetDirtyBit(1); // Force SyncVar update
        }

        // Set current linear and angular velocity - Used for rigidbodies only
        [Server]
        public void SetMotion(Vector3 velocity, Vector3 angularVelocity)
        {
            _linVelocity = velocity;
            _angVelocity = angularVelocity;
        }

        // Interaction - Grab entity
        [Server]
        public void Grab(ARCUser user, Vector3 position, Quaternion rotation)
        {
            if (!_isInteractable || (user == null)) return;

            // Entity was grabbed by an ARCUser
            if (handler != null)
            {
                if (_exclusiveMode || handler == user)
                {
                    return;
                }
                else
                {
                    Release(handler, position, rotation);
                    StartCoroutine(StealGrab(user, position, rotation));
                    return;
                }
            }

            handler = user;
            if (_rb != null) _rb.isKinematic = true;

            switch (_positionMode) {
                case ARCPositionMode.KeepInitialOffset:
                    _handlePosition = Quaternion.Inverse(rotation) * (transform.localPosition - position); break;
                case ARCPositionMode.CenterToView:
                    _handlePosition = _positionOffset; break;
                default: _handlePosition = Vector3.positiveInfinity; break;
            }

            switch (_rotationMode) {
                case ARCRotationMode.UseViewRotation:
                    _handleRotation = _lookForward ? Quaternion.identity : Quaternion.Inverse(rotation) * transform.localRotation; break;
                case ARCRotationMode.KeepInitialRotation:
                case ARCRotationMode.ForkliftMode:
                default:
                    _handleRotation = Quaternion.identity; break;
            }

            if (user.isLocalPlayer)
                transform.SetParent(Camera.main.transform);
            else
                transform.SetParent(user.transform);
            
            RpcGrab(user.GetComponent<NetworkIdentity>().netId);
            user.OnGrabEntity(this);
            _grabEvent.Invoke();
        }

        // Support coroutine - Deals with users grabbing other users' entities
        [Server]
        IEnumerator StealGrab(ARCUser user, Vector3 position, Quaternion rotation)
        {
            yield return new WaitForSeconds(0.1f);
            Grab(user, position, rotation);
        }

        // Interaction - Release entity
        [Server]
        public void Release(ARCUser user, Vector3 position, Quaternion rotation)
        {
            if (!_isInteractable || (user == null)) return;

            // Entity was released
            _lastMotionTime = Time.time;
            handler.OnReleaseEntity();
            handler = null;
            if (_rb != null) { _rb.isKinematic = false; }
            transform.SetParent(ARCSession.origin);

            Vector3 newPosition = rotation * _handlePosition + position;
            
            if (_snapToGrid)
            {
                newPosition = new Vector3(
                    Mathf.Round(newPosition.x / _gridSize) * _gridSize,
                    Mathf.Round(newPosition.y / _gridSize) * _gridSize,
                    Mathf.Round(newPosition.z / _gridSize) * _gridSize
                );
            }

            Quaternion newRotation;

            switch (_rotationMode) {
                case ARCRotationMode.ForkliftMode:
                    Vector3 uFwdProj = rotation * Vector3.forward;
                    uFwdProj = new Vector3(uFwdProj.x, 0f, uFwdProj.z);
                    newRotation = Quaternion.LookRotation(uFwdProj, Vector3.up) * _handleRotation; break;
                case ARCRotationMode.UseViewRotation:
                    newRotation = rotation * _handleRotation; break;
                case ARCRotationMode.KeepInitialRotation:
                default:
                    newRotation = _rotation * _handleRotation; break;
            }

            transform.localPosition = newPosition;
            transform.localRotation = newRotation;

            if (_rb != null)
            {
                SetMotion(_rb.velocity, _rb.angularVelocity);
            }
            SetPose(newPosition, newRotation);
            _releaseEvent.Invoke();
        }

        // Set absolute uniform scale
        [Server]
        public void SetScale(float newScale)
        {
            _scale = Mathf.Clamp(newScale, _minScale, _maxScale);
        }

        // Add scale amount to current uniform scale
        [Server]
        public void AddScale(float amount)
        {
            SetScale(_scale + amount);
        }

        // Set object rotation when grabbed. If not grabbed, the function won't have any effect
        [Server]
        public void SetHandlingRotation(Quaternion rotation)
        {
            _handleRotation = rotation;
        }

        // Rotate object when grabbed. If not grabbed, the function won't have any effect
        [Server]
        public void AddHandlingRotation(Quaternion rotation, bool local = true)
        {
            if (local)
                _handleRotation *= rotation;
            else
                _handleRotation *= Quaternion.Inverse(transform.localRotation) *
                Quaternion.Inverse(handler.transform.localRotation) * rotation;
        }

        // Propagate grabbing effects to clients
        [ClientRpc]
        void RpcGrab(NetworkInstanceId netId)
        {
            if (!_isInteractable) return;

            if (isServer) return;

            StartCoroutine(OnGrabClient(netId));
        }

        IEnumerator OnGrabClient(NetworkInstanceId netId)
        {
            yield return new WaitForSeconds(0.1f);

            ARCUser user = ClientScene.FindLocalObject(netId).GetComponent<ARCUser>();

            if (user.isLocalPlayer)
                transform.SetParent(Camera.main.transform);
            else
                transform.SetParent(user.transform);
            
            handler = user;
            handler.OnGrabEntity(this);

            if (_rb != null) _rb.isKinematic = true;
            _grabEvent.Invoke();
        }

        // Position hook function
        void OnNewPosition(Vector3 newPos)
        {
            _position = newPos;

            OnNewPose();

            if (_rb == null) return;

            _rb.isKinematic = false;

            Vector3 difference = _position - transform.localPosition;
            float distance = difference.magnitude;

            if (distance > _positionSnap)
                transform.localPosition = _position;
            else
                transform.localPosition = Vector3.Lerp(transform.localPosition, _position, _positionFactor);
        }

        // Rotation hook function
        void OnNewRotation(Quaternion newRot)
        {
            _rotation = newRot;
            _smoothedGrabRotation = Quaternion.identity;

            if (_rb == null) return;

            _rb.isKinematic = false;

            float distance = Quaternion.Angle(_rotation, transform.localRotation);

            if (distance > _rotationSnap)
                transform.localRotation = _rotation;
            else
                transform.localRotation = Quaternion.Lerp(transform.localRotation, _rotation, _rotationFactor);
        }

        // Same as SetPose() but applies to clients on position/rotation updates
        void OnNewPose()
        {
            transform.SetParent(ARCSession.origin);

            if (handler != null)
            {
                _releaseEvent.Invoke();
                handler.OnReleaseEntity();
                handler = null;
            }
        }

        // Linear velocity hook function
        void OnNewLinVelocity(Vector3 linearVelocity)
        {
            _linVelocity = ARCSession.origin.TransformVector(linearVelocity);
            if (_rb != null) _rb.velocity = _linVelocity;
        }

        // Angular velocity hook function
        void OnNewAngVelocity(Vector3 angularVelocity)
        {
            _angVelocity = ARCSession.origin.TransformVector(angularVelocity);
            if (_rb != null) _rb.angularVelocity = _angVelocity;
        }
    }
}
