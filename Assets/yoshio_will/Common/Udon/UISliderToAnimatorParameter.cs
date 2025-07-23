
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UISliderToAnimatorParameter : UdonSharpBehaviour
    {
        [SerializeField] private Animator Animator;
        [SerializeField] private string ParameterName;
        [SerializeField] private string ParameterNameInt;
        [SerializeField] private float Magnifier = 1;
        [SerializeField] private Slider Slider;

        private int _animParameterName, _animParameterInt;

        void Start()
        {
            _animParameterName = Animator.StringToHash(ParameterName);
            _animParameterInt = Animator.StringToHash(ParameterNameInt);
        }

        public void OnValueChanged()
        {
            float value = Slider.value * Magnifier;
            Animator.SetFloat(_animParameterName, value);
            Animator.SetInteger(_animParameterInt, Mathf.FloorToInt(value));
        }
    }
}