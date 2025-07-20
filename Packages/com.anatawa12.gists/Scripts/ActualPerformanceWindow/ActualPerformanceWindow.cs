/*
 * Actual Performance Info Window
 * https://gist.github.com/anatawa12/a4bb4e2e5d75b4fa5ba42e236aae564d
 *
 * Copy this cs file to anywhere in your asset folder is the only step to install this tool.
 *
 * A window to see actual performance rank on building avatars.
 * When you click the `Build & Publish` button, this class will compute actual performance rank show you that.
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

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_a4bb4e2e5d75b4fa5ba42e236aae564d)

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDKBase;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;

namespace anatawa12.gists
{
    using static AvatarPerformanceCategory;
    //using dynamic = AvatarPerformanceStats;

    internal class ActualPerformanceWindow : EditorWindow, ISerializationCallbackReceiver
    {
        [SerializeField] private AvatarPerformanceInfoSet[] avatars = Array.Empty<AvatarPerformanceInfoSet>();
        [SerializeField] private int selectingIndex;

        [SerializeField] private Vector2 scroll;

        [SerializeField] private bool calculatePc = true;
        [SerializeField] private bool calculateAndroid = true;
        [SerializeField] private ShowingTarget showingPcInfo = ShowingTarget.PC;

        // true if build is in progress. this is used to avoid build info update on play
        [SerializeField] private bool isBuilding;
        private bool IsBuilding => isBuilding;

        private const float singleRowWidth = 450;
        private bool IsTwoRow => singleRowWidth < position.width;

        private GUIContent[] _labels;
        private bool _searchMode;
        private string _searchText = string.Empty;
        private GUIContent[] _filteredLabels;
        private int _filteredSelectingIndex;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (_searchMode)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();
                _searchText = EditorGUILayout.TextField(_searchText);
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("Performance Rank for Previous build of ", Styles.TextStyle, GUILayout.MinHeight(32));
            }
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_search_icon"), GUILayout.Width(22), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                _searchMode = !_searchMode;
                if (_searchMode)
                {
                    _filteredSelectingIndex = selectingIndex;
                    _searchText = string.Empty;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            if (_searchMode)
            {
                _filteredLabels = string.IsNullOrEmpty(_searchText) ? _labels : _labels.Where(x => x.text.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (_filteredSelectingIndex >= _filteredLabels.Length)
                {
                    _filteredSelectingIndex = 0;
                }
                _filteredSelectingIndex = EditorGUILayout.Popup(_filteredSelectingIndex, _filteredLabels);
                if (_filteredSelectingIndex >= 0 && _filteredSelectingIndex < _filteredLabels.Length)
                {
                    var filteredLabelText = _filteredLabels[_filteredSelectingIndex].text;
                    var filteredIndex = Array.FindIndex(_labels, x => x.text == filteredLabelText);
                    if (filteredIndex >= 0)
                    {
                        selectingIndex = filteredIndex;
                    }
                }
            }
            else
            {
                selectingIndex = EditorGUILayout.Popup(selectingIndex, _labels);
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Avatars"))
            {
                avatars = Array.Empty<AvatarPerformanceInfoSet>();
                _labels = Array.Empty<GUIContent>();
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            if (IsTwoRow) EditorGUILayout.BeginHorizontal();
            calculatePc = EditorGUILayout.ToggleLeft("Calculate PC", calculatePc);
            calculateAndroid = EditorGUILayout.ToggleLeft("Calculate Android", calculateAndroid);
            if (IsTwoRow) EditorGUILayout.EndHorizontal();

            var data = ComputeShowingData();

            // header
            switch (data)
            {
                case ShowingData.BothInTwoRow:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("PC", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    break;
                case ShowingData.BothInSingleRow:
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Result for");
                    showingPcInfo = (ShowingTarget)EditorGUILayout.EnumPopup(showingPcInfo);
                    EditorGUILayout.EndHorizontal();
                    break;
                case ShowingData.OnlyPC:
                    EditorGUILayout.LabelField("PC", EditorStyles.boldLabel);
                    break;
                case ShowingData.OnlyAndroid:
                    EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
                    break;
                case ShowingData.NoInfoForAvatar:
                case ShowingData.NoAvatars:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            switch (data)
            {
                case ShowingData.NoAvatars:
                    GUILayout.Label("No avatars selecting", GUILayout.Height(32));
                    break;
                case ShowingData.BothInTwoRow:
                    var avatar = avatars[selectingIndex];
                    EditorGUILayout.BeginHorizontal();
                    DisplayAvatarInfo(avatar.pc);
                    DisplayAvatarInfo(avatar.android);
                    EditorGUILayout.EndHorizontal();
                    break;
                case ShowingData.BothInSingleRow:
                    switch (showingPcInfo)
                    {
                        case ShowingTarget.PC:
                            DisplayAvatarInfo(avatars[selectingIndex].pc);
                            break;
                        case ShowingTarget.Android:
                            DisplayAvatarInfo(avatars[selectingIndex].android);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case ShowingData.OnlyPC:
                    DisplayAvatarInfo(avatars[selectingIndex].pc);
                    break;
                case ShowingData.OnlyAndroid:
                    DisplayAvatarInfo(avatars[selectingIndex].android);
                    break;
                case ShowingData.NoInfoForAvatar:
                    GUILayout.Label("No calculated data for this avatar", GUILayout.Height(32));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.EndScrollView();
        }

        enum ShowingData
        {
            NoAvatars,
            BothInTwoRow,
            BothInSingleRow,
            OnlyPC,
            OnlyAndroid,
            NoInfoForAvatar,
        }

        ShowingData ComputeShowingData()
        {
            if (selectingIndex >= 0 && selectingIndex < avatars.Length)
            {
                var avatar = avatars[selectingIndex];
                if (avatar.pc.Valid) {
                    if (avatar.android.Valid)
                        return IsTwoRow ? ShowingData.BothInTwoRow : ShowingData.BothInSingleRow;
                    else
                        return ShowingData.OnlyPC;
                }
                else
                {
                    if (avatar.android.Valid)
                        return ShowingData.OnlyAndroid;
                    else
                        return ShowingData.NoInfoForAvatar;
                }
            }
            else
            {
                return ShowingData.NoAvatars;
            }
        }

        private void DisplayAvatarInfo(AvatarPerformanceInfo avatar)
        {
            EditorGUILayout.BeginVertical();
            DisplayRating(avatar.overall.rating, $"Overall Rating: {avatar.overall.rating}");

            foreach (var performanceInfo in avatar.info)
                DisplayRating(performanceInfo.rating,
                    $"{performanceInfo.categoryName}: {performanceInfo.rating} ({performanceInfo.data})");
            EditorGUILayout.EndVertical();
        }

        private static void DisplayRating(PerformanceRating rating, string message)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(PerformanceIcons.GetIconForPerformance(rating)), GUILayout.Height(32),
                GUILayout.Width(32));
            GUILayout.Label(message, Styles.TextStyle, GUILayout.MinHeight(32));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        [NotNull]
        private static ActualPerformanceWindow GetWindowInstance() => GetWindow<ActualPerformanceWindow>("Actual Performance");

        [CanBeNull]
        private static ActualPerformanceWindow TryGetWindowInstance()
        {
            var objects = Resources.FindObjectsOfTypeAll<ActualPerformanceWindow>();
            return objects.Length == 0 ? null : objects[0];
        }

        private void AddInfo(in AvatarPerformanceInfoSet performanceInfo)
        {
            EditorUtility.SetDirty(this);
            if (avatars == null) avatars = Array.Empty<AvatarPerformanceInfoSet>();

            for (var i = 0; i < avatars.Length; i++)
            {
                if (avatars[i].avatarName == performanceInfo.avatarName)
                {
                    avatars[i] = performanceInfo;
                    selectingIndex = i;
                    return;
                }
            }

            // not found: add
            ArrayUtility.Add(ref avatars, performanceInfo);
            selectingIndex = avatars.Length - 1;
            ResetLabels();
        }

        private void MarkBuilding()
        {
            if (!BuildAvatarInEditModeDetector.IsVrcSdkSupportsBuildingAvatarInEditMode) return;
            isBuilding = true;
            EditorUtility.SetDirty(this);
        }

        private void ClearBuilding()
        {
            if (!BuildAvatarInEditModeDetector.IsVrcSdkSupportsBuildingAvatarInEditMode) return;
            isBuilding = false;
            EditorUtility.SetDirty(this);
        }

        private void ResetLabels()
        {
            // \u2215: ∕ division slash, which is similar to slash
            _labels = avatars.Select(x => new GUIContent(x.avatarName)).ToArray();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            ResetLabels();
        }

        static class Styles
        {
            public static GUIStyle TextStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        static class PerformanceIcons
        {
            private static Texture _excellent;
            private static Texture _good;
            private static Texture _medium;
            private static Texture _poor;
            private static Texture _veryPoor;

            public static Texture Excellent => _excellent ? _excellent : _excellent = LoadIcon("Great");
            public static Texture Good => _good ? _good : _good = LoadIcon("Good");
            public static Texture Medium => _medium ? _medium : _medium = LoadIcon("Medium");
            public static Texture Poor => _poor ? _poor : _poor = LoadIcon("Poor");
            public static Texture VeryPoor => _veryPoor ? _veryPoor : _veryPoor = LoadIcon("Horrible");

            public static Texture GetIconForPerformance(PerformanceRating rating)
            {
                switch (rating)
                {
                    case PerformanceRating.Excellent:
                        return Excellent;
                    case PerformanceRating.Good:
                        return Good;
                    case PerformanceRating.Medium:
                        return Medium;
                    case PerformanceRating.Poor:
                        return Poor;
                    case PerformanceRating.VeryPoor:
                        return VeryPoor;
                    case PerformanceRating.None:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static Texture LoadIcon(string texName)
            {
                return Resources.Load<Texture>($"PerformanceIcons/Perf_{texName}_32");
            }
        }

        /// For VRCSDK 3.3.0 or later, entering play mode is not performed during build.
        /// So, skipping computing on play mode after build callback is not needed.
        static class BuildAvatarInEditModeDetector
        {
            public static bool IsVrcSdkSupportsBuildingAvatarInEditMode;

            static BuildAvatarInEditModeDetector()
            {
                try
                {
                    // VRCSdkControlPanel is very old vrcsdk API so it's safe to access directly
                    // TryGetBuilder is new api in 3.3.0 which introduces VRChat SDK Public API
                    // so find for it
                    IsVrcSdkSupportsBuildingAvatarInEditMode = typeof(VRCSdkControlPanel).GetMethods().Any(x => x.Name == "TryGetBuilder");
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to detect VRCSDK public API.");
                    Debug.LogException(e);
                    IsVrcSdkSupportsBuildingAvatarInEditMode = false;
                }
            }
        }

        enum ShowingTarget
        {
            PC,
            Android,
        }

        [Serializable]
        private class AvatarPerformanceInfoSet
        {
            [SerializeField] [NotNull] public string avatarName;
            [SerializeField] [NotNull] public AvatarPerformanceInfo pc;
            [SerializeField] [NotNull] public AvatarPerformanceInfo android;

            [NotNull] public AvatarPerformanceInfo Current => PerformanceRankComputer.CurrentIsMobile ? android : pc;

            public AvatarPerformanceInfoSet(string avatarName, AvatarPerformanceInfo pc = null, AvatarPerformanceInfo android = null)
            {
                this.avatarName = avatarName;
                this.pc = pc ?? new AvatarPerformanceInfo();
                this.android = android ?? new AvatarPerformanceInfo();
            }
        }

        [Serializable]
        private class AvatarPerformanceInfo
        {
            [SerializeField] [NotNull] public PerformanceInfo[] info;
            [SerializeField] public PerformanceInfo overall;
            public bool Valid => info.Length != 0;

            public AvatarPerformanceInfo([NotNull] PerformanceInfo[] info, PerformanceInfo overall)
            {
                if (info == null) throw new ArgumentNullException(nameof(info));
                Array.Sort(info, (x, y) => x.rating != y.rating ? -x.rating.CompareTo(y.rating) : x.category.CompareTo(y.category));
                this.info = info;
                this.overall = overall;
            }

            public AvatarPerformanceInfo()
            {
                info = Array.Empty<PerformanceInfo>();
                overall = default;
            }
        }

        [Serializable]
        private struct PerformanceInfo
        {
            public AvatarPerformanceCategory category;
            public string categoryName;
            public string data;
            public PerformanceRating rating;
        }

        private class ActualPerformanceCallback : IVRCSDKPreprocessAvatarCallback
        {
            // run at last
            public int callbackOrder => int.MaxValue;

            public bool OnPreprocessAvatar(GameObject avatarGameObject)
            {
                var window = GetWindowInstance();
                window.MarkBuilding();
                var name = avatarGameObject.name;

                // strip (Clone) at end
                if (name.EndsWith("(Clone)", StringComparison.Ordinal))
                    name = name.Substring(0, name.Length - "(Clone)".Length);

                var info = new AvatarPerformanceInfoSet(
                    name,
                    pc: window.calculatePc ? PerformanceRankComputer.Compute(avatarGameObject, false) : null,
                    android: window.calculateAndroid ? PerformanceRankComputer.Compute(avatarGameObject, true) : null
                    );
                window.AddInfo(info);
                if (info.Current.Valid) UpdateFallbackStatus(avatarGameObject, info.Current);
                return true;
            }

            private void UpdateFallbackStatus(GameObject avatarGameObject, AvatarPerformanceInfo info)
            {
                var pipeline = avatarGameObject.GetComponent<PipelineManager>();
                if (pipeline == null) return;

                if (pipeline.fallbackStatus != PipelineManager.FallbackStatus.InvalidPerformance) return;

                if (info.overall.rating <= PerformanceRating.Good)
                    pipeline.fallbackStatus = PipelineManager.FallbackStatus.Valid;
            }
        }

        private static class PerformanceRankComputer
        {
            private static readonly Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string>>
                Computers = new Dictionary<AvatarPerformanceCategory, Func<AvatarPerformanceStats, string>>();

            static PerformanceRankComputer()
            {
                void AddCategory(string categoryName, params Func<dynamic, string>[] computers)
                {
                    if (Enum.TryParse(categoryName, out AvatarPerformanceCategory category))
                    {
                        if (!Computers.ContainsKey(category))
                        {
                            Computers.Add(category, stats =>
                            {
                                foreach (var computer in computers)
                                {
                                    try
                                    {
                                        return computer(stats);
                                    }
                                    catch
                                    {
                                        // ignored
                                    }
                                }

                                return "(unknown)";
                            });
                        }
                    }
                }

                string ToStringBytes(int? bytes) => bytes is int asInt ? $"{asInt / 1024.0 / 1024.0:F2}MB" : "(unknown)";
                string ToStringMegabytes(float? megabytes) => megabytes is float asFloat ? $"{asFloat:F2}MB" : "(unknown)";

                string ToStringCount(int? value) => value?.ToString() ?? "(unknown)";
                string ToStringCountZero(int? value) => value?.ToString() ?? "0";

                string ToStringEnabled(bool? value) =>
                    value is bool flag ? (flag ? "Enabled" : "Disabled") : "(unknown)";

                AddCategory("DownloadSize", stats => ToStringBytes(stats.downloadSizeBytes), stats => ToStringMegabytes(stats.downloadSize));
                AddCategory("UncompressedSize", stats => ToStringBytes(stats.uncompressedSizeBytes), stats => ToStringMegabytes(stats.uncompressedSize));
                AddCategory("PolyCount", stats => ToStringCount(stats.polyCount));
                AddCategory("AABB", stats => stats.aabb != null ? stats.aabb.ToString() : "(unknown)");
                AddCategory("SkinnedMeshCount", stats => ToStringCount(stats.skinnedMeshCount));
                AddCategory("MeshCount", stats => ToStringCount(stats.meshCount));
                AddCategory("MaterialCount", stats => ToStringCount(stats.materialCount));
                AddCategory("DynamicBoneComponentCount", stats => ToStringCountZero(stats.dynamicBone?.componentCount));
                AddCategory("DynamicBoneSimulatedBoneCount", stats => ToStringCountZero(stats.dynamicBone?.transformCount));
                AddCategory("DynamicBoneColliderCount", stats => ToStringCountZero(stats.dynamicBone?.colliderCount));
                AddCategory("DynamicBoneCollisionCheckCount", stats => ToStringCountZero(stats.dynamicBone?.collisionCheckCount));
                AddCategory("PhysBoneComponentCount", stats => ToStringCountZero(stats.physBone?.componentCount));
                AddCategory("PhysBoneTransformCount", stats => ToStringCountZero(stats.physBone?.transformCount));
                AddCategory("PhysBoneColliderCount", stats => ToStringCountZero(stats.physBone?.colliderCount));
                AddCategory("PhysBoneCollisionCheckCount", stats => ToStringCountZero(stats.physBone?.collisionCheckCount));
                AddCategory("ContactCount", stats => ToStringCount(stats.contactCount));
                AddCategory("AnimatorCount", stats => ToStringCount(stats.animatorCount));
                AddCategory("BoneCount", stats => ToStringCount(stats.boneCount));
                AddCategory("LightCount", stats => ToStringCount(stats.lightCount));
                AddCategory("ParticleSystemCount", stats => ToStringCount(stats.particleSystemCount));
                AddCategory("ParticleTotalCount", stats => ToStringCount(stats.particleTotalCount));
                AddCategory("ParticleMaxMeshPolyCount", stats => ToStringCount(stats.particleMaxMeshPolyCount));
                AddCategory("ParticleTrailsEnabled", stats => ToStringEnabled(stats.particleTrailsEnabled));
                AddCategory("ParticleCollisionEnabled", stats => ToStringEnabled(stats.particleCollisionEnabled));
                AddCategory("TrailRendererCount", stats => ToStringCount(stats.trailRendererCount));
                AddCategory("LineRendererCount", stats => ToStringCount(stats.lineRendererCount));
                AddCategory("ClothCount", stats => ToStringCount(stats.clothCount));
                AddCategory("ClothMaxVertices", stats => ToStringCount(stats.clothMaxVertices));
                AddCategory("PhysicsColliderCount", stats => ToStringCount(stats.physicsColliderCount));
                AddCategory("PhysicsRigidbodyCount", stats => ToStringCount(stats.physicsRigidbodyCount));
                AddCategory("AudioSourceCount", stats => ToStringCount(stats.audioSourceCount));
                AddCategory("TextureMegabytes", stats => ToStringMegabytes(stats.textureMegabytes));
                AddCategory("ConstraintsCount", stats => ToStringCount(stats.constraintsCount));
                AddCategory("ConstraintDepth", stats => ToStringCount(stats.constraintDepth));
            }

            public static AvatarPerformanceInfo Compute(GameObject avatarGameObject, bool isMobile)
            {
                var stats = new AvatarPerformanceStats(isMobile);
                AvatarPerformance.CalculatePerformanceStats(avatarGameObject.name, avatarGameObject, stats, isMobile);

                var info = new List<PerformanceInfo>();

                foreach (var category in Enum.GetValues(typeof(AvatarPerformanceCategory))
                             .Cast<AvatarPerformanceCategory>())
                {
                    var categoryName = TryGetCategoryName(category);
                    if (categoryName == null) continue;
                    info.Add(new PerformanceInfo
                    {
                        category = category,
                        categoryName = categoryName,
                        data = PerformanceData(stats, category),
                        rating = stats.GetPerformanceRatingForCategory(category),
                    });
                }

                var overall = new PerformanceInfo
                {
                    category = Overall,
                    categoryName = "Overall",
                    data = "",
                    rating = stats.GetPerformanceRatingForCategory(Overall),
                };

                return new AvatarPerformanceInfo(info.ToArray(), overall);
            }

            public static bool CurrentIsMobile
            {
                get => EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone;
            }

            private static string PerformanceData(AvatarPerformanceStats stats, AvatarPerformanceCategory category)
            {
                if (!Computers.TryGetValue(category, out var computer)) return "(unknown)";
                return computer(stats);
            }

            private static string TryGetCategoryName(AvatarPerformanceCategory category)
            {
                try
                {
                    return AvatarPerformanceStats.GetPerformanceCategoryDisplayName(category);
                }
                catch
                {
                    // GetPerformanceCategoryDisplayName may throw KeyNotFoundException
                    return null;
                }
            }
        }

        [InitializeOnLoad]
        private static class ComputeOnPlay
        {
            private const string EnableMenuName = "Tools/anatawa12 gists/Compute actual Performance on Play";
            private const string EnableSettingName = "com.anatawa12.gist.compute-actual-performance-on-play";
            private static bool _computeDone;

            public static bool Enabled
            {
                get => EditorPrefs.GetBool(EnableSettingName, true);
                set => EditorPrefs.SetBool(EnableSettingName, value);
            }

            static ComputeOnPlay()
            {
                EditorApplication.delayCall += () => Menu.SetChecked(EnableMenuName, Enabled);
                EditorApplication.update += OnUpdate;
                EditorApplication.playModeStateChanged += PlaymodeChanged;
            }

            private static void OnUpdate()
            {
                if (EditorApplication.isPlaying)
                {
                    if (_computeDone) return; // already computed
                    _computeDone = true;
                    if (!Enabled) return;
                    var window = GetWindowInstance();
                    if (window.IsBuilding)
                    {
                        Debug.Log("Skipping the computing because this play is avatar info input play");
                        return; // build is in progress
                    }

                    foreach (var vrcAvatarDescriptor in Enumerable.Range(0, SceneManager.sceneCount)
                                 .Select(SceneManager.GetSceneAt)
                                 .Where(x => x.isLoaded)
                                 .SelectMany(x => x.GetRootGameObjects())
                                 .SelectMany(x => x.GetComponentsInChildren<VRC_AvatarDescriptor>()))
                    {
                        var gameObject = vrcAvatarDescriptor.gameObject;
                        window.AddInfo(
                            new AvatarPerformanceInfoSet(gameObject.name + " (Play)",
                                pc: window.calculatePc ? PerformanceRankComputer.Compute(gameObject, false) : null,
                                android: window.calculateAndroid ? PerformanceRankComputer.Compute(gameObject, true) : null
                                ));
                    }
                }
                else
                {
                    _computeDone = false;
                }
            }

            private static void PlaymodeChanged(PlayModeStateChange obj)
            {
                if (obj != PlayModeStateChange.ExitingPlayMode) return;
                TryGetWindowInstance()?.ClearBuilding();
            }

            [MenuItem(EnableMenuName)]
            private static void ToggleApplyOnPlay()
            {
                Enabled = !Enabled;
                Menu.SetChecked(EnableMenuName, Enabled);
            }
        }
    }
}
#endif
