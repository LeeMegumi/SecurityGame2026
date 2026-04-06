using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WallGizmoCanvas — 將 WallTouchDetector 的 Gizmos 視覺化即時顯示在 Canvas 上
///
/// ━━━━ 顯示內容 ━━━━
///   🟩 綠色框    = 牆面總範圍
///   ⚪ 白點      = 感測器位置
///   🟡 黃色扇形線 = 掃描範圍示意
///   🟨 黃色框    = 矩形偵測區域（enableDetectionRect 開啟時）
///   🔴 紅點      = 即時觸碰點（每幀更新）
///
/// ━━━━ Canvas 設定建議 ━━━━
///   1. 在場景中建立一個 Canvas（Screen Space - Overlay）
///   2. 在 Canvas 下建立一個 Panel，作為「牆面視覺化區域」
///   3. 將此 Panel 的 Pivot 設為 (0, 0)（左下角）→ Inspector 右上 Pivot 欄位
///   4. 將此 Panel 的 Image color 設為半透明深色（方便看到線條）
///   5. 將此 Panel 的 RectTransform 拖入 wallPanel 欄位
/// </summary>
public class WallGizmoCanvas : MonoBehaviour
{
    [Header("─── 參考元件 ───")]
    [Tooltip("WallTouchDetector 元件參考")]
    public WallTouchDetector detector;
    [Tooltip("代表牆面視覺化區域的 Panel（Pivot 請設為 (0,0) 左下角）")]
    public RectTransform wallPanel;

    [Header("─── 顯示開關 ───")]
    public bool showWallBorder    = true;
    public bool showSensor        = true;
    public bool showScanLines     = true;
    public bool showDetectionRect = true;
    public bool showTouchPoints   = true;

    [Header("─── 顏色設定 ───")]
    public Color wallBorderColor    = new Color(0.0f, 1.0f, 0.3f, 0.7f);
    public Color sensorColor        = Color.white;
    public Color scanLineColor      = new Color(1.0f, 1.0f, 0.0f, 0.35f);
    public Color detectionRectColor = new Color(1.0f, 0.9f, 0.0f, 0.9f);
    public Color touchColor         = new Color(1.0f, 0.15f, 0.15f, 0.9f);

    [Header("─── 尺寸設定 ───")]
    [Tooltip("所有線條的寬度 (px)")]
    public float lineWidth        = 2f;
    [Tooltip("觸碰點大小 (px)")]
    public float touchDotSize     = 24f;
    [Tooltip("感測器標記點大小 (px)")]
    public float sensorDotSize    = 14f;
    [Tooltip("每隔幾個 step 畫一條掃描線")]
    public int   scanLineInterval = 50;
    [Tooltip("掃描線長度 (mm)，0 = 自動計算至牆面對角長度")]
    public float scanLineLengthMm = 0f;

    // ── 動態建立的 UI 物件 ──────────────────────────────
    private RectTransform[]       _wallBorderLines = new RectTransform[4];
    private RectTransform         _sensorDot;
    private List<RectTransform>   _scanLines       = new List<RectTransform>();
    private RectTransform[]       _detRectLines    = new RectTransform[4];
    private List<RectTransform>   _touchDots       = new List<RectTransform>();

    private RectTransform _scanLineContainer;
    private RectTransform _touchDotContainer;

    // ── 快取（用來判斷是否需要重建掃描線）──────────────
    private float _prevSensorX, _prevSensorY;
    private int   _prevStartStep = -1, _prevEndStep = -1;
    private bool  _initialized  = false;

    // ═══════════════════════════════════════════════════
    void Start()
    {
        if (detector  == null) { Debug.LogError("[GizmoCanvas] 請在 Inspector 指定 WallTouchDetector"); return; }
        if (wallPanel == null) { Debug.LogError("[GizmoCanvas] 請在 Inspector 指定 wallPanel");         return; }

        Canvas.ForceUpdateCanvases(); // 確保 Canvas Layout 已完成
        BuildAllElements();
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        UpdateWallBorder();
        UpdateSensorDot();

        if (ShouldRebuildScanLines()) RebuildScanLines();
        else SetContainerActive(_scanLineContainer, showScanLines);

        UpdateDetectionRect();
        UpdateTouchDots();
    }

