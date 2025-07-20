/*
 * Merge Animation Clip
 * https://gist.github.com/anatawa12/f7476d2d727bc43d86121f6a3337d2c3
 *
 * Micro tool to multiple animation clip into one.
 * Click `Tools/anatawa12 gists/Merge Animation Clip` to open the merge window.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_f7476d2d727bc43d86121f6a3337d2c3)

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal class MergeAnimationClip : EditorWindow
    {
        [MenuItem("Tools/anatawa12 gists/Merge Animation Clip")]
        private static void ShowWindow() => GetWindow<MergeAnimationClip>("Merge Animation Clip", true);

        [SerializeField] private AnimationClip[] clips;

        // editor
        private SerializedObject _serializedObject;
        private SerializedProperty _clipsProp;

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _clipsProp = _serializedObject.FindProperty("clips");
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_clipsProp);
            _serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Merge Animation Clips!"))
                MergeButton();
        }

        private void MergeButton()
        {
            if (clips.Count(x => x) == 0)
            {
                EditorUtility.DisplayDialog("Error", "No clips are specified", "OK");
                return;
            }
            var path = EditorUtility.SaveFilePanelInProject("Merge Animation Clips", "merged.anim", "anim",
                "save merged animation clip");
            if (string.IsNullOrEmpty(path)) return;

            var merged = DoMerge();

            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(merged, path);
        }

        private AnimationClip DoMerge()
        {
            var floatSources = new Dictionary<EditorCurveBinding, AnimationClip>();
            var objectSources = new Dictionary<EditorCurveBinding, AnimationClip>();

            foreach (var clip in clips)
            {
                if (!clip) continue;
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    floatSources[binding] = clip;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    objectSources[binding] = clip;
            }

            var result = new AnimationClip();

            foreach (var kvp in floatSources)
            {
                var binding = kvp.Key;
                var clip = kvp.Value;

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(result, binding, curve);
            }

            foreach (var kvp in objectSources)
            {
                var binding = kvp.Key;
                var clip = kvp.Value;

                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                AnimationUtility.SetObjectReferenceCurve(result, binding, curve);
            }

            return result;
        }
    }
}

#endif
