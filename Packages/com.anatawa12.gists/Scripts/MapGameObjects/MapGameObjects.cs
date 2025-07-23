/*
 * MIT License
 * 
 * Copyright (c) 2023 anatawa12
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_e825ec4ee39ae29b64fdcc2f3f07a58c)

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace anatawa12
{
    public class MapGameObjects : EditorWindow
    {
        [MenuItem("Tools/anatawa12 gists/MapGameObjects")]
        public static void Open() => CreateWindow<MapGameObjects>();

        private Transform _mapFromRoot;
        private Transform _mapToRoot;
        private Transform _target;

        private void OnGUI()
        {
            _mapFromRoot = (Transform)EditorGUILayout.ObjectField("map from", _mapFromRoot, typeof(Transform), true);
            _mapToRoot = (Transform)EditorGUILayout.ObjectField("map to", _mapToRoot, typeof(Transform), true);
            _target = (Transform)EditorGUILayout.ObjectField("target", _target, typeof(Transform), true);

            EditorGUI.BeginDisabledGroup(!(_mapFromRoot && _mapToRoot && _target));
            if (GUILayout.Button("Do Map"))
                DoMap();
            EditorGUI.EndDisabledGroup();
        }

        private void DoMap()
        {
            var mapping = new Dictionary<Transform, Transform>();
            CreateMapping(_mapFromRoot, _mapToRoot, mapping);
            DoMapping(_target, mapping);
        }

        private void DoMapping(Transform target, Dictionary<Transform,Transform> mapping)
        {
            //Debug.Log($"Mapping: {target.name}");
            foreach (var component in target.gameObject.GetComponents<Component>())
            {
                var serialized = new SerializedObject(component);
                var p = serialized.GetIterator();
                while (p.Next(true))
                {
                    //Debug.Log($"mapping {p.propertyPath}");
                    if (p.propertyType == SerializedPropertyType.ObjectReference
                        && p.objectReferenceValue is Transform t
                        && mapping.TryGetValue(t, out var mapped))
                    {
                        //Debug.Log($"Mapped: {t.name} -> {mapped.name}");
                        p.objectReferenceValue = mapped;
                    }
                }
                serialized.ApplyModifiedProperties();
            }

            for (var i = 0; i < target.childCount; i++)
                DoMapping(target.GetChild(i), mapping);
        }

        private void CreateMapping(Transform mapFrom, Transform mapTo, IDictionary<Transform, Transform> mapping)
        {
            mapping[mapFrom] = mapTo;

            for (var i = 0; i < mapFrom.childCount; i++)
            {
                var child = mapFrom.GetChild(i);
                var found = mapTo.Find(child.gameObject.name);
                if (found)
                {
                    CreateMapping(child, found, mapping);
                }
                else
                {
                    child.parent = mapTo;
                    i--;
                }
            }
        }
    }
}
#endif
