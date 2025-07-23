using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace yoshio_will.common
{
    public class TMPFontAssetBulkChanger : MonoBehaviour
    {
        [SerializeField] public TMP_FontAsset FontAsset;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TMPFontAssetBulkChanger))]
    public class TMPFontAssetBulkChangerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty fontAsset = serializedObject.FindProperty(nameof(TMPFontAssetBulkChanger.FontAsset));
            TMPFontAssetBulkChanger tgt = (TMPFontAssetBulkChanger)target;
            TextMeshProUGUI[] texts = tgt.transform.GetComponentsInChildren<TextMeshProUGUI>(true);

            if (fontAsset.objectReferenceValue == null) fontAsset.objectReferenceValue = texts[0].font;

            EditorGUILayout.PropertyField(fontAsset);

            if (GUILayout.Button("↓ SET ↓"))
            {
                EditorGUI.BeginChangeCheck();
                foreach (var text in texts)
                {
                    SerializedObject so = new SerializedObject(text);
                    SerializedProperty prop = so.FindProperty("m_fontAsset");
                    prop.objectReferenceValue = fontAsset.objectReferenceValue;
                    so.ApplyModifiedProperties();
                }
                EditorGUI.EndChangeCheck();
            }

            foreach (var text in texts)
            {
                EditorGUILayout.ObjectField(text.name, text.font, typeof(TMP_FontAsset), false);
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
#endif
}