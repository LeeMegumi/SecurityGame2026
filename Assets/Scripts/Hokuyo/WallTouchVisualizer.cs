using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 牆面觸控 UI 視覺化 v2
///
/// 修正：WallToCanvas() 改用固定參考尺寸 1920×1080 當除數，
///       與 WallGizmoCanvas 的 MmToPanel() 邏輯完全一致，
///       確保藍點位置與紅點位置精準對齊。
///
/// 建立步驟：
///  1. 建立 Canvas（Screen Space - Overlay），解析度 1920×1080
///  2. 建立空 GameObject 掛此腳本
///  3. 建立一個圓形 Image Prefab，拖入 touchPointPrefab
///  4. 將 Canvas RectTransform 拖入 canvasRect
/// </summary>
public class WallTouchVisualizer : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public WallTouchDetector detector;
    [Header("視覺物件-")]
    public RectTransform     touchPointPrefab;
    [Header("UI觸發物件-")]
    public RectTransform     triggerPointPrefab;
    public RectTransform     USTcanvasRect;
    public RectTransform     resizeCanvasRect;

    [Header("─── 視覺設定 ───")]
    public Color USTtouchColor   = new Color(0f, 0.8f, 1f, 0.85f);
    public Color triggerTouchColor   = new Color(0f, 0f, 0f, 0.85f);
    public float touchDotSize = 60f;
    public float lerpSpeed    = 15f;

    public UITo3DSpawner spawner;

    [Header("UI觸發物件可視化")]
    public bool triggerVisualDebug;

    // ── 固定參考尺寸（與 WallGizmoCanvas 保持一致）────────
    // Canvas 解析度 1920×1080，所有 mm 座標依此比例換算
    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    private List<RectTransform> UST_dots = new List<RectTransform>();
    private List<RectTransform> resizeTrigger_dots = new List<RectTransform>();

    // ═══════════════════════════════════════════════════
    void Start()
    {
        if (detector == null) { Debug.LogError("[Visualizer] 請指定 WallTouchDetector"); return; }
        detector.onTouchDown.AddListener(SyncDots);
        detector.onTouchMove.AddListener(SyncDots);
        detector.onTouchUp.AddListener(HideAllDots);
    }

    // ═══════════════════════════════════════════════════
    private void SyncDots(List<Vector2> points)
    {
        // 物件池：不足時才新增
        while (UST_dots.Count < points.Count)
        {
            var dot = Instantiate(touchPointPrefab, USTcanvasRect);
            dot.sizeDelta = Vector2.one * touchDotSize;
            var img = dot.GetComponent<Image>();
          
            if (img != null) img.color = USTtouchColor;
           
            UST_dots.Add(dot);
        }

        for (int i = 0; i < UST_dots.Count; i++)
        {
            if (i < points.Count)
            {
                UST_dots[i].gameObject.SetActive(true);

                Vector2 target = WallToCanvas(points[i]);
                Vector2 screenTarget = target + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                spawner.SpawnBulletAtUIPosition(screenTarget);

                UST_dots[i].anchoredPosition = Vector2.Lerp(
                    UST_dots[i].anchoredPosition, target, Time.deltaTime * lerpSpeed);
            }
            else
            {
                UST_dots[i].gameObject.SetActive(false);
            }
        }

        while (resizeTrigger_dots.Count < points.Count)
        {
            var dot = Instantiate(triggerPointPrefab, resizeCanvasRect);

            dot.sizeDelta = Vector2.one * touchDotSize;
            var img = dot.GetComponent<Image>();
            var alphaGroup = dot.GetComponent<CanvasGroup>();
            if (img != null) img.color = triggerTouchColor;
            if (alphaGroup != null) alphaGroup.alpha = triggerVisualDebug == true ? 1 : 0;
            resizeTrigger_dots.Add(dot);
        }

        for (int i = 0; i < resizeTrigger_dots.Count; i++)
        {
            if (i < points.Count)
            {
                resizeTrigger_dots[i].gameObject.SetActive(true);
                var alphaGroup = resizeTrigger_dots[i].GetComponent<CanvasGroup>();
                if (alphaGroup != null) alphaGroup.alpha = triggerVisualDebug == true ? 1 : 0;
                Vector2 target = WallToCanvas(points[i]);
                Vector2 screenTarget = target + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                spawner.SpawnBulletAtUIPosition(screenTarget);

                resizeTrigger_dots[i].anchoredPosition = Vector2.Lerp(
                    resizeTrigger_dots[i].anchoredPosition, target, Time.deltaTime * lerpSpeed);
            }
            else
            {
                resizeTrigger_dots[i].gameObject.SetActive(false);
            }
        }

    }

    // ═══════════════════════════════════════════════════
    private void HideAllDots()
    {
        foreach (var d in UST_dots) d.gameObject.SetActive(false);
        foreach (var d in resizeTrigger_dots) d.gameObject.SetActive(false);

    }

    // ═══════════════════════════════════════════════════
    // ─── 座標轉換：牆面 mm → Canvas anchoredPosition ───
    //
    //  canvasRect 的 Pivot = (0.5, 0.5)（中心點），
    //  所以 anchoredPosition (0,0) = 螢幕正中央。
    //
    //  修正前（錯誤）：
    //    nx = wallPosMm.x / detector.wallWidth   ← wallHeight 不等於 1080 就偏移
    //    ny = wallPosMm.y / detector.wallHeight
    //
    //  修正後（正確）：
    //    nx = wallPosMm.x / REF_W               ← 固定 1920，與 WallGizmoCanvas 一致
    //    ny = wallPosMm.y / REF_H               ← 固定 1080，Y 軸不再偏差
    // ═══════════════════════════════════════════════════
    private Vector2 WallToCanvas(Vector2 wallPosMm)
    {
        float cw = USTcanvasRect.rect.width;
        float ch = USTcanvasRect.rect.height;

        float nx = (wallPosMm.x - detector.rectX) / detector.rectWidth;
        float ny = (wallPosMm.y - detector.rectY) / detector.rectHeight;

        return new Vector2(
            ((nx - 0.5f) * cw),
            ((ny - 0.5f) * ch));
        //wallPosMm.x = detector的rectX 到 rectX + rectWidth(10)(10+2980) 
        //wallPosMm.y = detector的rectY 到 rectY + rectHeight (450)(450+1680)

        //轉換成>>
        //X軸 -(canvasRect.width/2) = -960 到 (canvasRect.width/2) = 960
        //Y軸 -(canvasRect.height/2) = -540 到 (canvasRect.height/2) = 540

        //X 10 = -960 X 2990 = 960
        //Y 450 = -540 Y 2130 = 540
    }

    // ═══════════════════════════════════════════════════
    void OnDestroy()
    {
        if (detector == null) return;
        detector.onTouchDown.RemoveListener(SyncDots);
        detector.onTouchMove.RemoveListener(SyncDots);
        detector.onTouchUp.RemoveListener(HideAllDots);
    }
}
