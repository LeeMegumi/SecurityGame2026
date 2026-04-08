using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 牆面觸控 UI 視覺化
/// 在 Canvas 上顯示觸碰點圓圈
///
/// 建立步驟：
///   1. 建立 Canvas（Screen Space - Overlay）
///   2. 建立空 GameObject 掛此腳本
///   3. 建立一個圓形 Image Prefab，拖入 touchPointPrefab
///   4. 將 Canvas RectTransform 拖入 canvasRect
/// </summary>
public class WallTouchVisualizer : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public WallTouchDetector detector;
    public RectTransform touchPointPrefab;
    public RectTransform canvasRect;

    [Header("─── 視覺設定 ───")]
    public Color touchColor   = new Color(0f, 0.8f, 1f, 0.85f);
    public float touchDotSize = 60f;
    public float lerpSpeed    = 15f;

    private List<RectTransform> _dots = new List<RectTransform>();

    public UITo3DSpawner spawner;
    void Start()
    {
        if (detector == null) { Debug.LogError("[Visualizer] 請指定 WallTouchDetector"); return; }
        detector.onTouchDown.AddListener(SyncDots);
        detector.onTouchMove.AddListener(SyncDots);
        detector.onTouchUp.AddListener(HideAllDots);
    }

    private void SyncDots(List<Vector2> points)
    {
        while (_dots.Count < points.Count)
        {
            var dot = Instantiate(touchPointPrefab, canvasRect);
            dot.sizeDelta = Vector2.one * touchDotSize;
            var img = dot.GetComponent<Image>();
            if (img != null) img.color = touchColor;
            _dots.Add(dot);
        }
        for (int i = 0; i < _dots.Count; i++)
        {
            if (i < points.Count)
            {
                _dots[i].gameObject.SetActive(true);
                Vector2 target = WallToCanvas(points[i]);
                Vector2 MouseTargetPos = target+new Vector2(Screen.width/2, Screen.height/2); 
                spawner.SpawnCubeAtUIPosition(MouseTargetPos);
                _dots[i].anchoredPosition = Vector2.Lerp(
                    _dots[i].anchoredPosition, target, Time.deltaTime * lerpSpeed);
            }
            else _dots[i].gameObject.SetActive(false);
        }
    }

    private void HideAllDots()
    {
        foreach (var d in _dots) d.gameObject.SetActive(false);
    }

    private Vector2 WallToCanvas(Vector2 wallPosMm)
    {
        float nx = wallPosMm.x / detector.wallWidth;
        float ny = wallPosMm.y / detector.wallHeight;
        return new Vector2(
            (nx - 0.5f) * canvasRect.rect.width,
            (ny - 0.5f) * canvasRect.rect.height);
    }

    void OnDestroy()
    {
        if (detector == null) return;
        detector.onTouchDown.RemoveListener(SyncDots);
        detector.onTouchMove.RemoveListener(SyncDots);
        detector.onTouchUp.RemoveListener(HideAllDots);
    }
}
