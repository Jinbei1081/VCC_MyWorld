/*
 * Manual Bake Preprocess Avatar Callbacks
 * Calls IVRCSDKPreprocessAvatarCallback.OnPreprocessAvatar on selected Avatar
 * https://gist.github.com/anatawa12/9e2bf687b1dbc23c78d513bfa96f07d8
 *
 * Left-click on an Avatar in the Hierarchy and select "Manual Bake Preprocess Avatar Callbacks" from the context menu.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_9e2bf687b1dbc23c78d513bfa96f07d8)

using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Anatawa12.gists
{
    internal class ManualBakePreprocessAvatarCallbacks
    {
        [MenuItem("GameObject/Manual Bake Preprocess Avatar Callbacks", false, 0)]
        private static void ManualBakeFullAvatar()
        {
            var avatar = Selection.activeGameObject;
            if (avatar == null)
            {
                EditorUtility.DisplayDialog("Error", "No GameObject selected", "OK");
                return;
            }
            
            var originalName = avatar.name;
            avatar = Object.Instantiate(avatar);

            VRCBuildPipelineCallbacks.OnPreprocessAvatar(avatar);

            avatar.name = originalName + " (Baked)";
            var position = avatar.transform.localPosition;
            position.z += 1;
            avatar.transform.localPosition = position;
        }

        [MenuItem("GameObject/Manual Bake Full Avatar", true)]
        private static bool ValidateManualBakeFullAvatar() =>
            Selection.activeGameObject != null && 
            Selection.activeGameObject.GetComponent<VRC.SDKBase.VRC_AvatarDescriptor>();
    }
}

#endif
