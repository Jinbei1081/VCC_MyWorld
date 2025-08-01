﻿#if VRC_SDK_VRCSDK3
#define VRWT_IS_VRC
#endif

#if VRWT_IS_VRC
using VRC.SDKBase;
using VRC.Core;
#endif

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRWorldToolkit.Editor
{
    public class QuickFunctions : EditorWindow
    {
#if VRWT_IS_VRC
        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", false, 15)]
        public static void CopyWorldID()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager) EditorGUIUtility.systemCopyBuffer = pipelineManager.blueprintId;
            }
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", true)]
        private static bool CopyWorldIDValidate()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager) return pipelineManager.blueprintId.Length > 0;
            }

            return false;
        }
        
        [MenuItem("VRWorld Toolkit/Quick Functions/Open VRChat Worlds Build Folder", false, 16)]
        public static void OpenBuildFolder()
        {
#if UNITY_EDITOR_WIN
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localLowPath = Path.Combine(userProfilePath, "AppData", "LocalLow", "VRChat", "VRChat", "Worlds");

            if (Directory.Exists(localLowPath)) {
                System.Diagnostics.Process.Start("explorer.exe", localLowPath.Replace("/", "\\"));
            }
            else
            {
                EditorUtility.DisplayDialog("Folder Not Found", "The VRChat worlds build folder does not exist. It's possible you haven't made a build using the VRChat SDK yet.", "Ok");
            }
#else
            EditorUtility.DisplayDialog("Info", "This function is only supported while using the Unity Editor on Windows at this time. No action was taken.", "Ok");
#endif
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", false, 17)]
        public static void SetupLayersCollisionMatrix()
        {
            if (!UpdateLayers.AreLayersSetup()) UpdateLayers.SetupEditorLayers();

            if (!UpdateLayers.IsCollisionLayerMatrixSetup()) UpdateLayers.SetupCollisionLayerMatrix();
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", true)]
        private static bool SetupLayersCollisionMatrixValidate()
        {
            return !UpdateLayers.AreLayersSetup() || !UpdateLayers.IsCollisionLayerMatrixSetup();
        }
#endif

        [MenuItem("VRWorld Toolkit/Quick Functions/Remove Missing Scripts from Scene", false, 18)]
        private static void FindAndRemoveMissingScripts()
        {
            if (EditorUtility.DisplayDialog("Remove Missing Scripts", "Running this will go through all GameObjects in the open scene and remove any components with missing scripts. This action can't be reversed!\n\nAre you sure you want to continue?", "Continue", "Cancel"))
            {
                var overallRemovedCount = 0;
                var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                var allGameObjectsLength = allGameObjects.Length;
                for (var i = 0; i < allGameObjectsLength; i++)
                {
                    var gameObject = allGameObjects[i] as GameObject;

                    if (gameObject != null && (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject))) continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Checking For Missing Scripts", gameObject.name, (float) i / allGameObjectsLength)) break;

                    var removedCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                    if (removedCount > 0)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
                        overallRemovedCount += removedCount;
                    }
                }

                EditorUtility.ClearProgressBar();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                var message = overallRemovedCount > 0 ? $"Removed total of {overallRemovedCount} components with missing scripts." : "No components with missing scripts were found.";
                EditorUtility.DisplayDialog("Remove Missing Scripts", message, "Ok");
            }
        }
    }
}