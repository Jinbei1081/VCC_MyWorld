/*
 * ObjectFullDebugInspector
 * Debug inspector with full 
 * https://gist.github.com/anatawa12/8af588d3fc832910d5675566303002b5
 *
 * Tools/anatawa12 gists/ObjectFullDebugInspector to open the window
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_8af588d3fc832910d5675566303002b5)

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
  internal class ObjectFullDebugInspector : EditorWindow
  {
    private const string GIST_NAME = "ObjectFullDebugInspector";

    [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
    static void Create() => GetWindow<ObjectFullDebugInspector>(GIST_NAME);

    public Object obj;
    public bool editing;
    public Vector2 scroll;
    private SerializedObject _serializedObject;

    private void OnGUI()
    {
      obj = EditorGUILayout.ObjectField(obj, typeof(Object), true);
      if (obj != _serializedObject?.targetObject)
      {
        _serializedObject = obj ? new SerializedObject(obj) : null;
        editing = false;
      }

      var newEditing = EditorGUILayout.ToggleLeft("Allow Modification", editing);
      if (!editing && newEditing)
      {
        var confirm = EditorUtility.DisplayDialog("Warning",
          "You can collapse unity objects with ObjectFullDebugInspector so edit carefully.",
          "OK", "Cancel Editing");
        if (confirm)
          editing = true;
      }

      if (_serializedObject != null)
      {
        _serializedObject.Update();
        scroll = GUILayout.BeginScrollView(scroll);
        EditorGUI.BeginDisabledGroup(!editing);
        int num = EditorGUI.indentLevel;

        var prop = _serializedObject.GetIterator();
        var enterChildren = true;

        while (prop.Next(enterChildren))
        {
          EditorGUI.indentLevel = prop.depth + num;
          enterChildren = DefaultPropertyField(prop, new GUIContent(PropertyName(prop)));
          if (prop.isArray && enterChildren)
            prop.Next(true); // skip <arrayProperty>.array
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.EndScrollView();
        if (editing)
          _serializedObject.ApplyModifiedProperties();
      }
    }

    private static string PropertyName(SerializedProperty property)
    {
      var path = property.propertyPath;
      path = path.Substring(path.LastIndexOf('.') + 1);
      var bracketIndex = path.LastIndexOf('[');
      if (bracketIndex == -1) return path;
      return path.Substring(bracketIndex);
    }

    private static bool HasVisibleChildFields(SerializedProperty property)
    {
      switch (property.propertyType)
      {
        case SerializedPropertyType.Vector2:
        case SerializedPropertyType.Vector3:
        case SerializedPropertyType.Rect:
        case SerializedPropertyType.Bounds:
        case SerializedPropertyType.Vector2Int:
        case SerializedPropertyType.Vector3Int:
        case SerializedPropertyType.RectInt:
        case SerializedPropertyType.BoundsInt:
        case SerializedPropertyType.Integer:
        case SerializedPropertyType.Boolean:
        case SerializedPropertyType.Float:
        case SerializedPropertyType.String:
        case SerializedPropertyType.Color:
        case SerializedPropertyType.ObjectReference:
        case SerializedPropertyType.Enum:
        case SerializedPropertyType.Character:
        case SerializedPropertyType.AnimationCurve:
        case SerializedPropertyType.Gradient:
        case SerializedPropertyType.ArraySize:
        case SerializedPropertyType.ExposedReference:
        case SerializedPropertyType.LayerMask:
        case SerializedPropertyType.FixedBufferSize:
        case SerializedPropertyType.ManagedReference:
        case SerializedPropertyType.Vector4:
          return false;
        case SerializedPropertyType.Generic:
        case SerializedPropertyType.Quaternion:
          return true;
        default:
          return property.hasVisibleChildren;
      }
    }

    private static void SetExpandedRecurse(SerializedProperty property, bool expanded)
    {
      var serializedProperty = property.Copy();
      serializedProperty.isExpanded = expanded;
      var depth = serializedProperty.depth;
      while (serializedProperty.Next(true) && serializedProperty.depth > depth)
      {
        if (serializedProperty.hasVisibleChildren)
          serializedProperty.isExpanded = expanded;
      }
    }

    public static bool DefaultPropertyField(SerializedProperty property, GUIContent label)
    {
      if (!HasVisibleChildFields(property))
      {
        return EditorGUILayout.PropertyField(property, label, false);
      }
      else
      {
        var position = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(property.propertyType, null));
        label = EditorGUI.BeginProperty(position, label, property);
        var isExpanded = property.isExpanded;
        bool expanded;
        using (new EditorGUI.DisabledScope(!property.editable))
        {
          var style = DragAndDrop.activeControlID == -10 ? EditorStyles.foldoutPreDrop : EditorStyles.foldout;
          expanded = EditorGUI.Foldout(position, isExpanded, label, true, style);
        }

        if (expanded != isExpanded)
        {
          if (Event.current.alt)
            SetExpandedRecurse(property, expanded);
          else
            property.isExpanded = expanded;
        }

        EditorGUI.EndProperty();
        return expanded;
      }
    }

  }
}

#endif
