/*
 * Show Assets in Packages by Default in Selector Window
 * https://gist.github.com/anatawa12/b128ca8fc819b9684f3ed15be1f76e8c
 *
 * Copy this cs file to anywhere in your asset folder is the only step to install this tool.
 *
 * In Unity, the selector window will not show assets in packages by default.
 * This tool will change the default behavior to show assets in packages by default.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_b128ca8fc819b9684f3ed15be1f76e8c)

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    class ShowAssetsInPackagesByDefaultInSelector
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            try
            {
                EditorApplication.update += new ShowAssetsInPackagesByDefaultInSelector().Update;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private ShowAssetsInPackagesByDefaultInSelector()
        {
            const BindingFlags instancePrivate = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags staticPrivate = BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;

            var objectSelectorType = Type.GetType("UnityEditor.ObjectSelector, UnityEditor")
                                     ?? throw new Exception("UnityEditor.ObjectSelector not found");
            if (objectSelectorType.IsAssignableFrom(typeof(EditorWindow)))
                throw new Exception("UnityEditor.ObjectSelector is not EditorWindow");

            _skipHiddenPackages = objectSelectorType.GetField("m_SkipHiddenPackages", instancePrivate)
                                  ?? throw new Exception("ObjectSelector.m_SkipHiddenPackages not found");
            if (_skipHiddenPackages.FieldType != typeof(bool))
                throw new Exception("ObjectSelector.m_SkipHiddenPackages is not bool");

            _filterSettingsChanged = objectSelectorType.GetMethod("FilterSettingsChanged", instancePrivate,
                                         null, Type.EmptyTypes, null)
                                     ?? throw new Exception("ObjectSelector.FilterSettingsChanged not found");
            if (_filterSettingsChanged.ReturnType != typeof(void))
                throw new Exception("ObjectSelector.FilterSettingsChanged is not void");

            var sharedInstanceProperty = objectSelectorType.GetProperty("get",
                                             staticPrivate)
                                         ?? throw new Exception("ObjectSelector.get not found");
            if (sharedInstanceProperty.PropertyType != objectSelectorType)
                throw new Exception("ObjectSelector.get is not ObjectSelector");
            _sharedInstanceGetter = sharedInstanceProperty.GetMethod
                                    ?? throw new Exception("ObjectSelector.get does not have getter");

            _parent = typeof(EditorWindow).GetField("m_Parent", instancePrivate)
                      ?? throw new Exception("EditorWindow.m_Parent not found");
            if (_parent.FieldType.IsAssignableFrom(typeof(Object)))
                throw new Exception("EditorWindow.m_Parent is not EditorWindow");
        }

        private bool _visibleLastTick;
        private EditorWindow _cachedWindow;
        private readonly FieldInfo _parent;
        private readonly FieldInfo _skipHiddenPackages;
        private readonly MethodInfo _filterSettingsChanged;
        private readonly MethodInfo _sharedInstanceGetter;

        void Update()
        {
            if (_cachedWindow == null)
                _cachedWindow = _sharedInstanceGetter.Invoke(null, Array.Empty<object>()) as EditorWindow;

            if (IsVisible(_cachedWindow))
            {
                if (!_visibleLastTick)
                {
                    // show in this tick
                    _skipHiddenPackages.SetValue(_cachedWindow, false);
                    _filterSettingsChanged.Invoke(_cachedWindow, Array.Empty<object>());
                }

                _visibleLastTick = true;
            }
            else
            {
                _visibleLastTick = false;
            }
        }

        bool IsVisible(EditorWindow window) => window && (Object)_parent.GetValue(window);
    }
}

#endif
