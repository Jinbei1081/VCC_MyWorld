/*
 * Combine Assets To Single File
 * https://gist.github.com/anatawa12/1b304b398945ec547912239328708b6b
 *
 * Combines multiple assets into a single file.
 * Select Multiple Files and Right Click -> Assets -> Combine Assets To Single File
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_1b304b398945ec547912239328708b6b)

using System.IO;
using UnityEditor;

namespace Anatawa12.gists
{
    internal class CombineAssetsToSingleFile
    {
        [MenuItem("Assets/Combine Assets To Single File", false, 100)]
        private static void CombineAssets()
        {
            var objects = Selection.objects;
            if (objects.Length < 1)
            {
                EditorUtility.DisplayDialog("Error", "No Files selected", "OK");
                return;
            }

            var rootPath = AssetDatabase.GetAssetPath(objects[0]);
            var dirName = Path.GetDirectoryName(rootPath);

            var file = EditorUtility.SaveFilePanelInProject(
                "Save combined assets",
                "CombinedAssets",
                "asset",
                "Save Assets to Single File",
                dirName);

            if (string.IsNullOrEmpty(file))
                return;

            AssetDatabase.RemoveObjectFromAsset(objects[0]);
            AssetDatabase.CreateAsset(objects[0], file);

            for (var i = 1; i < objects.Length; i++)
            {
                AssetDatabase.RemoveObjectFromAsset(objects[i]);
                AssetDatabase.AddObjectToAsset(objects[i], file);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif
