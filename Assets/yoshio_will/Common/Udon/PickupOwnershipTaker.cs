
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PickupOwnershipTaker : UdonSharpBehaviour
    {
        public override void OnPickup()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            Transform[] trans = GetComponentsInChildren<Transform>(true);
            foreach(var tran in trans)
            {
                Networking.SetOwner(localPlayer, tran.gameObject);
            }
        }
    }
}