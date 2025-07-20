/*
 * AlignXAxisOnPlaneWithYRotation
 * Align X axis of multiple GameObjects onto plane of the gameobjects. This is made to align rotation of skirt bone
 * https://gist.github.com/anatawa12/4733d6e695df5dd5a08c599189bba589
 *
 * Click `Tools/anatawa12 gists/AlignXAxisOnPlaneWithYRotation` to open this window.
 *
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_4733d6e695df5dd5a08c599189bba589)

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal class AlignZAxisOnPlaneWithYRotation : EditorWindow
    {
        private const string GIST_NAME = "AlignXAxisOnPlaneWithYRotation";

        [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
        static void Create() => GetWindow<AlignZAxisOnPlaneWithYRotation>(GIST_NAME);

        public Transform[] transforms;

        private SerializedObject _serializedObject;
        private SerializedProperty _transforms;

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _transforms = _serializedObject.FindProperty(nameof(transforms));
        }

        /*
        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        private void OnDestroy() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sceneview)
        {
            var crossSum = CrossSum();
            if (crossSum != Vector3.zero)
            {
                var center = transforms.Select(x => x.position).Aggregate(Vector3.zero, (x, y) => x + y) /
                             transforms.Length;

                Handles.DrawLine(center, center + crossSum.normalized);

                foreach (var transform in transforms)
                {
                    var rotation = transform.rotation;
                    var yDir = rotation * new Vector3(0, 1, 0);
                    var zDir = rotation * new Vector3(0, 0, 1);
                    var newDir = Vector3.Cross(crossSum, yDir);
                    var angle = Vector3.SignedAngle(zDir, newDir, yDir);

                    var transformPosition = transform.position;
                    Handles.DrawLine(transformPosition, transformPosition + newXDir.normalized);
                    Handles.DrawLine(transformPosition, transformPosition + xDir.normalized);
                    Handles.Label(transformPosition, $"{angle}");
                }
            }
        }
        // */

        private IEnumerable<Transform> Transforms => transforms.Where(x => x);

        private void OnGUI()
        {
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_transforms);
            _serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Align"))
            {
                var crossSum = CrossSum();

                if (crossSum == Vector3.zero)
                {
                    EditorUtility.DisplayDialog("err", "cannot find plane for X axis", "ok");
                    return;
                }

                foreach (var transform in Transforms)
                {
                    using (var serializedTransform = new SerializedObject(transform))
                    {
                        var rotation = transform.rotation;
                        var yDir = rotation * new Vector3(0, 1, 0);
                        var zDir = rotation * new Vector3(0, 0, 1);
                        var newDir = Vector3.Cross(crossSum, yDir);
                        var angle = Vector3.SignedAngle(zDir, newDir, yDir);

                        var rotationProperty = serializedTransform.FindProperty("m_LocalRotation");
                        rotationProperty.quaternionValue *= Quaternion.Euler(0, angle, 0);
                        serializedTransform.ApplyModifiedProperties();
                    }
                }
            }
        }

        private Vector3 CrossSum()
        {
            var difference = ZipWithNext(Transforms).Select(x => x.Item1.position - x.Item2.position);
            var crossProducts = ZipWithNext(difference).Select(x => Vector3.Cross(x.Item1, x.Item2));
            var crossSum = crossProducts.Aggregate(Vector3.zero, (current, crossProduct) => current + crossProduct);
            return crossSum;
        }

        private IEnumerable<(T, T)> ZipWithNext<T>(IEnumerable<T> source)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext()) yield break;
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    yield return (prev, current);
                    prev = current;
                }
            }
        }
    }
}

#endif
