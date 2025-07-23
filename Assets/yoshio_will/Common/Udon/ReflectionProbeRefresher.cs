
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ReflectionProbeRefresher : UdonSharpBehaviour
    {
        [SerializeField] private ReflectionProbe[] ReflectionProbes;
        [SerializeField] private float RefreshInterval = 10;
        [SerializeField] private float FirstRenderDelay = 5;

        private float _nextRefleshTime, _firstRenderTime;
        private int _index;
        private bool _isFirstRenderFinished = false;

        private void Start()
        {
            _firstRenderTime = Time.time + FirstRenderDelay;
        }

        private void Update()
        {
            ReflectionProbe probe;
            GameObject gobj;

            // 初回の全部レンダリング
            if (!_isFirstRenderFinished)
            {
                if (Time.time < _firstRenderTime) return;
                for(int idx = 0; idx < ReflectionProbes.Length; idx++)
                {
                    probe = ReflectionProbes[idx];
                    if (probe)
                    {
                        gobj = probe.gameObject;
                        if (gobj.activeInHierarchy) probe.RenderProbe();
                    }
                }

                _isFirstRenderFinished = true;
                return;
            }

            // 2回目以降
            if (Time.time < _nextRefleshTime) return;

            _index++;
            if (_index >= ReflectionProbes.Length) _index = 0;

            int tries = 0;
            while (tries < ReflectionProbes.Length)
            {
                probe = ReflectionProbes[_index];
                if (probe)
                {
                    gobj = probe.gameObject;
                    if (gobj.activeInHierarchy) break;
                }
                _index++;
                tries++;
                if (_index >= ReflectionProbes.Length) _index = 0;
            }

            probe = ReflectionProbes[_index];
            if (!probe) return;
            gobj = probe.gameObject;
            if (!gobj.activeInHierarchy) return;
            
            probe.RenderProbe();

            _nextRefleshTime = Time.time + RefreshInterval;
        }
    }
}