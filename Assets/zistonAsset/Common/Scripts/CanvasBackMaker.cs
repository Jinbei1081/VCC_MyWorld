
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CanvasBackMaker : UdonSharpBehaviour
{
    [SerializeField] private float _thickness = 0.01f;
    [SerializeField] private CanvasRenderer _canvas = null;
    void Start()
    {
        if (_canvas) {
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Renderer[] canvasBackRends = GetComponentsInChildren<Renderer>(true);
            if (canvasBackRends != null && canvasBackRends[0]) {
                UnityEngine.UI.Image canvasImage = _canvas.GetComponent<UnityEngine.UI.Image>();
                if (canvasImage) {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetColor("_Color", canvasImage.color);
                    canvasBackRends[0].SetPropertyBlock(block);
                }
                canvasBackRends[0].transform.position = _canvas.transform.position;
                canvasBackRends[0].transform.rotation = _canvas.transform.rotation;
                canvasBackRends[0].transform.localScale = new Vector3(_canvas.transform.localScale.x * canvasRect.rect.size.x, _canvas.transform.localScale.y * canvasRect.rect.size.y, _thickness / transform.parent.lossyScale.z);
                canvasBackRends[0].gameObject.SetActive(true);
            }
        }
    }
}
