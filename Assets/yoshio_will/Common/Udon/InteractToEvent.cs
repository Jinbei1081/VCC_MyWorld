
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class InteractToEvent : UdonSharpBehaviour
    {
        public bool Networked = false;
        public NetworkEventTarget EventTarget = NetworkEventTarget.Owner;
        public UdonBehaviour TargetUdonBehaviour;
        public string EventName;

        public override void Interact()
        {
            if (Networked)
            {
                TargetUdonBehaviour.SendCustomNetworkEvent(EventTarget, EventName);
            }
            else
            {
                TargetUdonBehaviour.SendCustomEvent(EventName);
            }
        }
    }
}