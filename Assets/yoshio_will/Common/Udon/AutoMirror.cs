
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [RequireComponent(typeof(Collider)),UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AutoMirror : UdonSharpBehaviour
    {
        [SerializeField] private float OffDistance = 5;
        [SerializeField] private float DotThresholdOff = 0;
        [SerializeField] private float DotThresholdLook = 0.25f;
        [SerializeField] private float DistanceThresholdOff = 2.5f;
        [SerializeField] private float DistanceThresholdLook = 1f;
        [SerializeField] private Animator MirrorAnimator;
        [SerializeField] private string AnimatorParamName = "MirrorFade";
        [SerializeField] private string AnimatorStateParamName = "State";

        private VRCPlayerApi _localPlayer;
        private Collider _collider;
        private int _animParam, _animStateParam;
        private bool _isCompletelyFade;

        private int _state = 0;

        void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _collider = GetComponent<Collider>();
            _animParam = Animator.StringToHash(AnimatorParamName);
            _animStateParam = Animator.StringToHash(AnimatorStateParamName);
        }

        private void Update()
        {
            if (_state != 0) return;

            Vector3 pos;
            Quaternion rot;

            VRCPlayerApi.TrackingData head = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            if (head.position == Vector3.zero)
            {
                // Genericアバター
                pos = _localPlayer.GetPosition();
                rot = _localPlayer.GetRotation();
            }
            else
            {
                pos = head.position;
                rot = head.rotation;
            }

            // OffDistanceより離れているか
            float distance = Vector3.Distance(transform.position, pos);
            if (distance > OffDistance && _isCompletelyFade) return;

            // 厳密な距離の測定
            Vector3 mirrorPoint = _collider.ClosestPoint(pos);
            float strictDistance = Vector3.Distance(mirrorPoint, pos);
            if (strictDistance > DistanceThresholdOff && _isCompletelyFade) return;

            // 視線のベクトルとの内積(1なら鏡をまっすぐ見ている)
            Vector3 look = rot * Vector3.back;
            Vector3 mirrorToHead = (pos - mirrorPoint).normalized;
            float dot = Vector3.Dot(mirrorToHead, look);

            // フェード量を計算しAnimatorにセット
            float fadeByDistance = InverseLerpUnclamped(DistanceThresholdOff, DistanceThresholdLook, strictDistance);
            float fadeByAngle = InverseLerpUnclamped(DotThresholdOff, DotThresholdLook, dot);  // 0=オフ 1=オン
            float fade = Mathf.Clamp01(fadeByAngle * fadeByDistance);
            MirrorAnimator.SetFloat(_animParam, fade);
            _isCompletelyFade = (fade <= 0);
        }

        private float InverseLerpUnclamped(float a, float b, float val)
        {
            if (a != b) return (val - a) / (b - a); else return 0;
        }

        public void SetAuto()
        {
            SetState(0);
        }

        public void SetOn()
        {
            SetState(1);
        }

        public void SetOff()
        {
            SetState(-1);
        }

        private void SetState(int state)
        {
            _state = state;
            MirrorAnimator.SetInteger(_animStateParam, state);
        }
    }
}
