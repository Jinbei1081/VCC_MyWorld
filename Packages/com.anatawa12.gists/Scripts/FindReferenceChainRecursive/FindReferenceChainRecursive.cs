/*
 * FindReferenceChainRecursive
 * Tool to find unexpected references
 * https://gist.github.com/anatawa12/ae5f7b3c5e07150ddc1eb9f0948019ff
 *
 * Open Tools/anatawa12 gists/Find Reference Chain Recursive and drop your root object to find references.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_ae5f7b3c5e07150ddc1eb9f0948019ff)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal class FindReferenceChainRecursive : EditorWindow
    {
        public Object entryPoint;
        public bool unpackFirst = true;

        [MenuItem("Tools/anatawa12 gists/Find Reference Chain Recursive")]
        static void Create() => GetWindow<FindReferenceChainRecursive>("Find Reference Chain Recursive");

        private void OnGUI()
        {
            entryPoint = EditorGUILayout.ObjectField("Entry Point", entryPoint, typeof(Object), true);
            unpackFirst = EditorGUILayout.Toggle("Unpack First", unpackFirst);

            if (entryPoint == null)
            {
                EditorGUILayout.HelpBox("Select EntryPoint", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("Find References!"))
                {
                    FindReferences();
                }
            }
        }

        private void FindReferences()
        {
            var exportTo = EditorUtility.SaveFilePanel("References Text", ".", "references.txt", "txt");
            if (string.IsNullOrEmpty(exportTo)) return;

            // ReSharper disable once LocalVariableHidesMember
            var entryPoint = this.entryPoint;
            var unpack = unpackFirst && PrefabUtility.IsPartOfPrefabInstance(entryPoint);

            if (unpack)
                entryPoint = Instantiate(entryPoint, null);
            try
            {
                var queue = new Queue<Object>();
                var set = new HashSet<int>();

                void Add(Object obj)
                {
                    if (set.Add(obj.GetInstanceID())) queue.Enqueue(obj);
                }

                queue.Enqueue(entryPoint);
                var builder = new StringBuilder();
                var count = 0;
                try
                {
                    while (queue.Count != 0)
                    {
                        EditorUtility.DisplayProgressBar("Find References", $"{count} / {set.Count}",
                            (float)count / set.Count);
                        count++;
                        FindRecursive(queue.Dequeue(), Add, builder);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                File.WriteAllText(exportTo, builder.ToString());
            }
            finally
            {
                if (unpack)
                    DestroyImmediate(entryPoint);
            }
        }

        private static void FindRecursive(Object obj, Action<Object> add, StringBuilder builder)
        {
            var serialized = new SerializedObject(obj);

            var iter = serialized.GetIterator();
            var enterChildren = true;

            var persistent = EditorUtility.IsPersistent(obj);

            builder.Append($"{obj.GetInstanceID()}: ({obj.GetType().FullName}) " +
                           $"({(persistent ? "persistent" : "scene")}): {obj.name}\n");
            var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            if (id.identifierType != 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(id.assetGUID.ToString());
                builder.Append($"  at {path} with prefab {id.targetPrefabId} and id {id.targetObjectId}\n");
            }


            while (iter.Next(enterChildren))
            {
                switch (iter.propertyType)
                {
                    case SerializedPropertyType.Generic:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.AnimationCurve:
                    case SerializedPropertyType.Gradient:
                    case SerializedPropertyType.ExposedReference:
                    case SerializedPropertyType.FixedBufferSize:
                    case SerializedPropertyType.ManagedReference:
                        enterChildren = true;
                        break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.Boolean:
                    case SerializedPropertyType.Float:
                    case SerializedPropertyType.String:
                    case SerializedPropertyType.Color:
                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.Vector2:
                    case SerializedPropertyType.Vector3:
                    case SerializedPropertyType.Vector4:
                    case SerializedPropertyType.Rect:
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Character:
                    case SerializedPropertyType.Bounds:
                    case SerializedPropertyType.Quaternion:
                    case SerializedPropertyType.Vector2Int:
                    case SerializedPropertyType.Vector3Int:
                    case SerializedPropertyType.RectInt:
                    case SerializedPropertyType.BoundsInt:
                        enterChildren = false;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        if (iter.objectReferenceValue)
                        {
                            builder.Append($"  {iter.propertyPath}: {iter.objectReferenceInstanceIDValue}\n");
                            add(iter.objectReferenceValue);
                        }
                        enterChildren = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}

#endif
