
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AutoRespawnPickup : UdonSharpBehaviour
    {
        private float _timer;
        [SerializeField] private float RespawnTimer;
        [SerializeField] private Animator Animator;
        [SerializeField] private AudioSource OnCollisionSound;
        [SerializeField] private UdonBehaviour RespawnEventTarget;
        [SerializeField] private string RespawnEventName;
        [Header("Home Position")]
        [SerializeField] private bool IsSnapToHome = false;
        [SerializeField] private float HomeAreaRadius = 0.5f;
        [Header("OnUseDown")]
        [SerializeField] private AudioSource OnUseSound;
        [SerializeField] private ParticleSystem OnUseParticle;
        [SerializeField] private UdonBehaviour OnUseCustomEvent;
        [SerializeField] private string CustomEventName;
        [Header("OnUseDown Count")]
        [SerializeField] private Animator UseCountAnimator;
        [SerializeField] private int MaxCount;
        private int _currentCount;
        private VRCObjectSync _sync;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private int _animPickedUp, _animIsHomePosition, _animCount;
        private bool _isPickedUp, _isInHome, _isWantToEmitSound;
        private AudioClip _onCollisionAudioClip, _onUseAudioClip;
        private float _nextSoundEmittableTime;

        const float MinSoundEventInterval = 0.1f;

        void Start()
        {
            _sync = GetComponent<VRCObjectSync>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _animPickedUp = Animator.StringToHash("PickedUp");
            _animIsHomePosition = Animator.StringToHash("HomePosition");
            _animCount = Animator.StringToHash("Count");
            if (OnCollisionSound) _onCollisionAudioClip = OnCollisionSound.clip;
            if (OnUseSound) _onUseAudioClip = OnUseSound.clip;
            _currentCount = 0;
        }

        public override void OnPickup()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GlobalPickup));
            _isPickedUp = true;
        }

        public override void OnDrop()
        {
            _timer = Time.time + RespawnTimer;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GlobalDrop));
            _isPickedUp = false;
        }

        private void Update()
        {
            if (!Networking.IsOwner(gameObject)) return;

            // 音出す？
            if (_isWantToEmitSound && _nextSoundEmittableTime < Time.time)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EmitSound));
                _nextSoundEmittableTime = Time.time + MinSoundEventInterval;
                _isWantToEmitSound = false;
            }

            // リスポンタイマ
            if (float.IsInfinity(_timer)) _timer = Time.time + RespawnTimer;
            if (Time.time < _timer) return;

            // リスポーン見直しの時期が来た
            if (_isPickedUp)
            {
                // 手に持ってたら延期
                _timer = Time.time + RespawnTimer;
                return;
            }

            // リスポーン地点近くにあったら延期
            UpdateIsInHome();
            if (_isInHome)
            {
                _timer = Time.time + RespawnTimer;
                return;
            }

            Respawn();
            _timer = float.PositiveInfinity;

            if (RespawnEventTarget) RespawnEventTarget.SendCustomEvent(RespawnEventName);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (_isPickedUp) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GlobalPickup));
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GlobalDrop));
            if (UseCountAnimator) MasterSyncCount();
        }

        public void GlobalPickup()
        {
            if (Animator == null) return;
            Animator.SetBool(_animPickedUp, true);
            Animator.SetBool(_animIsHomePosition, false);
        }

        public void GlobalDrop()
        {
            if (Animator == null) return;
            Animator.SetBool(_animPickedUp, false);
            UpdateIsInHome();
        }

        public void GlobalUse()
        {
            if (OnUseSound) OnUseSound.PlayOneShot(_onUseAudioClip);
            if (OnUseParticle) OnUseParticle.Emit(1);
            if (OnUseCustomEvent) OnUseCustomEvent.SendCustomEvent(CustomEventName);
        }

        public override void OnPickupUseDown()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GlobalUse));
            if (UseCountAnimator)
            {
                _currentCount++;
                if (_currentCount > MaxCount) _currentCount = 0;
                MasterSyncCount();
            }
        }


        public void Respawn()
        {
            if (_sync != null)
            {
                _sync.Respawn();
            }
            else
            {
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
            }
            UpdateIsInHome();
        }

        private void UpdateIsInHome()
        {
            _isInHome = (Vector3.Distance(transform.position, _initialPosition) < HomeAreaRadius);
            if (IsSnapToHome && _isInHome)
            {
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
            }
            if (Animator) Animator.SetBool(_animIsHomePosition, _isInHome);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (OnCollisionSound == null) return;
            if (collision == null) return;

            _isWantToEmitSound = true;
        }

        public void EmitSound()
        {
            OnCollisionSound.PlayOneShot(_onCollisionAudioClip);
        }

        private void MasterSyncCount()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Set" + _currentCount.ToString());
        }

        public void Set0() { _currentCount = 0; UpdateCountAnimator(); }
        public void Set1() { _currentCount = 1; UpdateCountAnimator(); }
        public void Set2() { _currentCount = 2; UpdateCountAnimator(); }
        public void Set3() { _currentCount = 3; UpdateCountAnimator(); }
        public void Set4() { _currentCount = 4; UpdateCountAnimator(); }
        public void Set5() { _currentCount = 5; UpdateCountAnimator(); }
        public void Set6() { _currentCount = 6; UpdateCountAnimator(); }
        public void Set7() { _currentCount = 7; UpdateCountAnimator(); }
        public void Set8() { _currentCount = 8; UpdateCountAnimator(); }
        public void Set9() { _currentCount = 9; UpdateCountAnimator(); }
        private void UpdateCountAnimator()
        {
            if (UseCountAnimator) UseCountAnimator.SetInteger(_animCount, _currentCount);
        }
    }
}