    // ═══════════════════════════════════════════════════
    // 初始化：建立所有 UI 物件
    // ═══════════════════════════════════════════════════
    private void BuildAllElements()
    {
        // ── 掃描線容器 ──────────────────────────────────
        _scanLineContainer = CreateFullStretchContainer("ScanLines");

        // ── 牆面邊框（4條線）──────────────────────────
        for (int i = 0; i < 4; i++)
            _wallBorderLines[i] = CreateLine($"WallBorder_{i}", wallPanel, wallBorderColor);

        // ── 感測器標記點 ────────────────────────────────
        _sensorDot = CreateDot("SensorDot", wallPanel, sensorColor, sensorDotSize);

        // ── 矩形偵測區域（4條線）──────────────────────
        for (int i = 0; i < 4; i++)
            _detRectLines[i] = CreateLine($"DetRect_{i}", wallPanel, detectionRectColor);

        // ── 觸碰點容器 ──────────────────────────────────
        _touchDotContainer = CreateFullStretchContainer("TouchDots");

        // ── 建立初始掃描線 ──────────────────────────────
        RebuildScanLines();
    }

    // ═══════════════════════════════════════════════════
    // 每幀更新：牆面邊框
    // ═══════════════════════════════════════════════════
    private void UpdateWallBorder()
    {
        bool active = showWallBorder;
        foreach (var l in _wallBorderLines) l.gameObject.SetActive(active);
        if (!active) return;

        float pw = wallPanel.rect.width;
        float ph = wallPanel.rect.height;

        // 底邊、頂邊、左邊、右邊
        SetLineTransform(_wallBorderLines[0], new Vector2(0,  0 ), new Vector2(pw,  0 ));
        SetLineTransform(_wallBorderLines[1], new Vector2(0,  ph), new Vector2(pw,  ph));
        SetLineTransform(_wallBorderLines[2], new Vector2(0,  0 ), new Vector2(0,   ph));
        SetLineTransform(_wallBorderLines[3], new Vector2(pw, 0 ), new Vector2(pw,  ph));
    }

    // ═══════════════════════════════════════════════════
    // 每幀更新：感測器標記點
    // ═══════════════════════════════════════════════════
    private void UpdateSensorDot()
    {
        _sensorDot.gameObject.SetActive(showSensor);
        if (showSensor)
            _sensorDot.anchoredPosition = WallToPanel(detector.sensorX, detector.sensorY);
    }

    // ═══════════════════════════════════════════════════
    // 掃描線：判斷是否需要重建
    // ═══════════════════════════════════════════════════
    private bool ShouldRebuildScanLines()
    {
        return !Mathf.Approximately(_prevSensorX,  detector.sensorX)    ||
               !Mathf.Approximately(_prevSensorY,  detector.sensorY)    ||
               _prevStartStep != detector.wallStartStep                 ||
               _prevEndStep   != detector.wallEndStep;
    }

    // ═══════════════════════════════════════════════════
    // 掃描線：重建扇形線條
    // ═══════════════════════════════════════════════════
    private void RebuildScanLines()
    {
        // 清除舊的
        foreach (var t in _scanLines) if (t != null) Destroy(t.gameObject);
        _scanLines.Clear();

        if (showScanLines)
        {
            Vector2 sensorPanel = WallToPanel(detector.sensorX, detector.sensorY);

            float maxDistMm = scanLineLengthMm > 0
                ? scanLineLengthMm
                : Mathf.Sqrt(detector.wallWidth  * detector.wallWidth +
                             detector.wallHeight * detector.wallHeight);

            for (int step = detector.wallStartStep; step <= detector.wallEndStep; step += scanLineInterval)
            {
                float angleRad = (step - 540) * 0.25f * Mathf.Deg2Rad;
                float endWallX = detector.sensorX + Mathf.Sin(angleRad) * maxDistMm;
                float endWallY = detector.sensorY - Mathf.Cos(angleRad) * maxDistMm;
                Vector2 endPanel = WallToPanel(endWallX, endWallY);

                var line = CreateLine($"ScanLine_{step}", _scanLineContainer, scanLineColor);
                SetLineTransform(line, sensorPanel, endPanel);
                _scanLines.Add(line);
            }
        }

        // 快取目前值
        _prevSensorX   = detector.sensorX;
        _prevSensorY   = detector.sensorY;
        _prevStartStep = detector.wallStartStep;
        _prevEndStep   = detector.wallEndStep;
    }

    // ═══════════════════════════════════════════════════
    // 每幀更新：矩形偵測區域
    // ═══════════════════════════════════════════════════
    private void UpdateDetectionRect()
    {
        bool active = showDetectionRect && detector.enableDetectionRect;
        foreach (var l in _detRectLines) l.gameObject.SetActive(active);
        if (!active) return;

        // 4 個角點（牆面 mm → 面板 px）
        Vector2 bl = WallToPanel(detector.rectX,                      detector.rectY                       );
        Vector2 br = WallToPanel(detector.rectX + detector.rectWidth,  detector.rectY                       );
        Vector2 tl = WallToPanel(detector.rectX,                      detector.rectY + detector.rectHeight  );
        Vector2 tr = WallToPanel(detector.rectX + detector.rectWidth,  detector.rectY + detector.rectHeight  );

        SetLineTransform(_detRectLines[0], bl, br); // 底邊
        SetLineTransform(_detRectLines[1], tl, tr); // 頂邊
        SetLineTransform(_detRectLines[2], bl, tl); // 左邊
        SetLineTransform(_detRectLines[3], br, tr); // 右邊
    }

