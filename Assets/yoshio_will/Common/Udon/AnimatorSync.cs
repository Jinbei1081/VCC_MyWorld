
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AnimatorSync : UdonSharpBehaviour
    {
        // このUdonBehaviourと同じところにくっついているAnimatorのレイヤ0のループするアニメーションを同期する
        // Resetという名前のTriggerが発火したらアニメーションが巻き戻るように組んでおくこと

        private Animator _animator;
        private float _previousNormalizedTimeInt;
        private int _animReset;
        private float _nextResetTime;
        [SerializeField] private float ResetInterval = 600;

        void Start()
        {
            _animator = GetComponent<Animator>();
            _animReset = Animator.StringToHash("Reset");
        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
                float currentTimeInt = Mathf.Floor(info.normalizedTime);
                if (_previousNormalizedTimeInt != currentTimeInt)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TriggerReset));
                    _previousNormalizedTimeInt = currentTimeInt;
                }
            }
        }

        public void TriggerReset()
        {
            if (Time.time < _nextResetTime) return;

            _animator.SetTrigger(_animReset);
            _nextResetTime = Time.time + ResetInterval;
        }
    }
}