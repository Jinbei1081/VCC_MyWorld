/*
 * RemovePropertiesFromAnimations
 * A window to remove some property from multiple animations
 * https://gist.github.com/anatawa12/930c08c724af17197a401bcfd580985b
 *
 * Open Tools/antawa12 gists/Remove Properties from Animations and set animations to modify,
 * select properties to remove, and click remove!
 * If you want to copy instead of in-place modification, check 'Copy instead of in-place' and
 * select folder.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_930c08c724af17197a401bcfd580985b)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal class RemovePropertiesFromAnimations : EditorWindow, ISerializationCallbackReceiver
    {
        private const string GIST_NAME = "Remove Properties from Animations";

        [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
        static void Create() => GetWindow<RemovePropertiesFromAnimations>(GIST_NAME);

        private SerializedObject _serialized;
        private SerializedProperty _animations;

        public Vector2 scroll;
        public AnimationClip[] animations = Array.Empty<AnimationClip>();
        public HashSet<CurveBindingId> RemovePros = new HashSet<CurveBindingId>();
        public bool copyInsteadOfInPlace;
        public string outputPath;
        public CurveBindingId[] removeProps = Array.Empty<CurveBindingId>();
        private CurveBindingId[] _allPropsCache = Array.Empty<CurveBindingId>();

        private void OnEnable()
        {
            _serialized = new SerializedObject(this);
            _animations = _serialized.FindProperty(nameof(animations));
            _animations.isExpanded = true;
        }

        private void OnGUI()
        {
            var error = false;

            EditorGUI.BeginChangeCheck();
            _serialized.Update();
            EditorGUILayout.PropertyField(_animations);
            _serialized.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                UpdateAllPropsCache();
            }

            copyInsteadOfInPlace = EditorGUILayout.ToggleLeft("Copy instead of in-place", copyInsteadOfInPlace);
            if (copyInsteadOfInPlace)
            {
                outputPath = EditorGUILayout.TextField("Output Directory", outputPath);
                var folderAsset = EditorGUILayout.ObjectField("OR drag folder here =>", null, typeof(Object), false);
                if (folderAsset)
                {
                    var path = AssetDatabase.GetAssetPath(folderAsset);
                    if (!string.IsNullOrEmpty(path) && !File.Exists(path))
                        outputPath = path;
                }

                var components = outputPath.Split(Path.PathSeparator, Path.AltDirectorySeparatorChar);
                var validPath = components.Length >= 2 
                                && components[0] == "Assets" 
                                && components.All(x => x != "..")
                                && components.All(x => x.Split(Path.GetInvalidFileNameChars()).Length == 1);
                if (!validPath)
                {
                    EditorGUILayout.HelpBox("Output Directory is invalid. \n" +
                                            "It must be in Assets folder and must not contains '..' component", MessageType.Error);
                    error = true;
                }

                if (File.Exists(outputPath))
                {
                    EditorGUILayout.HelpBox("There is an file at specified Output Directory", MessageType.Error);
                    error = true;
                }

                var animationNames = new HashSet<string>(animations.Select(x => x.name));

                if (animationNames.Count != animations.Length)
                {
                    EditorGUILayout.HelpBox("There are animations with same name.", MessageType.Error);
                    error = true;
                }

                if (validPath && Directory.Exists(outputPath))
                {
                    if (animationNames
                        .Select(animationName => $"{outputPath}/{animationName}.anim")
                        .Any(path => File.Exists(path) || Directory.Exists(path)))
                    {
                        EditorGUILayout.HelpBox("There is some animation file with same name in output directory",
                            MessageType.Warning);
                    }
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Properties to freeze:");
            foreach (var curveBindingId in _allPropsCache)
            {
                var exits = RemovePros.Contains(curveBindingId);
                var newExists = EditorGUILayout.ToggleLeft(curveBindingId.ToString(), exits);
                if (newExists != exits)
                {
                    if (newExists) RemovePros.Add(curveBindingId);
                    else RemovePros.Remove(curveBindingId);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUI.BeginDisabledGroup(error);
            if (GUILayout.Button("Remove Properties (Undo not supported)"))
            {
                DoGenerate();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DoGenerate()
        {
            if (copyInsteadOfInPlace)
                Directory.CreateDirectory(outputPath);
            foreach (var animationClip in animations)
                DoGenerate(animationClip);
            AssetDatabase.Refresh();
        }

        private void DoGenerate(AnimationClip animationClip)
        {
            var originalName = animationClip.name;
            if (copyInsteadOfInPlace)
            {
                animationClip = Instantiate(animationClip);
                animationClip.name = originalName;
            }

            var curves = AnimationUtility.GetCurveBindings(animationClip)
                .Where(binding => !RemovePros.Contains(new CurveBindingId(binding)))
                .Select(binding => (binding, AnimationUtility.GetEditorCurve(animationClip, binding)))
                .ToArray();
            
            var referenceCurves = AnimationUtility.GetObjectReferenceCurveBindings(animationClip)
                .Where(binding => !RemovePros.Contains(new CurveBindingId(binding)))
                .Select(binding => (binding, AnimationUtility.GetObjectReferenceCurve(animationClip, binding)))
                .ToArray();

            animationClip.ClearCurves();
            foreach (var (binding, animationCurve) in curves)
                AnimationUtility.SetEditorCurve(animationClip, binding, animationCurve);
            foreach (var (binding, referenceCurve) in referenceCurves)
                AnimationUtility.SetObjectReferenceCurve(animationClip, binding, referenceCurve);

            if (copyInsteadOfInPlace)
            {
                var path = $"{outputPath}/{originalName}.anim";
                //AssetDatabase.DeleteAsset(path)
                AssetDatabase.CreateAsset(animationClip, path);
            }
            EditorUtility.SetDirty(animationClip);
        }

        private void UpdateAllPropsCache()
        {
            _allPropsCache = animations.SelectMany(clip =>
                    AnimationUtility.GetCurveBindings(clip)
                        .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)))
                    .Select(x => new CurveBindingId(x))
                    .Distinct()
                    .ToArray();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            removeProps = RemovePros.ToArray();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RemovePros = new HashSet<CurveBindingId>(removeProps);
            UpdateAllPropsCache();
        }

        [Serializable]
        public struct CurveBindingId
        {
            public string path;
            public string propertyName;

            public CurveBindingId(EditorCurveBinding binding)
            {
                path = binding.path;
                propertyName = binding.propertyName;
            }

            public bool Equals(CurveBindingId other)
            {
                return path == other.path && propertyName == other.propertyName;
            }

            public override bool Equals(object obj)
            {
                return obj is CurveBindingId other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((path != null ? path.GetHashCode() : 0) * 397) ^ (propertyName != null ? propertyName.GetHashCode() : 0);
                }
            }

            public override string ToString() => $"{propertyName} at {path}";
        }
    }
}

#endif
