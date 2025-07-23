/*
 * SetDirtyRecursively
 * Set dirty all components on selected GameObject to avoid reference to prefab asset.
 * https://gist.github.com/anatawa12/ecf33339c315f259cee62b304910fe43
 *
 * Left click object and select `SetDirty Recursively`
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_TODO)

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal static class SetDirtyRecursively
    {
        private const string SetDirtyRecursivelyMenu = "GameObject/SetDirty Recursively";
        [MenuItem(SetDirtyRecursivelyMenu, false, -10)]
        private static void Execute()
        {
            foreach (var component in Selection.activeGameObject.GetComponents<Component>())
                EditorUtility.SetDirty(component);
        }

        [MenuItem(SetDirtyRecursivelyMenu, true, -10)]
        private static bool Validate()
        {
            return Selection.objects.All(x => x && x is GameObject);
        }
    }
}

#endif
