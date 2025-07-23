
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PickupUseToEvent : UdonSharpBehaviour
    {
        [Header("PickupUseDown")]
        public bool IsUseDownNetworked = false;
        public NetworkEventTarget UseDownEventTarget = NetworkEventTarget.Owner;
        public UdonBehaviour UseDownTargetUdonBehaviour;
        public string UseDownEventName;

        [Header("PickupUseUp")]
        public bool IsUseUpNetworked = false;
        public NetworkEventTarget UseUpEventTarget = NetworkEventTarget.Owner;
        public UdonBehaviour UseUpTargetUdonBehaviour;
        public string UseUpEventName;

        public override void OnPickupUseDown()
        {
            if (!UseDownTargetUdonBehaviour) return;

            if (IsUseDownNetworked)
            {
                UseDownTargetUdonBehaviour.SendCustomNetworkEvent(UseDownEventTarget, UseDownEventName);
            }
            else
            {
                UseDownTargetUdonBehaviour.SendCustomEvent(UseDownEventName);
            }
        }

        public override void OnPickupUseUp()
        {
            if (!UseUpTargetUdonBehaviour) return;

            if (IsUseUpNetworked)
            {
                UseUpTargetUdonBehaviour.SendCustomNetworkEvent(UseUpEventTarget, UseUpEventName);
            }
            else
            {
                UseUpTargetUdonBehaviour.SendCustomEvent(UseUpEventName);
            }
        }
    }
}