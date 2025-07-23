// MIT License
// 
// Copyright (c) 2023 anatawa12
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_379c4d828c2a0add4d623f8668209cbc)
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Dynamics;

namespace anatawa12.gists
{
    public static class PhysBoneEditorUtilities
    {
        [MenuItem("Tools/anatawa12 gists/Select Unused PhysBone Colliders")]
        public static void FindColliders()
        {
            var active = SceneManager.GetActiveScene();
            var colliders = new HashSet<VRCPhysBoneColliderBase>(active.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<VRCPhysBoneColliderBase>()));
            foreach (var collider in active.GetRootGameObjects()
                         .SelectMany(x => x.GetComponentsInChildren<VRCPhysBoneBase>())
                         .SelectMany(x => x.colliders))
                colliders.Remove(collider);

            if (colliders.Count == 0)
            {
                EditorUtility.DisplayDialog("Not Found", "No Unused Colliders Found", "OK");
            }
            else
            {
                Selection.objects = colliders.Select(x => x.gameObject).ToArray<Object>();
            }
        }

        [MenuItem("CONTEXT/VRCPhysBoneCollider/Select PhysBones using This Collider")]
        public static void FindColliderUsers(MenuCommand menuCommand)
        {
            var collider = menuCommand.context as VRCPhysBoneColliderBase;
            if (collider == null) return;

            var users = collider.gameObject.scene
                .GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<VRCPhysBoneBase>())
                .Where(x => x.colliders.Contains(collider))
                .Select(x => x.gameObject)
                .ToArray<Object>();

            if (users.Length == 0)
            {
                EditorUtility.DisplayDialog("Not Found", "No PhysBones using this Collider found", "OK");
            }
            else
            {
                Selection.objects = users;
            }
        }
    }
}
#endif
