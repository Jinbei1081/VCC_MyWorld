/*
 * CreateEmptyVRChatAvatar
 * Fast way to create a new empty VRChat avatar.
 * https://gist.github.com/anatawa12/2acd16939ef597895d8298ae97ba4122
 *
 * Left Click hierarchy and select `Create Empty VRChat Avatar` to create a new empty VRChat avatar.
 *
 * Empty VRChat Avatr will have:
 * - VRC Avatar Descriptor
 * - Pipeline Manager
 * - Animator
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_2acd16939ef597895d8298ae97ba4122)

using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace anatawa12.gists
{
    internal class CreateEmptyVRChatAvatar
    {
        [MenuItem("GameObject/Create Empty VRChat Avatar", false, 0, secondaryPriority = 3)]
        public static void Create(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("GameObject", typeof(Animator), typeof(VRCAvatarDescriptor), typeof(PipelineManager)), parent);
        }

        internal static void Place(GameObject go, GameObject parent, bool ignoreSceneViewPosition = true)
        {
            try
            {
                // Use Unity-internal method to place the object in the scene.
                // This is the same method that is used when creating objects through the UI.
                typeof(Editor).Assembly
                    .GetType("UnityEditor.GOCreationCommands")
                    .GetMethod("Place", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                        null,
                        new[] {typeof(GameObject), typeof(GameObject), typeof(bool)},
                        null)!
                    .Invoke(null, new object[] {go, parent, ignoreSceneViewPosition});
            }
            catch
            {
                // fallback: if parent is selected, use it as parent
                if (parent != null)
                {
                    var transform = go.transform;
                    Undo.SetTransformParent(transform, parent.transform, "Reparenting");
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    go.layer = parent.transform.gameObject.layer;
                }

                GameObjectUtility.EnsureUniqueNameForSibling(go);
                Undo.SetCurrentGroupName("Create " + go.name);
                Selection.activeGameObject = go;
            }
        }
    }
}

#endif
