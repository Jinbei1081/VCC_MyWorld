
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FixedUpdateRateMeasurement : UdonSharpBehaviour
    {
        [SerializeField] private bool IsContinuous = false;
        private int _state = 0;
        private float _timer, _timeMeasureStart, _timeMeasureEnd;
        private int _frames;
        private TextMeshProUGUI _text;

        const float InitialCoolDown = 10;
        const float MeasureDuration = 10;
        const float ContinuousInterval = 0.333f;

        void Start()
        {
            _state = 0;
            if (IsContinuous) _state = 2;
            _timer = Time.time + InitialCoolDown;
            _timeMeasureStart = Time.time;
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void FixedUpdate()
        {
            if (_state < 0) return;

            switch(_state)
            {
                case 0:
                    if (Time.time > _timer)
                    {
                        _state = 1;
                        _frames = 0;
                        _timer = Time.time + MeasureDuration;
                        _timeMeasureStart = Time.time;
                        _timeMeasureEnd = Time.time;
                    }
                    break;

                case 1:
                    _frames++;
                    _timeMeasureEnd = Time.time;
                    if (Time.time > _timer)
                    {
                        _state = -1;
                    }
                    break;

                case 2:
                    _frames++;
                    return;
            }

            float timeDiff = (_timeMeasureEnd - _timeMeasureStart);
            if (timeDiff == 0) return;
            float fps = _frames / timeDiff;

            _text.text = string.Format("{0:0.0}", fps);

            if (_state < 0) enabled = false;
        }

        private void Update()
        {
            if (!IsContinuous) return;

            if (Time.time > _timer)
            {
                float timeDiff = (Time.time - _timeMeasureStart);
                if (timeDiff == 0) return;
                float fps = _frames / timeDiff;

                _text.text = string.Format("{0:0.0}", fps);

                _timeMeasureStart = Time.time;
                _frames = 0;
                _timer = Time.time + ContinuousInterval;
            }
        }
    }
}