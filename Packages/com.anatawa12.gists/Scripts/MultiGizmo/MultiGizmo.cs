/*
 * MultiGizmo
 * Tool to show position gizmo of multiple objects
 * https://gist.github.com/anatawa12/6af2d5e41b0941aee1ed3fd7e0860ac0
 *
 * Open `Tools/anatawa12 gists/MultiGizmo` and select objects to show gizmo
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_6af2d5e41b0941aee1ed3fd7e0860ac0)

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal class MultiGizmo : EditorWindow
    {
        private const string GIST_NAME = "MultiGizmo";

        [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
        static void Create() => GetWindow<MultiGizmo>(GIST_NAME);

        public Mode mode = Mode.Position;
        public float scale = 1;
        public Transform[] transforms;

        private SerializedObject _serializedObject;
        private SerializedProperty _transforms;
        private SerializedProperty _scale;
        private SerializedProperty _mode;

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _mode = _serializedObject.FindProperty(nameof(mode));
            _transforms = _serializedObject.FindProperty(nameof(transforms));
            _scale = _serializedObject.FindProperty(nameof(scale));
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        private void OnDestroy() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sceneview)
        {
            switch (mode)
            {
                case Mode.Position:
                    OnSceneGUIPosition();
                    break;
                case Mode.DotAndLine:
                    OnSceneGUIDotAndLine();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnSceneGUIPosition()
        {
            var matrix = Handles.matrix;
            try
            {
                foreach (var transform in transforms ?? Array.Empty<Transform>())
                {
                    if (!transform) continue;
                    Handles.matrix = matrix;
                    Handles.matrix *= Matrix4x4.Translate(transform.position);
                    Handles.matrix *= Matrix4x4.Rotate(transform.rotation);
                    Handles.matrix *= Matrix4x4.Scale(new Vector3(scale, scale, scale));
                    if (Event.current.type == EventType.Repaint)
                        Handles.PositionHandle(Vector3.zero, Quaternion.identity);
                }
            }
            finally
            {
                Handles.matrix = matrix;
            }
        }

        private void OnSceneGUIDotAndLine()
        {
            var matrix = Handles.matrix;
            try
            {
                foreach (var transform in transforms ?? Array.Empty<Transform>())
                {
                    if (!transform) continue;;
                    var position1 = transform.position;
                    if (Event.current.type == EventType.Repaint)
                    {
                        Handles.DotHandleCap(0, position1, transform.rotation, HandleUtility.GetHandleSize(position1) * 0.1f, EventType.Repaint);
                    }
                    if (transform.parent)
                    {
                        Handles.DrawLine(transform.parent.position, position1);
                    }
                }
            }
            finally
            {
                Handles.matrix = matrix;
            }
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_mode);
            if (mode == Mode.Position)
                EditorGUILayout.PropertyField(_scale);
            EditorGUILayout.PropertyField(_transforms);
            _serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }

        public enum Mode
        {
            Position,
            DotAndLine
        }
    }
}

#endif
