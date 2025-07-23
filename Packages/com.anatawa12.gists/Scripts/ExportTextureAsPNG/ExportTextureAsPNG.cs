/*
 * Export Texture as PNG
 * Save existing texture asset as PNG
 * https://gist.github.com/anatawa12/f52ad0643f7db137a99d207428f44dc6
 *
 * Click `Assets/Export Texture as PNG` when selecting texture asset
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_f52ad0643f7db137a99d207428f44dc6)

using System.IO;
using UnityEditor;
using UnityEngine;

namespace anatawa12.gists
{
    internal class ExportTextureAsPNG
    {
        [MenuItem("Assets/Export Texture as PNG")]
        private static void DoExportTextureAsPNG()
        {
            var texture = Selection.activeObject as Texture2D;
            if (!texture)
            {
                EditorUtility.DisplayDialog("Error", "Please select a texture", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Save texture as PNG", "", texture.name, "png");
            if (string.IsNullOrEmpty(path))
                return;

            if (!texture.isReadable)
            {
                // make it readable
                var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, 
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                try
                {
                    Graphics.Blit(texture, renderTexture);
                    var previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = renderTexture;
                        var readable = new Texture2D(texture.width, texture.height);
                        readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                        readable.Apply();
                        texture = readable;
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }

            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        [MenuItem("Assets/Export Texture as PNG", true)]
        private static bool ValidateExportTextureAsPNG() => Selection.activeObject is Texture2D;
    }
}

#endif
