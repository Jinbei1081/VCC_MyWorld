/*
 * GenerateMeshWithBackFace
 * https://gist.github.com/anatawa12/4c900d5c15050fb5bdc0f9d027962183
 *
 * Left click Mesh and select Generate Mesh with backface to generate new mesh with backface
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_4c900d5c15050fb5bdc0f9d027962183)

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;


namespace anatawa12.gists
{
    internal static class GenerateMeshWithBackFace
    {
        private const string MenuItemName = "Assets/Generate Mesh with backface";

        [MenuItem(MenuItemName)]
        private static void Action()
        {
            var mesh = (Mesh)Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(mesh);
            var dir = "";
            if (!string.IsNullOrEmpty(assetPath) && assetPath.LastIndexOf('/') > 1)
                dir = assetPath.Substring(0, assetPath.LastIndexOf('/'));

            var path = EditorUtility.SaveFilePanelInProject("Save Mesh with backface", 
                $"{mesh.name} with backface.asset", "asset", 
                "Please enter a file name for generated mesh", dir);

            if (string.IsNullOrEmpty(path)) return;

            var generated = Generate(mesh);
            AssetDatabase.CreateAsset(generated, path);
            Selection.activeObject = generated;
        }

        [MenuItem(MenuItemName, validate = true)]
        private static bool ValidateAction()
        {
            return Selection.activeObject is Mesh;
        }

        private static Mesh Generate(Mesh mesh)
        {
            {
                var result = Object.Instantiate(mesh);
                result.name = $"{mesh.name} with backface";
                mesh = result;
            }

            if (mesh.triangles.Length == 0) return mesh;
            if (mesh.subMeshCount == 0) return mesh;

            SubMeshDescriptor[] subMeshInfos = new SubMeshDescriptor[mesh.subMeshCount];

            for (var i = 0; i < mesh.subMeshCount; i++)
                subMeshInfos[i] = mesh.GetSubMesh(i);

            int totalMaxTriangleCount = 0;
            foreach (var subMeshInfo in subMeshInfos)
                totalMaxTriangleCount += subMeshInfo.indexCount * 2;

            var srcTriangles = mesh.triangles;
            var dstTriangles = new int[totalMaxTriangleCount];
            var dstIndex = 0;

            for (var i = 0; i < subMeshInfos.Length; i++)
            {
                var info = subMeshInfos[i];

                Assert.AreEqual(MeshTopology.Triangles, info.topology);

                var newIndexStart = dstIndex;

                for (var j = 0; j + 2 < info.indexCount; j += 3)
                {
                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 0];
                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 1];
                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 2];

                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 0];
                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 2];
                    dstTriangles[dstIndex++] = srcTriangles[info.indexStart + j + 1];
                }

                subMeshInfos[i] = new SubMeshDescriptor(newIndexStart, info.indexCount * 2);
            }

            Assert.AreEqual(subMeshInfos.Length, dstIndex);

            mesh.triangles = dstTriangles;
            mesh.subMeshCount = subMeshInfos.Length;
            for (var i = 0; i < subMeshInfos.Length; i++)
                mesh.SetSubMesh(i, subMeshInfos[i]);

            return mesh;
        }
    }
}
#endif
