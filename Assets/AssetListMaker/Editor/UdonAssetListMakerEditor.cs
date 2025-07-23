using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using TMPro;

[CustomEditor(typeof(UdonAssetListMaker))]
public class UdonAssetListMakerEditor : Editor
{
    private UdonAssetListMaker _assetListMaker;
    private SerializedProperty _assetListData;
    private SerializedProperty _lineHeight;
    private SerializedProperty _paddingVertical;
    private SerializedProperty _verticalScrollbar;
    private SerializedProperty _panelObject;
    private GameObject _assetList;
    private Transform _panelTransform;
    static string pageTitlePrefabGUID = "1bf31dd8844495a499adf9926da9d433";
    static string urlPrefabGUID = "5e5f6ebd1ad2f6c4694b088b2eb2453d";
    private float sleepTime = 1.5f;
    static GameObject pageTitlePrefab
    {
        get => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pageTitlePrefabGUID));
    }
    static GameObject urlPrefab
    {
        get => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(urlPrefabGUID));
    }

    private void OnEnable()
    {
        _assetListMaker = (UdonAssetListMaker)target;
        _assetListData = serializedObject.FindProperty(nameof(_assetListMaker._assetListData));
        _verticalScrollbar = serializedObject.FindProperty(nameof(_assetListMaker._verticalScrollbar));
        _lineHeight = serializedObject.FindProperty(nameof(_assetListMaker._lineHeight));
        _paddingVertical = serializedObject.FindProperty(nameof(_assetListMaker._paddingVertical));
        _panelObject = serializedObject.FindProperty(nameof(_assetListMaker._panelObject));
        _assetList = _assetListMaker.transform.parent.gameObject;
    }
    public override void OnInspectorGUI()
    {
        if(UdonSharpGUI.DrawProgramSource(target)) return;
        serializedObject.Update();
        EditorGUILayout.PropertyField(_assetListData);
        EditorGUILayout.PropertyField(_lineHeight);
        EditorGUILayout.PropertyField(_paddingVertical);
        EditorGUILayout.PropertyField(_verticalScrollbar);
        EditorGUILayout.PropertyField(_panelObject);
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Make Asset List", GUILayout.Width(200), GUILayout.Height(40))) {
            MakeAssetList();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    private void MakeAssetList()
    {
        _panelTransform = ((GameObject)_panelObject.objectReferenceValue).transform;
        string rawText = ((TextAsset)_assetListData.objectReferenceValue).text;
        ziston.FallbackFontChanger.SetFallbackFont();
        rawText = rawText.Trim();
        rawText = rawText.Replace("\r\n", "\n");
        string[] urls;
        urls = rawText.Split('\n');
        for (int i = _panelTransform.childCount - 1; i >= 0; i--) {
            GameObject.DestroyImmediate(_panelTransform.GetChild(i).gameObject);
        }
        int cnt = 0;
        for (int i = 0; i < urls.Length; i++) {
            if(urls[i] == "") continue;
            urls[i] = urls[i].Trim();
            if (!urls[i].StartsWith("http://") && !urls[i].StartsWith("https://")) continue;
            GetURLs(urls[i], cnt);
            cnt++;
            System.Threading.Thread.Sleep((int)(sleepTime * 1000));
        }
        RectTransform panelRect = _panelTransform.GetComponent<RectTransform>();
        if (panelRect) panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, cnt * _lineHeight.intValue * 2 + _paddingVertical.intValue * 2);
    }
    async void GetURLs(string url, int cnt)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<UnityEngine.Networking.UnityWebRequest>();
        var request = UnityEngine.Networking.UnityWebRequest.Get(url);
        var async = request.SendWebRequest();
        async.completed += _ => tcs.SetResult(async.webRequest);
        var response = await tcs.Task;

        if (response.isHttpError || response.isNetworkError) return;
        GameObject pageTitleObject = (GameObject)PrefabUtility.InstantiatePrefab(pageTitlePrefab);
        GameObject urlObject = (GameObject)PrefabUtility.InstantiatePrefab(urlPrefab);
        Vector3 position;

        pageTitleObject.name = "PageTitle (" + cnt + ")";
        pageTitleObject.transform.SetParent(_panelTransform);
        pageTitleObject.transform.SetSiblingIndex(cnt * 2 + 1);

        position = pageTitleObject.transform.localPosition;
        position.z = 0;
        pageTitleObject.transform.localPosition = position;
        pageTitleObject.transform.localRotation = Quaternion.identity;
        pageTitleObject.transform.localScale = Vector3.one;

        TextMeshProUGUI title = pageTitleObject.GetComponent<TextMeshProUGUI>();
        
        urlObject.name = "URL (" + cnt + ")";
        urlObject.transform.SetParent(_panelTransform);
        urlObject.transform.SetSiblingIndex(cnt * 2 + 2); 
        
        position = urlObject.transform.localPosition;
        position.z = 0;
        urlObject.transform.localPosition = position;
        urlObject.transform.localRotation = Quaternion.identity;
        urlObject.transform.localScale = Vector3.one;
        
        TMP_InputField urlText = urlObject.GetComponent<TMP_InputField>();

        System.Text.RegularExpressions.Regex r =
            new System.Text.RegularExpressions.Regex(
                @"<(title)\b[^>]*>(.*?)</\1>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Singleline);

        System.Text.RegularExpressions.Match m = r.Match(request.downloadHandler.text);
        if (title) {
            string titleText = m.Groups[2].Value;
            titleText = titleText.Replace("&#39;", "\'");
            titleText = titleText.Replace("&quot;", "\"");
            titleText = titleText.Replace("&yen;", "\\");
            title.text = titleText;
        }
        if (urlText) urlText.text = url;
    }
}