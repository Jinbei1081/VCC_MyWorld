
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalPlayerFollower : UdonSharpBehaviour
    {
        [SerializeField] private bool IsStayOnGround = false;
        [SerializeField] private bool IsRotateGroundNormal = false;
        [SerializeField] private Vector3 RotateVector = new Vector3(0, 1, 0);
        [SerializeField] private LayerMask GroundLayerMask = 1;

        private VRCPlayerApi _localPlayer;

        const float RaycastOffsetHeight = 0.5f;
        const float RaycastDistance = 100f;

        void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            Vector3 pos = _localPlayer.GetPosition();
            if (IsStayOnGround)
            {
                RaycastHit rhit;
                Vector3 raycastOrigin = pos + Vector3.up * RaycastOffsetHeight;
                if (Physics.Raycast(raycastOrigin, Vector3.down, out rhit, RaycastDistance, GroundLayerMask))
                {
                    pos = rhit.point;
                    if (IsRotateGroundNormal)
                    {
                        transform.rotation = Quaternion.FromToRotation(RotateVector, rhit.normal);
                    }
                }
            }
            transform.position = pos;
        }
    }
}