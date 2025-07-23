
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PickupRespawnButton : UdonSharpBehaviour
    {
        [SerializeField] public VRC_Pickup[] Pickups;

        private Vector3[] _initialPositions;
        private Quaternion[] _initialRotations;
        private VRCObjectSync[] _objectSyncs;
        private Rigidbody[] _rigidBodies;
        private VRCPlayerApi _localPlayer;

        void Start()
        {
            int pickupCount = Pickups.Length;
            _initialPositions = new Vector3[pickupCount];
            _initialRotations = new Quaternion[pickupCount];
            _objectSyncs = new VRCObjectSync[pickupCount];
            _rigidBodies = new Rigidbody[pickupCount];
            _localPlayer = Networking.LocalPlayer;

            for (int i = 0; i < pickupCount; i++)
            {
                VRC_Pickup p = Pickups[i];
                _initialPositions[i] = Vector3.zero;
                _initialRotations[i] = Quaternion.identity;
                _objectSyncs[i] = null;
                _rigidBodies[i] = null;

                if (!Utilities.IsValid(p)) continue;
                Transform t = p.transform;
                _initialPositions[i] = t.position;
                _initialRotations[i] = t.rotation;

                VRCObjectSync s = t.GetComponent<VRCObjectSync>();
                if (Utilities.IsValid(s)) _objectSyncs[i] = s;

                Rigidbody r = t.GetComponent<Rigidbody>();
                if (Utilities.IsValid(r)) _rigidBodies[i] = r;
            }
        }

        public override void Interact()
        {
            DoRespawn();
        }

        public void DoRespawn()
        {
            for (int i = 0; i < Pickups.Length; i++)
            {
                VRC_Pickup p = Pickups[i];
                if (!Utilities.IsValid(p)) continue;
                GameObject g = p.gameObject;
                if (Utilities.IsValid(g)) Networking.SetOwner(_localPlayer, g);
                VRCObjectSync s = _objectSyncs[i];
                if (Utilities.IsValid(s))
                {
                    s.Respawn();
                }
                else
                {
                    Transform t = p.transform;
                    t.position = _initialPositions[i];
                    t.rotation = _initialRotations[i];

                    Rigidbody r = _rigidBodies[i];
                    if (Utilities.IsValid(r))
                    {
                        r.position = _initialPositions[i];
                        r.rotation = _initialRotations[i];
                    }
                }
            }
        }
    }
}