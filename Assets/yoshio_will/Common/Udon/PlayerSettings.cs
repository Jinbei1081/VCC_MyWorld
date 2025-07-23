
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerSettings : UdonSharpBehaviour
    {
        [SerializeField] private Slider RunSpeedSlider;
        [SerializeField] private float WalkSpeedMagnifier = 0.5f;
        [SerializeField] private float StrafeSpeedMagnifier = 0.5f;
        [SerializeField] private Slider JumpImpulseSlider;

        void Start()
        {

        }

        public void OnRunSpeedSliderChanged()
        {
            float speed = RunSpeedSlider.value;
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            localPlayer.SetRunSpeed(speed);
            localPlayer.SetWalkSpeed(speed * WalkSpeedMagnifier);
            localPlayer.SetStrafeSpeed(speed * StrafeSpeedMagnifier);
        }

        public void OnJumpImpulseSliderChanged()
        {
            float impulse = JumpImpulseSlider.value;
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            localPlayer.SetJumpImpulse(impulse);
        }
    }
}