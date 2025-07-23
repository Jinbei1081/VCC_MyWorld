using TMPro;
using UnityEditor;

namespace ziston
{
    [InitializeOnLoad]
    public class FallbackFontChanger
    {
        static string DefaultFontAssetGUID = "22bd0859db8cc544c8e37024725ddca7";
        static string FallbackFontAssetGUID = "dc8ea9f4e6c38b84eb6c995f04d80e08";
        static TMP_FontAsset DefaultFontAsset => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(DefaultFontAssetGUID));
        static TMP_FontAsset FallbackFontAsset => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FallbackFontAssetGUID));

        static FallbackFontChanger()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            SetFallbackFont();
        }
        public static void SetFallbackFont()
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null) return;
            if (DefaultFontAsset == null || FallbackFontAsset == null) return;
            SerializedObject serializedObject = new SerializedObject(settings);
            serializedObject.Update();
            serializedObject.FindProperty("m_defaultFontAsset").objectReferenceValue = DefaultFontAsset;
            SerializedProperty fallbackFontAssets = serializedObject.FindProperty("m_fallbackFontAssets");
            fallbackFontAssets.arraySize = 1;
            fallbackFontAssets.GetArrayElementAtIndex(0).objectReferenceValue = FallbackFontAsset;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}