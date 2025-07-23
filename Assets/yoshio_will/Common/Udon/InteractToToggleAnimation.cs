
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class InteractToToggleAnimation : UdonSharpBehaviour
    {
        [SerializeField] private Animator Animator;
        [SerializeField] private string ToggleParameterName;

        public override void Interact()
        {
            if (Animator.GetBool(ToggleParameterName))
            {
                // オンなのでオフにする
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOffAnimation));
            }
            else
            {
                // オフなのでオンにする
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOnAnimation));
            }
        }

        public void ToggleOnAnimation()
        {
            Animator.SetBool(ToggleParameterName, true);
        }

        public void ToggleOffAnimation()
        {
            Animator.SetBool(ToggleParameterName, false);
        }

        // LateJoiner向け同期
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (player.isLocal) return;
            if (!Networking.IsOwner(gameObject)) return;
            
            if (Animator.GetBool(ToggleParameterName))
            {
                // オンなのでオンを伝える
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOnAnimation));
            }
            else
            {
                // オフなのでオフを伝える
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOffAnimation));
            }
        }
    }
}