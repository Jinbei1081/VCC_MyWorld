
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRCWorldSettingsCKW : UdonSharpBehaviour
    {
        [Header("Player Locomotion Settings")]
        [SerializeField] private float JumpImpulse = 3f;
        [SerializeField] private float WalkSpeed = 2f;
        [SerializeField] private float RunSpeed = 4f;
        [SerializeField] private float StrafeSpeed = 2f;

        [Header("Avatar Scaling Settings")]
        [SerializeField] private bool IsDisableAvatarScaling = false;
        [SerializeField] private float MinimumHeight = 0.2f;
        [SerializeField] private float MaximumHeight = 5f;
        [SerializeField] private bool IsAlwaysEnforceHeight = false;

        [Header("Player Voice Settings")]
        [SerializeField] private bool IsSetPlayerVoiceSettings = false;
        [SerializeField] private float VoiceGain = 0f;
        [SerializeField] private float VoiceDistanceNear = 0f;
        [SerializeField] private float VoiceDistanceFar = 25f;
        [SerializeField] private float VoiceVolumetricRadius = 0f;
        [SerializeField] private bool IsVoiceLowpass = true;

        [Header("Avatar Audio Settings")]
        [SerializeField] private bool IsSetAvatarAudioSettings = false;
        [SerializeField] private float AvatarAudioGain = 10f;
        [SerializeField] private float AvatarAudioNearRadius = 40f;
        [SerializeField] private float AvatarAudioFarRadius = 40f;
        [SerializeField] private float AvatarAudioVolumetricRadius = 40f;
        [SerializeField] private bool IsAvatarAudioForceSpatial = false;
        [SerializeField] private bool IsAvatarAudioCustomCurve = false;

        private VRCPlayerApi _localPlayer;

        void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(_localPlayer))
            {
                Debug.Log("VRCWorldSettingsCKW: LocalPlayer is INVALID. Aborting.");
                enabled = false;
                return;
            }

            _localPlayer.SetJumpImpulse(JumpImpulse);
            _localPlayer.SetWalkSpeed(WalkSpeed);
            _localPlayer.SetRunSpeed(RunSpeed);
            _localPlayer.SetStrafeSpeed(StrafeSpeed);

            _localPlayer.SetManualAvatarScalingAllowed(!IsDisableAvatarScaling);
            _localPlayer.SetAvatarEyeHeightMinimumByMeters(MinimumHeight);
            _localPlayer.SetAvatarEyeHeightMaximumByMeters(MaximumHeight);
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (!Utilities.IsValid(player)) return;
            if (!player.isLocal) return;
            if (!IsAlwaysEnforceHeight) return;

            float currentHeight = _localPlayer.GetAvatarEyeHeightAsMeters();
            float clampedHeight = Mathf.Clamp(currentHeight, MinimumHeight, MaximumHeight);
            if (clampedHeight == currentHeight) return;

            _localPlayer.SetAvatarEyeHeightByMeters(clampedHeight);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;

            if (IsSetPlayerVoiceSettings)
            {
                player.SetVoiceGain(VoiceGain);
                player.SetVoiceDistanceNear(VoiceDistanceNear);
                player.SetVoiceDistanceFar(VoiceDistanceFar);
                player.SetVoiceVolumetricRadius(VoiceVolumetricRadius);
                player.SetVoiceLowpass(IsVoiceLowpass);
            }

            if (IsSetAvatarAudioSettings)
            {
                player.SetAvatarAudioGain(AvatarAudioGain);
                player.SetAvatarAudioNearRadius(AvatarAudioNearRadius);
                player.SetAvatarAudioFarRadius(AvatarAudioFarRadius);
                player.SetAvatarAudioVolumetricRadius(AvatarAudioVolumetricRadius);
                player.SetAvatarAudioForceSpatial(IsAvatarAudioForceSpatial);
                player.SetAvatarAudioCustomCurve(IsAvatarAudioCustomCurve);
            }
        }
    }
}
