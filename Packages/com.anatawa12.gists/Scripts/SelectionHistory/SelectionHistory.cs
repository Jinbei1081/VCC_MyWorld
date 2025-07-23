/*
 * SelectionHistory
 * This is a tool to back / forward history with mouse button 4, 5.
 *
 * https://gist.github.com/anatawa12/70a50d924fe018716761741780893b3a
 *
 * Click `Tools/anatawa12 gists/Selection History/Show history` to open window, or just click mouse back/forward button.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_70a50d924fe018716761741780893b3a)

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    [InitializeOnLoad]
    internal class SelectionHistory : EditorWindow
    {
        private const string GIST_NAME = "Selection History";
        private const string MENU_BASE = "Tools/anatawa12 gists/" + GIST_NAME;

        static SelectionHistory()
        {
            Selection.selectionChanged = () => SelectionHistoryObject.instance.OnSelectionChanged();
            RegisterBeforeEventProcessedCallback();
        }

        private static Event guiUtilityEvent;

        private static void RegisterBeforeEventProcessedCallback()
        {
            var type = typeof(GUIUtility);
            var beforeEventProcessedField = type.GetField("beforeEventProcessed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (beforeEventProcessedField == null)
            {
                Debug.LogError("Failed to find beforeEventProcessed field in GUIUtility");
                return;
            }

            var eventField = type.GetField("m_Event", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (eventField == null)
            {
                Debug.LogError("Failed to find m_Event field in GUIUtility");
                return;
            }

            guiUtilityEvent = (Event)eventField.GetValue(null);

            var beforeEventProcessed = (Action<EventType, KeyCode>)beforeEventProcessedField.GetValue(null);
            beforeEventProcessed += BeforeEventProcessed;
            beforeEventProcessedField.SetValue(null, beforeEventProcessed);
        }

        private static void BeforeEventProcessed(EventType eventType, KeyCode key)
        {
            if (guiUtilityEvent.type == EventType.MouseDown)
            {
                if (guiUtilityEvent.button == 3)
                {
                    SelectionHistoryObject.instance.BackSelection();
                }
                else if (guiUtilityEvent.button == 4)
                {
                    SelectionHistoryObject.instance.ForwardSelection();
                }
            }
        }

        [MenuItem(MENU_BASE + "/Show history")]
        private static void Open() =>
            GetWindow<SelectionHistory>(GIST_NAME);

        [MenuItem(MENU_BASE + "/Back Selection")]
        private static void Back() => SelectionHistoryObject.instance.BackSelection();

        [MenuItem(MENU_BASE + "/Back Selection", true)]
        private static bool BackEnabled() => SelectionHistoryObject.instance.CanBackSelection();

        [MenuItem(MENU_BASE + "/Forward Selection")]
        private static void Forward() => SelectionHistoryObject.instance.ForwardSelection();

        [MenuItem(MENU_BASE + "/Forward Selection", true)]
        private static bool ForwardEnabled() => SelectionHistoryObject.instance.CanForwardSelection();

        [MenuItem(MENU_BASE + "/Clear Selection History")]
        private static void Clear() => SelectionHistoryObject.instance.ClearHistory();

        #region GUI

        [SerializeField]
        private List<bool> expands = new List<bool>();

        private void OnEnable() => Selection.selectionChanged += Repaint;
        private void OnDisable() => Selection.selectionChanged -= Repaint;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Selection History", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear History"))
            {
                SelectionHistoryObject.instance.ClearHistory();
                expands.Clear();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!SelectionHistoryObject.instance.CanBackSelection());
            if (GUILayout.Button("Back Selection"))
            {
                SelectionHistoryObject.instance.BackSelection();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!SelectionHistoryObject.instance.CanForwardSelection());
            if (GUILayout.Button("Forward Selection"))
            {
                SelectionHistoryObject.instance.ForwardSelection();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            SelectionHistoryObject.instance.DrawHistory(expands);
        }

        #endregion

        internal class SelectionHistoryObject : ScriptableSingleton<SelectionHistoryObject>
        {
            [SerializeField]
            List<SelectionEntry> history = new();
            [SerializeField]
            int backCount = 0;

            [NonSerialized] [CanBeNull] private Object[] ourLastSelection = null;

            [Serializable]
            struct SelectionEntry
            {
                public Object[] selection;
            }

            public void OnSelectionChanged()
            {
                var selection = Selection.objects;

                if (ourLastSelection != null && selection.SequenceEqual(ourLastSelection))
                {
                    ourLastSelection = null;
                    return;
                }
                ourLastSelection = null;

                history.RemoveRange(history.Count - backCount, backCount);
                history.Add(new SelectionEntry { selection = selection });
                backCount = 0;
            }

            public void ClearHistory()
            {
                history.Clear();
                backCount = 0;
            }

            public int CurrentIndex => history.Count - backCount - 1;
            public void BackSelection() => SelectAt(CurrentIndex - 1);
            public bool CanBackSelection() => history.Count > backCount + 1;
            public void ForwardSelection() => SelectAt(CurrentIndex + 1);
            public bool CanForwardSelection() => backCount > 0;

            private bool SelectAt(int selectionIndex)
            {
                if (selectionIndex < 0 || selectionIndex >= history.Count) return false;
                var selection = history[selectionIndex].selection;
                Selection.objects = selection;
                ourLastSelection = selection;
                backCount = history.Count - selectionIndex - 1;
                return true;
            }

            static class Styles
            {
                public static GUIStyle labelStyle;
                public static GUIStyle currentSelectionStyle;
                static Styles()
                {
                    labelStyle = new GUIStyle(EditorStyles.label);
                    labelStyle.margin = EditorStyles.foldout.margin;
                    labelStyle.padding = EditorStyles.foldout.padding;

                    currentSelectionStyle = new GUIStyle(GUIStyle.none);
                    currentSelectionStyle.normal.background = EditorGUIUtility.whiteTexture;
                }
            }

            public void DrawHistory(List<bool> expands)
            {
                if (expands.Count < history.Count) expands.AddRange(Enumerable.Repeat(false, history.Count - expands.Count));
                if (expands.Count > history.Count) expands.RemoveRange(history.Count, expands.Count - history.Count);

                var label = new GUIContent();

                for (var i = history.Count - 1; i >= 0; i--)
                {
                    var viewIndex = history.Count - i;
                    var selection = history[i].selection;

                    var color = GUI.backgroundColor;
                    if (viewIndex - 1 == backCount) GUI.backgroundColor = new Color(0.0f, 1f, 0.0f, .25f);
                    else GUI.backgroundColor = Color.clear;
                    EditorGUILayout.BeginVertical(Styles.currentSelectionStyle);
                    GUI.backgroundColor = color;

                    var position = EditorGUILayout.GetControlRect(true, 18f);

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && position.Contains(Event.current.mousePosition))
                    {
                        // selection is clicked, update selection
                        SelectAt(i);
                    }

                    if (selection.Length == 0)
                    {
                        int controlId = GUIUtility.GetControlID(i + 0xFFFF, FocusType.Passive, position);
                        label.text = $"History #{viewIndex}";
                        position = EditorGUI.PrefixLabel(position, controlId, label, Styles.labelStyle);
                        if (Event.current.type == EventType.Repaint)
                        {
                            label.text = "Empty";
                            EditorStyles.label.Draw(position, label, controlId);
                        }
                    }
                    else if (selection.Length == 1)
                    {
                        int controlId = GUIUtility.GetControlID(i + 0xFFFF, FocusType.Passive, position);
                        label.text = $"History #{viewIndex}";
                        position = EditorGUI.PrefixLabel(position, controlId, label, Styles.labelStyle);
                        if (Event.current.type == EventType.Repaint)
                        {
                            label.text = "Empty";
                            EditorStyles.label.Draw(position, label, controlId);
                        }
                        EditorGUI.ObjectField(position, selection[0], typeof(Object), true);
                    }
                    else
                    {
                        expands[i] = EditorGUI.Foldout(position, expands[i], $"History #{viewIndex} ({selection.Length})");
                        if (expands[i])
                        {
                            EditorGUI.indentLevel++;
                            foreach (var obj in selection)
                            {
                                EditorGUILayout.ObjectField(obj, typeof(Object), true);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}

#endif
