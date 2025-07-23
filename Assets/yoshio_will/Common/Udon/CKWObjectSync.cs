
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CKWObjectSync : UdonSharpBehaviour
    {
        [Header("Common")]
        [SerializeField] private bool _allowCollisionOwnershipTransfer = false;

        [Header("Owner")]
        [SerializeField] public float SyncPositionMinDistance = 0.001f;
        [SerializeField] public float SyncPositionMaxDistance = 0.1f;
        [SerializeField] public float SyncRotationMinAngle = 1f;
        [SerializeField] public float SyncRotationMaxAngle = 10f;
        [SerializeField] public float SyncIntervalMin = 0.15f;
        [SerializeField] public float SyncIntervalMax = 10f;

        [Header("Remote")]
        [SerializeField] public bool IsUseForce = true;
        [SerializeField] public float SmoothDampSpeedMagnifier = 1.5f;
        [SerializeField] public float RotationLerpFactor = 1f;
        [SerializeField] public float PositionPropotional = 10f;
        [SerializeField] public float PositionIntegral = 0.0001f;
        [SerializeField] public float PositionDelivative = 5f;
        [SerializeField] public float RotationPropotional = 0.3f;
        [SerializeField] public float RotationIntegral = 0.01f;
        [SerializeField] public float RotationDelivative = 20f;

        /*
        [Header("Debug")]
        [SerializeField] private Transform TargetTransform;
        */

        [UdonSynced] private Vector3 _position;
        [UdonSynced] private Quaternion _rotation;
        [UdonSynced] private bool _isDiscontinuity = false;
        [UdonSynced] private bool _isKinematic;
        [UdonSynced] private bool _isUseGravity;

        private bool _isLocalOwned;
        private Rigidbody _rigidbody;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private VRCPlayerApi _localPlayer;

        private Vector3 _smoothedPosition, _smoothVelocity;
        private Quaternion _smoothedRotation;
        private float _syncReceiveInterval;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private float _prevSyncTime;    // Owner/Remoteで意味が異なる

        private Vector3 _posIntegralError, _posPrevError;
        private Quaternion _rotIntegralError, _rotPrevError;

        void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _position = transform.position;
            _rotation = transform.rotation;
            _smoothedPosition = _initialPosition;
            _smoothedRotation = _initialRotation;
            _rigidbody = GetComponent<Rigidbody>();
            _localPlayer = Networking.LocalPlayer;

            if (_rigidbody)
            {
                _isKinematic = _rigidbody.isKinematic;
                _isUseGravity = _rigidbody.useGravity;
            }
            else
            {
                _isKinematic = true;
                _isUseGravity = false;
            }

            _isLocalOwned = Networking.IsOwner(gameObject);
        }

        public bool AllowCollisionOwnershipTransfer
        {
            get => _allowCollisionOwnershipTransfer;
            set
            {
                _allowCollisionOwnershipTransfer = value;
            }
        }

        public void SetKinematic(bool value)
        {
            if (!_isLocalOwned) return;
            if (!_rigidbody) return;
            _isKinematic = value;
            _rigidbody.isKinematic = value; // for Owner
            RequestSerialization();
        }

        public void SetGravity(bool value)
        {
            if (!_isLocalOwned) return;
            if (!_rigidbody) return;
            _isUseGravity = value;
            _rigidbody.useGravity = value;  // for Owner
            RequestSerialization();
        }

        public void FlagDiscontinuity()
        {
            if (_isLocalOwned) _isDiscontinuity = true;
        }

        public void TeleportTo(Transform targetLocation)
        {
            OwnerTeleportTo(targetLocation.position, targetLocation.rotation);
        }

        public void Respawn()
        {
            OwnerTeleportTo(_initialPosition, _initialRotation);
        }

        private void OwnerTeleportTo(Vector3 position, Quaternion rotation)
        {
            if (!_isLocalOwned) return;
            SetPositionAndRotation(position, rotation);
            _isDiscontinuity = true;
            RequestSerialization();
        }

        private void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            if (_rigidbody)
            {
                _rigidbody.position = position;
                _rigidbody.rotation = rotation;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                _isDiscontinuity = false;
            }
        }

        private void Update()
        {
            if (_isLocalOwned)
            {
                // オーナーの処理
                float deltaTime = Time.deltaTime;

                // 前回同期からどれだけ移動したか
                float distance = Vector3.Distance(transform.position, _position);
                float angle = Quaternion.Angle(transform.rotation, _rotation);

                // deltaから同期間隔を算出する
                float distanceT = Mathf.InverseLerp(SyncPositionMinDistance, SyncPositionMaxDistance, distance);
                float angleT = Mathf.InverseLerp(SyncRotationMinAngle, SyncRotationMaxAngle, angle);
                float syncInterval = Mathf.Lerp(SyncIntervalMax, SyncIntervalMin, Mathf.Max(distanceT, angleT));

                // 同期する？
                if (_prevSyncTime + syncInterval < Time.time) Sync();
            }
            else
            {
                // 非オーナーの処理

                // スムーズ化（目標値）
                _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, _position, ref _smoothVelocity, _syncReceiveInterval / SmoothDampSpeedMagnifier);
                _smoothedRotation = Quaternion.Slerp(_smoothedRotation, _rotation, RotationLerpFactor * Time.deltaTime / _syncReceiveInterval);

                if (!_isKinematic && IsUseForce)
                {
                    // 非Kinematic(+Rigidbody)の場合は力を加えて目標位置へ持っていく
                    // Rigidbodyが無い場合、_isKinematicは必ずtrueになるようにしているので
                    // このスコープ内で _rigidbody は必ず not null

                    // 位置のPID
                    Vector3 posError = _smoothedPosition - transform.position;
                    _posIntegralError += posError * Time.deltaTime;
                    Vector3 posDelivativeError = (posError - _posPrevError) / Time.deltaTime;
                    _posPrevError = posError;
                    Vector3 posForce = posError * PositionPropotional + _posIntegralError * PositionIntegral + posDelivativeError * PositionDelivative;
                    _rigidbody.AddForce(posForce, ForceMode.Acceleration);

                    // 回転のPID
                    Quaternion rotError = _smoothedRotation * Quaternion.Inverse(transform.rotation);
                    _rotIntegralError *= Quaternion.Slerp(Quaternion.identity, rotError, Time.deltaTime);
                    Quaternion rotDelivativeError = Quaternion.Slerp(Quaternion.identity, rotError * Quaternion.Inverse(_rotPrevError), 1 / Time.deltaTime);
                    _rotPrevError = rotError;
                    Quaternion rotForceQ =
                        Quaternion.SlerpUnclamped(Quaternion.identity, rotError, RotationPropotional) *
                        Quaternion.SlerpUnclamped(Quaternion.identity, _rotIntegralError, RotationIntegral) *
                        Quaternion.SlerpUnclamped(Quaternion.identity, rotDelivativeError, RotationDelivative);
                    Vector3 rotForce = NormalizeForAngle(rotForceQ.eulerAngles);
                    _rigidbody.AddTorque(rotForce, ForceMode.Acceleration);
                }
                else
                {
                    // Kinematicでない場合は座標を直にセットして終わり
                    transform.position = _smoothedPosition;
                    transform.rotation = _smoothedRotation;
                }
            }

            _prevPosition = transform.position;
            _prevRotation = transform.rotation;
        }

        private Vector3 NormalizeForAngle(Vector3 v)
        {
            v.x = NormalizeForAngle(v.x);
            v.y = NormalizeForAngle(v.y);
            v.z = NormalizeForAngle(v.z);
            return v;
        }

        private float NormalizeForAngle(float v)
        {
            while (v < -180) v += 360;
            while (v > 180) v -= 360;
            return v;
        }

        private void Sync()
        {
            _prevSyncTime = Time.time;
            _position = transform.position;
            _rotation = transform.rotation;
            RequestSerialization();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_allowCollisionOwnershipTransfer) return;
            if (_isLocalOwned) return;
            GameObject gobj = collision.gameObject;
            if (!Networking.IsOwner(gobj)) return;

            // このオブジェクトのオーナでなく、ぶつかってきたオブジェクトのオーナーである場合に以下を実行する
            Networking.SetOwner(_localPlayer, gameObject);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _isLocalOwned = player.isLocal;
            UpdateKinematicAndGravity();
            if (_isLocalOwned)
            {
                SetPositionAndRotation(_position, _rotation);
            }
            else
            {
                _smoothedPosition = _position;
                _smoothedRotation = _rotation;
            }
        }

        private void UpdateKinematicAndGravity()
        {
            if (!_rigidbody) return;
            if (_isLocalOwned)
            {
                _rigidbody.useGravity = _isUseGravity;
                _rigidbody.isKinematic = _isKinematic;
            }
            else
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = _isKinematic;
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            _syncReceiveInterval = Time.time - _prevSyncTime;
            _prevSyncTime = Time.time;
            UpdateKinematicAndGravity();

            if (_isDiscontinuity)
            {
                _smoothedPosition = _position;
                _smoothedRotation = _rotation;

                _posIntegralError = Vector3.zero;
                _posPrevError = Vector3.zero;
                _rotIntegralError = Quaternion.identity;
                _rotPrevError = Quaternion.identity;

                SetPositionAndRotation(_position, _rotation);
            }
        }

        public override void OnPickup()
        {
            Networking.SetOwner(_localPlayer, gameObject);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (player.isLocal) return;
            if (_isLocalOwned) Sync();
        }
    }
}