    // ═══════════════════════════════════════════════════
    // 每幀更新：觸碰點（物件池）
    // ═══════════════════════════════════════════════════
    private void UpdateTouchDots()
    {
        var touches = detector.GetTouchPoints();

        if (!showTouchPoints)
        {
            foreach (var d in _touchDots) d.gameObject.SetActive(false);
            return;
        }

        // 物件池：不夠時才新增
        while (_touchDots.Count < touches.Count)
            _touchDots.Add(CreateDot($"TouchDot_{_touchDots.Count}", _touchDotContainer, touchColor, touchDotSize));

        for (int i = 0; i < _touchDots.Count; i++)
        {
            if (i < touches.Count)
            {
                _touchDots[i].gameObject.SetActive(true);
                _touchDots[i].anchoredPosition = WallToPanel(touches[i].x, touches[i].y);
            }
            else
            {
                _touchDots[i].gameObject.SetActive(false);
            }
        }
    }

    // ═══════════════════════════════════════════════════
    // 公開 API
    // ═══════════════════════════════════════════════════

    /// <summary>強制重建所有視覺元件（設定大幅更改時使用）</summary>
    public void RebuildAll()
    {
        // 清除所有動態建立的物件
        foreach (var l in _wallBorderLines) if (l) Destroy(l.gameObject);
        foreach (var l in _scanLines)       if (l) Destroy(l.gameObject);
        foreach (var l in _detRectLines)    if (l) Destroy(l.gameObject);
        foreach (var d in _touchDots)       if (d) Destroy(d.gameObject);
        if (_sensorDot)         Destroy(_sensorDot.gameObject);
        if (_scanLineContainer) Destroy(_scanLineContainer.gameObject);
        if (_touchDotContainer) Destroy(_touchDotContainer.gameObject);

        _scanLines.Clear();
        _touchDots.Clear();
        _prevStartStep = -1; // 強制重建掃描線

        Canvas.ForceUpdateCanvases();
        BuildAllElements();
    }

    // ═══════════════════════════════════════════════════
    // ─── 座標轉換 ───
    //
    //  前提：wallPanel 的 Pivot = (0, 0)（左下角）
    //  Wall  (0, 0)               → Panel (0, 0)            = 左下角
    //  Wall  (wallWidth, wallHeight) → Panel (panelW, panelH) = 右上角
    // ═══════════════════════════════════════════════════
    private Vector2 WallToPanel(float wallX, float wallY)
    {
        float pw = wallPanel.rect.width;
        float ph = wallPanel.rect.height;
        return new Vector2(
            wallX / detector.wallWidth  * pw,
            wallY / detector.wallHeight * ph
        );
    }

    // ═══════════════════════════════════════════════════
    // ─── UI 建立輔助函式（不需任何 Prefab）───
    // ═══════════════════════════════════════════════════

    // 建立全滿的 RectTransform 容器
    private RectTransform CreateFullStretchContainer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(wallPanel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // 建立帶顏色的 Image RectTransform（anchor = 左下，pivot = 中心）
    private RectTransform CreateImageRT(string name, RectTransform parent, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.color        = color;
        img.raycastTarget = false; // 避免遮擋 UI 互動
        return rt;
    }

    // 建立線條（薄長矩形，由 SetLineTransform 設定角度與長度）
    private RectTransform CreateLine(string name, RectTransform parent, Color color)
    {
        return CreateImageRT(name, parent, color);
    }

    // 建立圓點（正方形，可搭配圓形 Sprite 使用）
    private RectTransform CreateDot(string name, RectTransform parent, Color color, float size)
    {
        var rt = CreateImageRT(name, parent, color);
        rt.sizeDelta = Vector2.one * size;
        return rt;
    }

    // 設定線條的位置、長度、旋轉角度
    // from / to 均為面板座標（px）
    private void SetLineTransform(RectTransform line, Vector2 from, Vector2 to)
    {
        Vector2 dir    = to - from;
        float   length = dir.magnitude;

        if (length < 0.5f)
        {
            line.sizeDelta = Vector2.zero;
            return;
        }

        // atan2(y, x) - 90° 將「向上」方向對齊 sizeDelta 的 height 軸
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        line.anchoredPosition = from + dir * 0.5f;
        line.sizeDelta        = new Vector2(lineWidth, length);
        line.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }

    // 設定容器及其子物件的 active 狀態
    private void SetContainerActive(RectTransform container, bool active)
    {
        if (container != null) container.gameObject.SetActive(active);
    }
}
