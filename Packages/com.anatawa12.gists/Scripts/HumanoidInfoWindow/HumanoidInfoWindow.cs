/*
 * Humanoid Info Window
 * https://gist.github.com/anatawa12/8375f82dbc751086a32fcd2c626fa09b
 *
 * Open Tools/anatawa12 gists/ShowHumanoidInfo and drop your animator
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_8375f82dbc751086a32fcd2c626fa09b)

using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal class HumanoidInfoWindow : EditorWindow
    {
        public Animator animator;
        public Vector2 scroll;

        [MenuItem("Tools/anatawa12 gists/Humanoid Info Window")]
        static void Create() => GetWindow<HumanoidInfoWindow>("Humanoid Info Window");

        private void OnGUI()
        {
            animator = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);

            if (animator == null)
            {
                EditorGUILayout.HelpBox("Specify Animator", MessageType.Error);
            }
            else if (animator.avatar == null)
            {
                EditorGUILayout.HelpBox("Avatar of Animator is null", MessageType.Error);
            }
            else if (!animator.avatar.isHuman)
            {
                EditorGUILayout.HelpBox("Avatar of Animator is not humanoid", MessageType.Error);
            }
            else
            {
                if (!animator.avatar.isValid)
                    EditorGUILayout.HelpBox("Avatar of Animator is invalid", MessageType.Error);

                scroll = GUILayout.BeginScrollView(scroll);

                for (var bone = HumanBodyBones.Hips; bone < HumanBodyBones.LastBone; bone++)
                {
                    EditorGUILayout.ObjectField($"{bone}", animator.GetBoneTransform(bone), typeof(Transform), true);
                }
                GUILayout.EndScrollView();
            }
        }
    }
}

#endif
