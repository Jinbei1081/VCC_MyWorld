// MIT License
// https://gist.github.com/anatawa12/581b66619711eaf5ebacbd85369d62e6
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_581b66619711eaf5ebacbd85369d62e6)

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace anatawa12.gists
{
    internal static class SetRandomBlueprintId
    {
        private static readonly Type PipelineManagerType = FindType("VRC.Core.PipelineManager");
        private static readonly Type AvatarDescriptorType = FindType("VRC.SDKBase.VRC_AvatarDescriptor");
        private static readonly Type SceneDescriptorType = FindType("VRC.SDKBase.VRC_SceneDescriptor");

        static SetRandomBlueprintId()
        {
        }

        [MenuItem("CONTEXT/PipelineManager/Set Random Blueprint ID")]
        private static void Component(MenuCommand menuCommand)
        {
            if (PipelineManagerType == null || !PipelineManagerType.IsInstanceOfType(menuCommand.context))
                return;
            var context = (Component)menuCommand.context;

            var serialized = new SerializedObject(menuCommand.context);
            var blueprintId = serialized.FindProperty("blueprintId");
            var contentType = serialized.FindProperty("contentType");
            var completedSDKPipeline = serialized.FindProperty("completedSDKPipeline");

            if (!string.IsNullOrEmpty(blueprintId.stringValue))
            {
                if (!EditorUtility.DisplayDialog("Confirm?",
                        "BlueprintID is already set. Are you want to clear and set new BlueprintID?", "Clear & Set",
                        "Cancel"))
                {
                    return;
                }
            }

            bool isAvatar = context.GetComponent(AvatarDescriptorType);
            bool isScene = context.GetComponent(SceneDescriptorType);

            if (isAvatar && isScene)
            {
                EditorUtility.DisplayDialog("Error?",
                    "Both VRC Avatar Descriptor and VRC Scene Descriptor are attached!", "OK");
                return;
            }
            
            if (!isAvatar && !isScene)
            {
                EditorUtility.DisplayDialog("Error?",
                    "Both VRC Avatar Descriptor and VRC Scene Descriptor are attached!", "OK");
                return;
            }

            if (isAvatar)
            {
                blueprintId.stringValue = "avtr_" + Guid.NewGuid();
                contentType.enumValueIndex = (int)ContentType.avatar;
            }
            else
            {
                Assert.IsTrue(isScene);
                blueprintId.stringValue = "wrld_" + Guid.NewGuid();
                contentType.enumValueIndex = (int)ContentType.world;
            }

            completedSDKPipeline.boolValue = false;

            serialized.ApplyModifiedProperties();
        }

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(avatarValidation => avatarValidation != null);
        }

        private enum ContentType
        {
            avatar,
            world,
        }
    }
}

#endif
