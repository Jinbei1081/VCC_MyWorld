/*
 * CreateIdleAnimation
 * Creates Idle face animation from SkinnedMeshRenderer
 * https://gist.github.com/anatawa12/667a1b7a892f121a7572bdec325442ea
 *
 * Click `Tools/anatawa12 gists/FindPhysBoneAffectedTransforms` to open this window.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_667a1b7a892f121a7572bdec325442ea)

using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal static class CreateIdleAnimation
    {
        private const string GIST_NAME = "CreateIdleAnimation";
        private const string MENU = "CONTEXT/SkinnedMeshRenderer/anatawa12 gists/" + GIST_NAME;

        [MenuItem(MENU, true, 49)]
        private static bool Check()
        {
            return Selection.activeGameObject;
        }

        [MenuItem(MENU, false, 49)]
        private static void ExecuteManualBake(MenuCommand menuCommand)
        {
            var renderer = menuCommand.context as SkinnedMeshRenderer;
            if (!renderer) return;
            var mesh = renderer.sharedMesh;
            if (!mesh) return;

            var path = EditorUtility.SaveFilePanelInProject("Save Animation", "idle.anim", "anim", "Save Animation");
            if (string.IsNullOrEmpty(path)) return;

            var anim = new AnimationClip();

            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                var name = mesh.GetBlendShapeName(i);
                var value = renderer.GetBlendShapeWeight(i);
                AnimationUtility.SetEditorCurve(anim,
                    renderer.name, typeof(SkinnedMeshRenderer), $"blendShape.{name}",
                    AnimationCurve.Constant(0, 1, value));
            }

            AssetDatabase.CreateAsset(anim, path);
        }
    }
}

#endif

