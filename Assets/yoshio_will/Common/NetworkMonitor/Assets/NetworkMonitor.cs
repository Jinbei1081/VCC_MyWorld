
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NetworkMonitor : UdonSharpBehaviour
    {
        [SerializeField] public Material ClogMaterial;
        [SerializeField] public Material SettleMaterial;
        [SerializeField] public Color BadColor = new Color(255, 0, 0);
        [SerializeField] public Color GoodColor = new Color(0, 255, 0);
        int _idColor;

        void Start()
        {
            _idColor = VRCShader.PropertyToID("_Color");
        }

        private void Update()
        {
            if (Networking.IsClogged) ClogMaterial.SetColor(_idColor, BadColor);
            else ClogMaterial.SetColor(_idColor, GoodColor);

            if (Networking.IsNetworkSettled) SettleMaterial.SetColor(_idColor, GoodColor);
            else SettleMaterial.SetColor(_idColor, BadColor);
        }
    }
}
