/*
 * AndroidOnlyCheck
 * https://gist.github.com/anatawa12/5f847a1692fb30c2c9f00a47d50243ad
 *
 * A VRCSDKPreprocessAvatarCallback which prevents PC builds.
 * In Android build, this script does nothing. In PC build, this script will show dialog & cancel the build.
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

#if (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_5f847a1692fb30c2c9f00a47d50243ad)

#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_EDITOR && !UNITY_ANDROID
using VRC.Core;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace anatawa12.gists
{
    [AddComponentMenu("anatawa12 gists/Allow PC Uploads")]
    internal class AndroidOnlyCheck : MonoBehaviour
    {
        // everything if empty array.
        public string[] blueprintIds;
        
#if UNITY_EDITOR && !UNITY_ANDROID
        internal class AndroidOnlyCheckCallback : IVRCSDKPreprocessAvatarCallback
        {
            public int callbackOrder => int.MinValue;

            public bool OnPreprocessAvatar(GameObject avatarGameObject)
            {
                if (avatarGameObject.GetComponent<AndroidOnlyCheck>() is AndroidOnlyCheck component)
                {
                    var blueprintIds = component.blueprintIds;
                    DestroyImmediate(component);
                    // if empty, PC build is allowed
                    if (blueprintIds.Length == 0) return true;
                    // if blueprintId is matched, PC build is allowed.
                    if (avatarGameObject.GetComponent<PipelineManager>() is PipelineManager manager
                        && blueprintIds.Contains(manager.blueprintId))
                        return true;
                }
                EditorUtility.DisplayDialog("PC Build Detected!", 
                    "It's not OK to publish this branch to PC!\n" +
                    "If you actually want to upload this avatar for PC, add 'Allow PC Uploads' Component!",
                    "OK, cancel Build");
                return false;
            }
        }

        [InitializeOnLoad]
        static class PatchWhitelistedComponents
        {
            internal static readonly bool PATCH_OK;

            static PatchWhitelistedComponents()
            {
                try
                {
                    ValidationUtils.WhitelistedTypes("avatar-sdk3", (IEnumerable<Type>)null)
                        .Add(typeof(AndroidOnlyCheck));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
#endif
#if UNITY_EDITOR
        [CustomEditor(typeof(AndroidOnlyCheck))]
        class AndroidOnlyCheckEditor : Editor
        {
            static class Styles
            {
                public static GUIStyle label = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };
            }

            private SerializedProperty _blueprintIds;

            private void OnEnable()
            {
                _blueprintIds = serializedObject.FindProperty("blueprintIds");
            }

            public override void OnInspectorGUI()
            {
                GUILayout.Label("You've installed AndroidOnlyCheck by anatawa12.", Styles.label);
                GUILayout.Label("This tool prevents you to uploading your avatars to PC (unexpectedly)", Styles.label);
                GUILayout.Label("Adding this component allows you to upload this avatar for PC exceptionally.",
                    Styles.label);
                GUILayout.Label("If Blueprint Ids is empty, all uploads to PC is allowed.", Styles.label);
                GUILayout.Label("If Blueprint Ids is not empty, " +
                                "uploading to PC is allowed only if blueprintId is listed.", Styles.label);

                _blueprintIds.isExpanded = true; // always expanded
                EditorGUILayout.PropertyField(_blueprintIds);
            }
        }
#endif
    }
}

#endif
