
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual),RequireComponent(typeof(Collider))]
    public class PlayerAddVelocity : UdonSharpBehaviour
    {
        [SerializeField] private AudioSource AudioSource;
        [SerializeField] private Transform VelocityTransform;

        private AudioClip _audioClip;

        void Start()
        {
            if (AudioSource) _audioClip = AudioSource.clip;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (!player.isLocal) return;

            Vector3 vel = VelocityTransform.forward * VelocityTransform.localScale.x;
            player.SetVelocity(vel);

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EmitSound));
        }

        public void EmitSound()
        {
            if (AudioSource && _audioClip) AudioSource.PlayOneShot(_audioClip);
        }
    }
}
