/*
 * EditorScreenshotTakeTool
 * https://gist.github.com/anatawa12/875d0776305b771ba7ee74c656f082f6
 *
 * A tool to take screen shot of an Editor.
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

#if (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_875d0776305b771ba7ee74c656f082f6)
 
#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal static class EditorScreenshotTakeTool
    {
        class EditorScreenshotTakeWindow : EditorWindow
        {
            [MenuItem("Tools/anatawa12 gists/EditorScreenshotTakeTool")]
            public static void Open() => GetWindow<EditorScreenshotTakeWindow>("EditorScreenshotTakeTool");

            public Object target;
            public int width = 200;
            private SerializedObject _obj;
            private SerializedProperty _targetProp;
            private SerializedProperty _widthProp;

            private void OnEnable()
            {
                _obj = new SerializedObject(this);
                _targetProp = _obj.FindProperty(nameof(target));
                _widthProp = _obj.FindProperty(nameof(width));
            }

            private void OnGUI()
            {
                _obj.Update();
                EditorGUILayout.PropertyField(_targetProp);
                EditorGUILayout.PropertyField(_widthProp);
                _obj.ApplyModifiedProperties();
                if (GUILayout.Button("Take ScreenShot"))
                {
                    var path = EditorUtility.SaveFilePanel("Screen Shot",
                        "", "screenshot.png", "png");
                    if (!string.IsNullOrEmpty(path))
                        WindowForShot.Take(width, target, path);
                }
            }
        }

        class WindowForShot : EditorWindow
        {
            private VisualElement element;
            private string path;

            public static void Take(int width, Object target, string path)
            {
                var window = CreateInstance<WindowForShot>();
                window.minSize = new Vector2(width, window.minSize.y);
                window.maxSize = new Vector2(width, window.maxSize.y);
                window.path = path;
                window.ShowPopup();
                window.Focus();
                window.Take(target);
            }

            private void CreateGUI()
            {
                rootVisualElement.Add(new IMGUIContainer(() =>
                {
                    if (GUILayout.Button("Close"))
                        Close();
                }));
            }

            void Take(Object target)
            {
                rootVisualElement.Clear();
                element = new VisualElement();

                element.Add(new IMGUIContainer(() =>
                {
                    EditorGUILayout.InspectorTitlebar(true, target);
                }));

                var inspector = new InspectorElement(target);
                inspector.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
                element.Add(inspector);

                rootVisualElement.Add(element);

                rootVisualElement.Add(new IMGUIContainer(() =>
                {
                    if (GUILayout.Button("Take"))
                        TakeShot();
                }));
            }

            void GeometryChangedCallback(GeometryChangedEvent evt)
            {
                if (position.height - 50 <= element.resolvedStyle.height)
                {
                    var position = this.position;
                    position.height = element.resolvedStyle.height + 200;
                    this.position = position;
                }
            }

            void TakeShot()
            {
                var position = this.position.position;
                var width = (int)element.resolvedStyle.width;
                var height = (int)element.resolvedStyle.height;
                var pixels = InternalEditorUtility.ReadScreenPixel(position, width,height);
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Close();
            }
        }
    }
}
#endif
#endif
