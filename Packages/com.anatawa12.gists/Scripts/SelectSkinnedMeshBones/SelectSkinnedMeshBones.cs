/*
 * Select GameObjects used by SkinnedMeshRenderers
 * https://gist.github.com/anatawa12/94d6fd4272025fd26962476100a20ff0
 *
 * Delete EditorOnly On Play before Avatar Optimizer or Modular Avatar.
 * Add this component to any gameobject in your scene
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

#if (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_94d6fd4272025fd26962476100a20ff0)
#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.gists
{
    internal static class SelectSkinnedMeshBones
    {
        [MenuItem("GameObject/anatawa12 gists/Select SkinnedMesh Bones", false, 49)]
        public static void Menu()
        {
            var avatar = Selection.activeGameObject;

            Selection.objects = avatar
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(x => x.bones)
                .Where(x => x)
                .Select(x => x.gameObject)
                .ToArray<Object>();
        }
    }
}
#endif
#endif
