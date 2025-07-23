
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class InteractToTriggerAnimation : UdonSharpBehaviour
    {
        [SerializeField] private Animator Animator;
        [SerializeField] private string TriggerParameterName;

        public override void Interact()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TriggerAnimation));
        }

        public void TriggerAnimation()
        {
            Animator.SetTrigger(TriggerParameterName);
        }
    }
}