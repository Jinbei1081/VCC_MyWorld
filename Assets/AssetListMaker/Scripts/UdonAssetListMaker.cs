using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UdonAssetListMaker : UdonSharpBehaviour
{
    public TextAsset _assetListData;
    public int _lineHeight = 50;
    public int _paddingVertical = 10;
    public UnityEngine.UI.Scrollbar _verticalScrollbar;
    public GameObject _panelObject;
    private void Start()
    {
        if (_verticalScrollbar) _verticalScrollbar.value = 1;
    }
}