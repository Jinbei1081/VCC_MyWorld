/*
 * RemoveUnusedAnimatorParameters
 * Simple tool to remove unused animator parameter from animator controller
 * https://gist.github.com/anatawa12/9d6d69fdb042d636cc640f85ce0c5fae
 *
 * Click `Tools/anatawa12 gists/RemoveUnusedAnimatorParameters` to open this window.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_9d6d69fdb042d636cc640f85ce0c5fae)

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal class RemoveUnusedAnimatorParameters : EditorWindow
    {
        private const string GIST_NAME = "RemoveUnusedAnimatorParameters";

        [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
        static void Create() => GetWindow<RemoveUnusedAnimatorParameters>(GIST_NAME);

        private AnimatorController controller;
        private string[] unusedParameters = Array.Empty<string>();
        private string[] usedParameters = Array.Empty<string>();

        private Vector2 usedParametersScroll;
        private Vector2 unusedParametersScroll;

        private void OnGUI()
        {
            controller = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", controller,
                typeof(AnimatorController), false);

            if (GUILayout.Button("Find Unused Parameters"))
            {
                if (controller == null)
                {
                    EditorUtility.DisplayDialog(GIST_NAME, "Please select an Animator Controller", "OK");
                    return;
                }

                usedParameters = CollectUsedParameters(controller);
                unusedParameters = controller.parameters.Select(p => p.name).Except(usedParameters).ToArray();
            }

            EditorGUI.BeginDisabledGroup(controller == null || unusedParameters.Length == 0 || usedParameters.Length == 0);
            if (GUILayout.Button("Remove Unused Parameters"))
            {
                Undo.RecordObject(controller, "Remove Unused Parameters");
                var parameters = controller.parameters.ToList();
                parameters.RemoveAll(p => unusedParameters.Contains(p.name));
                controller.parameters = parameters.ToArray();
                EditorUtility.SetDirty(controller);

                unusedParameters = Array.Empty<string>();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Used Parameters", EditorStyles.boldLabel);
            usedParametersScroll = EditorGUILayout.BeginScrollView(usedParametersScroll);
            foreach (var parameter in usedParameters)
                EditorGUILayout.LabelField(parameter);
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Unused Parameters", EditorStyles.boldLabel);
            unusedParametersScroll = EditorGUILayout.BeginScrollView(unusedParametersScroll);
            foreach (var parameter in unusedParameters)
                EditorGUILayout.LabelField(parameter);
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private static string[] CollectUsedParameters([CanBeNull] AnimatorController controller)
        {
            if (controller == null) return Array.Empty<string>();
            var usedParameters = new HashSet<string>();
            foreach (var layer in controller.layers)
                CollectUsedParameters(usedParameters, layer.stateMachine);
            return usedParameters.ToArray();
        }

        private static void CollectUsedParameters(HashSet<string> hashSet, [CanBeNull] AnimatorStateMachine stateMachine)
        {
            if (stateMachine == null) return;

            foreach (var state in stateMachine.states)
                if (state.state is { } animatorState)
                    CollectUsedParameters(hashSet, animatorState);

            foreach (var stateMachineEntryTransition in stateMachine.entryTransitions)
                CollectUsedParameters(hashSet, stateMachineEntryTransition);
            foreach (var stateMachineTransition in stateMachine.anyStateTransitions)
                CollectUsedParameters(hashSet, stateMachineTransition);

            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                CollectUsedParameters(hashSet, childStateMachine.stateMachine);
                foreach (var transition in stateMachine.GetStateMachineTransitions(childStateMachine.stateMachine))
                    CollectUsedParameters(hashSet, transition);
            }
        }

        private static void CollectUsedParameters(HashSet<string> hashSet, [CanBeNull] AnimatorTransitionBase transition)
        {
            if (transition == null) return;
            foreach (var condition in transition.conditions)
                hashSet.Add(condition.parameter);
        }

        private static void CollectUsedParameters(HashSet<string> hashSet, [CanBeNull] AnimatorState animatorState)
        {
            if (animatorState == null) return;
            foreach (var behaviour in animatorState.transitions)
                CollectUsedParameters(hashSet, behaviour);

            CollectUsedParameters(hashSet, animatorState.motion);
        }

        private static void CollectUsedParameters(HashSet<string> hashSet, Motion motion)
        {
            if (motion is not BlendTree blendTree) return;

            switch (blendTree.blendType)
            {
                case BlendTreeType.Simple1D:
                case BlendTreeType.Direct:
                    hashSet.Add(blendTree.blendParameter);
                    break;
                case BlendTreeType.SimpleDirectional2D:
                case BlendTreeType.FreeformDirectional2D:
                case BlendTreeType.FreeformCartesian2D:
                    hashSet.Add(blendTree.blendParameter);
                    hashSet.Add(blendTree.blendParameterY);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var childMotion in blendTree.children)
                CollectUsedParameters(hashSet, childMotion.motion);
        }
    }
}

#endif
