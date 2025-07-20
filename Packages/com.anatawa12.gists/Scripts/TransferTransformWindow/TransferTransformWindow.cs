/*
 * Transfer Transform Window
 * https://gist.github.com/anatawa12/b8799da5d3131e4020f414439a4ea037
 *
 * A window to copy transform info recursively
 * `Tools/anatawa12 gist/Transfer Transform`
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_b8799da5d3131e4020f414439a4ea037)
using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal class TransferTransformWindow : EditorWindow
    {
        [SerializeField] private Transform source;
        [SerializeField] private Transform dest;

        [MenuItem("Tools/anatawa12 gists/Transfer Transform")]
        private static void Open() => CreateWindow<TransferTransformWindow>();

        private void OnGUI()
        {
            source = EditorGUILayout.ObjectField("source", source, typeof(Transform), true) as Transform;
            dest = EditorGUILayout.ObjectField("dest", dest, typeof(Transform), true) as Transform;
            using (new EditorGUI.DisabledScope(!(source && dest)))
            {
                if (GUILayout.Button("Do Transform"))
                {
                    Transfer(source, dest);
                }
            }
        }

        private static void Transfer(Transform source, Transform dest)
        {
            if (dest.localPosition != source.localPosition){
                dest.localPosition = source.localPosition;
                Undo.RecordObject(dest, "Transfer Transform");
            }
            if (dest.localRotation != source.localRotation){
                dest.localRotation = source.localRotation;
                Undo.RecordObject(dest, "Transfer Transform");
            }
            if (dest.localScale != source.localScale){
                dest.localScale = source.localScale;
                Undo.RecordObject(dest, "Transfer Transform");
            }

            for (var i = 0; i < source.childCount; i++)
            {
                var sourceChild = source.GetChild(i);
                var destChild = dest.Find(sourceChild.name);
                if (destChild)
                {
                    Transfer(sourceChild, destChild);
                }
            }
        }
    }
}

#endif
