
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePoint : UdonSharpBehaviour
    {
        [SerializeField] public Transform SpawnTransform;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (!player.isLocal) return;

            SpawnTransform.position = transform.position;
            SpawnTransform.rotation = transform.rotation;
        }
    }
}