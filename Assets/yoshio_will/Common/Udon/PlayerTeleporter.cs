
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerTeleporter : UdonSharpBehaviour
    {
        [SerializeField] private Transform Destination;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            player.TeleportTo(Destination.position, Destination.rotation);
        }
    }
}