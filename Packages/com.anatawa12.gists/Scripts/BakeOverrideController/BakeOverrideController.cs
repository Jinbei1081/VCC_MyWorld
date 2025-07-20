/*
 * Bake Override Controller
 * The tool to bake Animator Override Controller to Animator Controller.
 * https://gist.github.com/anatawa12/10b3ac3b4e7f6603e5ca5a91e50fe2b7
 *
 * Click `Tools/anatawa12 gists/ApplyOverrideController` to open this window,
 * and set Animator Override Controller to `Override Controller` field, and click `Bake` button.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_10b3ac3b4e7f6603e5ca5a91e50fe2b7)

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace anatawa12.gists
{
    internal class BakeOverrideController : EditorWindow
    {
        private const string GIST_NAME = "Bake Override Controller";

        [MenuItem("Tools/anatawa12 gists/" + GIST_NAME)]
        static void Create() => GetWindow<BakeOverrideController>(GIST_NAME);

        private AnimatorOverrideController? overrideController;

        private void OnGUI()
        {
            overrideController = (AnimatorOverrideController)EditorGUILayout.ObjectField("Override Controller",
                overrideController, typeof(AnimatorOverrideController), false);

            EditorGUI.BeginDisabledGroup(overrideController == null);
            if (GUILayout.Button("Bake"))
            {
                Bake();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void Bake()
        {
            if (overrideController == null) return;

            var sourcePath = AssetDatabase.GetAssetPath(overrideController);
            var baseDir = sourcePath == "" ? "Assets" : System.IO.Path.GetDirectoryName(sourcePath);
            var path = EditorUtility.SaveFilePanelInProject("Save Animator Controller", overrideController.name,
                "controller", "Save Animator Controller", baseDir);

            if (string.IsNullOrEmpty(path)) return;

            var (originalController, overrides) =
                GetControllerAndOverrides(overrideController);

            var newController = new AnimatorController();
            AssetDatabase.CreateAsset(newController, path);

            var stateMap = new Dictionary<AnimatorState, AnimatorState>();
            [return:NotNullIfNotNull("oldState")]
            AnimatorState? GetNewState(AnimatorState? oldState) => oldState == null ? null : stateMap[oldState];

            // copy statemachines
            var layers = Array.ConvertAll(originalController.layers, layer =>
                new AnimatorControllerLayer
                {
                    name = layer.name,
                    stateMachine = CloneStateMachine(layer.stateMachine, overrides, newController, stateMap),
                    avatarMask = layer.avatarMask,
                    blendingMode = layer.blendingMode,
                    syncedLayerIndex = layer.syncedLayerIndex,
                    iKPass = layer.iKPass,
                    defaultWeight = layer.defaultWeight,
                    syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
                });

            foreach (var layer in layers)
            {
                if (layer.syncedLayerIndex == -1) continue;
                var syncedNewLayer = layers[layer.syncedLayerIndex];
                var syncedOriginal = originalController.layers[layer.syncedLayerIndex];

                foreach (var originalState in AllStates(syncedOriginal.stateMachine))
                {
                    syncedNewLayer.SetOverrideMotion(GetNewState(originalState),
                        syncedOriginal.GetOverrideMotion(originalState));
                    syncedNewLayer.SetOverrideBehaviours(GetNewState(originalState),
                        Array.ConvertAll(syncedOriginal.GetOverrideBehaviours(originalState), Instantiate));
                }
            }

            newController.layers = layers;
            newController.parameters = Array.ConvertAll(originalController.parameters, parameter =>
                new AnimatorControllerParameter
                {
                    name = parameter.name,
                    type = parameter.type,
                    defaultBool = parameter.defaultBool,
                    defaultFloat = parameter.defaultFloat,
                    defaultInt = parameter.defaultInt,
                });

            AssetDatabase.SaveAssets();
        }

        [return:NotNullIfNotNull("original")]
        private AnimatorStateMachine? CloneStateMachine(AnimatorStateMachine? original,
            IReadOnlyDictionary<AnimationClip, AnimationClip> overrides, Object assetContainer,
            Dictionary<AnimatorState, AnimatorState> stateMap)
        {
            if (original == null) return null;
            [return:NotNullIfNotNull("oldState")]
            AnimatorState? GetNewState(AnimatorState? oldState) => oldState == null ? null : stateMap[oldState];

            // duplicate state
            foreach (var animatorState in AllStates(original))
            {
                if (!stateMap.ContainsKey(animatorState))
                {
                    var state = new AnimatorState
                    {
                        name = animatorState.name,
                        motion = CloneMotion(animatorState.motion, overrides, assetContainer),
                        hideFlags = animatorState.hideFlags,
                        speed = animatorState.speed,
                        cycleOffset = animatorState.cycleOffset,
                        mirror = animatorState.mirror,
                        tag = animatorState.tag,
                        writeDefaultValues = animatorState.writeDefaultValues,
                        timeParameterActive = animatorState.timeParameterActive,
                        timeParameter = animatorState.timeParameter,
                        speedParameterActive = animatorState.speedParameterActive,
                        speedParameter = animatorState.speedParameter,
                        mirrorParameterActive = animatorState.mirrorParameterActive,
                        mirrorParameter = animatorState.mirrorParameter,
                        cycleOffsetParameterActive = animatorState.cycleOffsetParameterActive,
                        cycleOffsetParameter = animatorState.cycleOffsetParameter,
                        iKOnFeet = animatorState.iKOnFeet,
                        behaviours = animatorState.behaviours.Select(Instantiate).ToArray(),
                    };
                    AssetDatabase.AddObjectToAsset(state, assetContainer);
                    stateMap.Add(animatorState, state);
                }
            }

            // create StateMachine and Trnasitions
            var stateMachineMap = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();
            [return:NotNullIfNotNull("oldStateMachine")]
            AnimatorStateMachine? GetNewStateMachine(AnimatorStateMachine? oldStateMachine) =>
                oldStateMachine == null ? null : stateMachineMap[oldStateMachine];

            AnimatorStateMachine CloneStateMachineInner(AnimatorStateMachine original)
            {
                var newStateMachine = new AnimatorStateMachine
                {
                    name = original.name,
                    hideFlags = original.hideFlags,
                    anyStatePosition = original.anyStatePosition,
                    entryPosition = original.entryPosition,
                    exitPosition = original.exitPosition,
                    parentStateMachinePosition = original.parentStateMachinePosition,
                    defaultState = GetNewState(original.defaultState),
                    states = Array.ConvertAll(original.states, state => new ChildAnimatorState
                    {
                        state = GetNewState(state.state),
                        position = state.position,
                    }),
                    stateMachines = Array.ConvertAll(original.stateMachines, stateMachine =>
                        new ChildAnimatorStateMachine
                        {
                            stateMachine = CloneStateMachineInner(stateMachine.stateMachine),
                            position = stateMachine.position,
                        }),
                };
                AssetDatabase.AddObjectToAsset(newStateMachine, assetContainer);
                stateMachineMap.Add(original, newStateMachine);
                return newStateMachine;
            }

            var newRootStateMachine = CloneStateMachineInner(original);

            AnimatorCondition CloneAnimatorCondition(AnimatorCondition condition)
            {
                return new AnimatorCondition
                {
                    mode = condition.mode,
                    parameter = condition.parameter,
                    threshold = condition.threshold,
                };
            }

            AnimatorTransition CloneAnimatorTransition(AnimatorTransition transition)
            {
                var newTransition = new AnimatorTransition
                {
                    name = transition.name,
                    hideFlags = transition.hideFlags,

                    solo = transition.solo,
                    mute = transition.mute,
                    isExit = transition.isExit,
                    destinationStateMachine = GetNewStateMachine(transition.destinationStateMachine),
                    destinationState = GetNewState(transition.destinationState),
                    conditions = Array.ConvertAll(transition.conditions, CloneAnimatorCondition),
                };
                AssetDatabase.AddObjectToAsset(newTransition, assetContainer);
                return newTransition;
            }

            AnimatorStateTransition CloneAnimatorStateTransition(AnimatorStateTransition transition)
            {
                var newTransition = new AnimatorStateTransition
                {
                    name = transition.name,
                    hideFlags = transition.hideFlags,

                    solo = transition.solo,
                    mute = transition.mute,
                    isExit = transition.isExit,
                    destinationStateMachine = GetNewStateMachine(transition.destinationStateMachine),
                    destinationState = GetNewState(transition.destinationState),
                    conditions = Array.ConvertAll(transition.conditions, CloneAnimatorCondition),

                    duration = transition.duration,
                    offset = transition.offset,
                    interruptionSource = transition.interruptionSource,
                    orderedInterruption = transition.orderedInterruption,
                    exitTime = transition.exitTime,
                    hasExitTime = transition.hasExitTime,
                    hasFixedDuration = transition.hasFixedDuration,
                    canTransitionToSelf = transition.canTransitionToSelf,
                };
                AssetDatabase.AddObjectToAsset(newTransition, assetContainer);
                return newTransition;
            }

            foreach (var originalState in AllStates(original))
            {
                var newState = GetNewState(originalState);
                newState.transitions = Array.ConvertAll(originalState.transitions, CloneAnimatorStateTransition);
            }

            foreach (var originalStateMachine in AllStateMachines(original))
            {
                var newStateMachine = GetNewStateMachine(originalStateMachine);
                newStateMachine.entryTransitions = Array.ConvertAll(originalStateMachine.entryTransitions, CloneAnimatorTransition);
                newStateMachine.anyStateTransitions = Array.ConvertAll(originalStateMachine.anyStateTransitions, CloneAnimatorStateTransition);

                foreach (var childOriginal in originalStateMachine.stateMachines)
                {
                    var childNew = GetNewStateMachine(childOriginal.stateMachine);

                    newStateMachine.SetStateMachineTransitions(childNew,
                        Array.ConvertAll(originalStateMachine.GetStateMachineTransitions(childOriginal.stateMachine),
                            CloneAnimatorTransition));
                }
            }

            return newRootStateMachine;
        }

        private Motion? CloneMotion(Motion? motion,
            IReadOnlyDictionary<AnimationClip, AnimationClip> overrides, Object assetContainer)
        {
            switch (motion)
            {
                case null:
                    return null;
                case AnimationClip clip:
                    return overrides.GetValueOrDefault(clip, clip);
                case BlendTree blendTree:
                    var newBlendTree = new BlendTree
                    {
                        blendParameter = blendTree.blendParameter,
                        blendParameterY = blendTree.blendParameterY,
                        blendType = blendTree.blendType,
                        hideFlags = blendTree.hideFlags,
                        name = blendTree.name,
                        children = Array.ConvertAll(blendTree.children, child =>
                            new ChildMotion
                            {
                                motion = CloneMotion(child.motion, overrides, assetContainer),
                                cycleOffset = child.cycleOffset,
                                directBlendParameter = child.directBlendParameter,
                                mirror = child.mirror,
                                position = child.position,
                                threshold = child.threshold,
                                timeScale = child.timeScale,
                            }),
                    };
                    AssetDatabase.AddObjectToAsset(newBlendTree, assetContainer);
                    return newBlendTree;
                default:
                    throw new NotImplementedException();
            }
        }

        // Based on Avatar Optimizer
        // https://github.com/anatawa12/AvatarOptimizer/blob/8897af7095941be405389517a30885da71d0efa2/Internal/Utils/ACUtils.GetControllerAndOverrides.cs#L11-L45
        public static (AnimatorController, IReadOnlyDictionary<AnimationClip, AnimationClip>) GetControllerAndOverrides(
            RuntimeAnimatorController runtimeController)
        {
            if (runtimeController == null) throw new ArgumentNullException(nameof(runtimeController));
            if (runtimeController is AnimatorController originalController)
                return (originalController, new Dictionary<AnimationClip, AnimationClip>());

            var overrides = new Dictionary<AnimationClip, AnimationClip>();
            var overridesBuffer = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            for (;;)
            {
                if (runtimeController is AnimatorController controller)
                    return (controller, overrides);

                var overrideController = (AnimatorOverrideController)runtimeController;

                runtimeController = overrideController.runtimeAnimatorController;
                overrideController.GetOverrides(overridesBuffer);
                overridesBuffer.RemoveAll(x => !x.Value);

                var currentOverrides = overridesBuffer
                    .GroupBy(kvp => kvp.Value, kvp => kvp.Key)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var upperMappedFrom in overrides.Keys.ToArray())
                    if (currentOverrides.TryGetValue(upperMappedFrom, out var currentMappedFrom))
                        foreach (var mappedFrom in currentMappedFrom)
                            overrides[mappedFrom] = overrides[upperMappedFrom];

                foreach (var (original, mapped) in overridesBuffer) overrides.TryAdd(original, mapped);
            }
        }

        // Based on Avatar Optimizer
        // https://github.com/anatawa12/AvatarOptimizer/blob/8897af7095941be405389517a30885da71d0efa2/Internal/Utils/ACUtils.cs#L20-L29
        public static IEnumerable<AnimatorState> AllStates(AnimatorStateMachine? stateMachine)
        {
            if (stateMachine == null) yield break;
            foreach (var state in stateMachine.states)
                yield return state.state;

            foreach (var child in stateMachine.stateMachines)
            foreach (var state in AllStates(child.stateMachine))
                yield return state;
        }

        // Based on Avatar Optimizer
        // https://github.com/anatawa12/AvatarOptimizer/blob/8897af7095941be405389517a30885da71d0efa2/Internal/Utils/ACUtils.cs#L10-L18
        public static IEnumerable<AnimatorStateMachine> AllStateMachines(AnimatorStateMachine? stateMachine)
        {
            if (stateMachine == null) yield break;
            yield return stateMachine;

            foreach (var child in stateMachine.stateMachines)
            foreach (var machine in AllStateMachines(child.stateMachine))
                yield return machine;
        }
    }
}

#endif