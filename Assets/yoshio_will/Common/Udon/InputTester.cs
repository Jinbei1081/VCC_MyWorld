
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InputTester : UdonSharpBehaviour
    {
        private TextMeshProUGUI _text;
        private string[] _axes;
        private VRCPlayerApi _localPlayer;

        void Start()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
            _localPlayer = Networking.LocalPlayer;
            InitArray();
        }

        private void Update()
        {
            Vector3 playerPos = _localPlayer.GetPosition();
            float distance = Vector3.Distance(transform.position, playerPos);
            if (distance > 2) return;

            if (_axes == null) return;

            int inputMethod = (int)InputManager.GetLastUsedInputMethod();
            string str = "Input Method: " + inputMethod;
            switch (inputMethod)
            {
                case 0: str += " (Keyboard)"; break;
                case 1: str += " (Mouse)"; break;
                case 2: str += " (Gamepad)"; break;
                case 3: str += " (Gaze)"; break;
                case 5: str += " (Vive)"; break;
                case 6: str += " (Oculus)"; break;
                case 7: str += " (ViveXr)"; break;
                case 10: str += " (Index)"; break;
                case 11: str += " (HPMotionController)"; break;
                case 12: str += " (OSC)"; break;
                case 13: str += " (QuestHands)"; break;
                case 14: str += " (Generic)"; break;
                case 15: str += " (Touch)"; break;
                case 16: str += " (OpenXRGeneric)"; break;
                case 17: str += " (Pico)"; break;
                case 18: str += " (SteamVR2)"; break;
            }

            str += "\n\n";

            for (int idx = 0; idx < _axes.Length; idx++)
            {
                string axis = _axes[idx];
                str += string.Format("{0} : {1:<color=red>+0.000</color>;<color=blue>-0.000</color>; 0.000} / {2}\n", axis, Input.GetAxisRaw(axis), Input.GetButton(axis));
            }

            _text.text = str;
        }

        private void InitArray()
        {
            _axes = new string[] {
                "Cancel",
                "Fire1",
                "Fire2",
                "Fire3",
                "Horizontal",
                "Joy1 Axis 1",
                "Joy1 Axis 2",
                "Joy1 Axis 3",
                "Joy1 Axis 4",
                "Joy1 Axis 5",
                "Joy1 Axis 6",
                "Joy1 Axis 7",
                "Joy1 Axis 8",
                "Joy1 Axis 9",
                "Joy1 Axis 10",
                "Joy2 Axis 1",
                "Joy2 Axis 2",
                "Joy2 Axis 3",
                "Joy2 Axis 4",
                "Joy2 Axis 5",
                "Joy2 Axis 6",
                "Joy2 Axis 7",
                "Joy2 Axis 8",
                "Joy2 Axis 9",
                "Joy2 Axis 10",
                "Jump",
                "Oculus_CrossPlatform_Button2",
                "Oculus_CrossPlatform_Button4",
                "Oculus_CrossPlatform_PrimaryHandTrigger",
                "Oculus_CrossPlatform_PrimaryIndexTrigger",
                "Oculus_CrossPlatform_PrimaryThumbstick",
                "Oculus_CrossPlatform_PrimaryThumbstickHorizontal",
                "Oculus_CrossPlatform_PrimaryThumbstickVertical",
                "Oculus_CrossPlatform_SecondaryHandTrigger",
                "Oculus_CrossPlatform_SecondaryIndexTrigger",
                "Oculus_CrossPlatform_SecondaryThumbstick",
                "Oculus_CrossPlatform_SecondaryThumbstickHorizontal",
                "Oculus_CrossPlatform_SecondaryThumbstickVertical",
                "Oculus_GearVR_DPadX",
                "Oculus_GearVr_LIndexTrigger",
                "Oculus_GearVR_LThumbstickX",
                "Oculus_GearVR_LThumbstickY",
                "Oculus_GearVR_RIndexTrigger",
                "Oculus_GearVR_RThumbstickX",
                "Oculus_GearVR_RThumbstickY",
                "Submit",
                "Vertical",
                    };
        }
    }
}