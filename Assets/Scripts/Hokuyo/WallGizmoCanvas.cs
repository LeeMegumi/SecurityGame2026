using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WallGizmoCanvas v2.1
/// </summary>
public class WallGizmoCanvas : MonoBehaviour
{
    [Header("─── 參考元件 ───")]
    public WallTouchDetector detector;
    [Tooltip("代表牆面視覺化區域的 Panel（Pivot 設為 (0,0) 左下角）")]
    public RectTransform wallPanel;

    [Header("─── 顯示開關 ───")]
    public bool showWallBorder    = true;
    public bool showSensor        = true;
    public bool showScanLines     = true;
    public bool showDetectionRect = true;
    public bool showTouchPoints   = true;

    [Header("─── 顯示比例 ───")]
    [Tooltip("整體縮放比例（1.0 = 原始大小，0.5 = 縮小一半），從左下角縮放")]
    [Range(0.05f, 2.0f)]
    public float displayScale = 0.5f;

    [Header("─── 顏色設定 ───")]
    public Color wallBorderColor    = new Color(0.0f, 1.0f, 0.3f, 0.7f);
    public Color sensorColor        = Color.white;
    public Color scanLineColor      = new Color(1.0f, 1.0f, 0.0f, 0.35f);
    public Color detectionRectColor = new Color(1.0f, 0.9f, 0.0f, 0.9f);
    public Color touchColor         = new Color(1.0f, 0.15f, 0.15f, 0.9f);

    [Header("─── 尺寸設定 ───")]
    [Tooltip("線條寬度 (px)，會隨 displayScale 自動縮放")]
    public float lineWidth        = 2f;
    [Tooltip("觸碰點大小 (px)")]
    public float touchDotSize     = 24f;
    [Tooltip("感測器標記大小 (px)")]
    public float sensorDotSize    = 14f;
    [Tooltip("每隔幾個 step 畫一條掃描線")]
    public int   scanLineInterval = 50;

    // ── 固定參考尺寸（對應 Canvas 1920×1080）────────────
    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    // ── 動態建立的 UI 物件 ──────────────────────────────
    private RectTransform[]     _wallBorderLines = new RectTransform[4];
    private RectTransform       _sensorDot;
    private List<RectTransform> _scanLines       = new List<RectTransform>();
    private RectTransform[]     _detRectLines    = new RectTransform[4];
    private List<RectTransform> _touchDots       = new List<RectTransform>();

    private RectTransform _scanLineContainer;
    private RectTransform _touchDotContainer;

    // ── 快取 ─────────────────────────────────────────
    private float _prevSensorX, _prevSensorY;
    private float _prevWallWidth, _prevWallHeight;
    private int   _prevStartStep = -1, _prevEndStep = -1;
    private float _prevDisplayScale = -1f;
    private bool  _initialized  = false;

    // ═══════════════════════════════════════════════════
    void Start()
    {
        if (detector  == null) { Debug.LogError("[GizmoCanvas] 請指定 WallTouchDetector"); return; }
        if (wallPanel == null) { Debug.LogError("[GizmoCanvas] 請指定 wallPanel");         return; }

        Canvas.ForceUpdateCanvases();
        BuildAllElements();
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        UpdateWallBorder();
        UpdateSensorDot();

        // displayScale 改變時強制重建掃描線
        if (ShouldRebuildScanLines()) RebuildScanLines();
        else _scanLineContainer.gameObject.SetActive(showScanLines);

        UpdateDetectionRect();
        UpdateTouchDots();
    }

    // ═══════════════════════════════════════════════════
    // 建立所有 UI 物件
    // ═══════════════════════════════════════════════════
    private void BuildAllElements()
    {
        _scanLineContainer = CreateContainer("ScanLines");
        _touchDotContainer = CreateContainer("TouchDots");

        for (int i = 0; i < 4; i++)
            _wallBorderLines[i] = CreateLine($"WallBorder_{i}", wallPanel, wallBorderColor);

        _sensorDot = CreateDot("SensorDot", wallPanel, sensorColor, sensorDotSize);

        for (int i = 0; i < 4; i++)
            _detRectLines[i] = CreateLine($"DetRect_{i}", wallPanel, detectionRectColor);

        RebuildScanLines();
    }

    // ═══════════════════════════════════════════════════
    // 🟩 綠色框 = 牆面總範圍
    // ═══════════════════════════════════════════════════
    private void UpdateWallBorder()
    {
        foreach (var l in _wallBorderLines) l.gameObject.SetActive(showWallBorder);
        if (!showWallBorder) return;

        Vector2 bl = MmToPanel(0f,                0f                 );
        Vector2 br = MmToPanel(detector.wallWidth, 0f                 );
        Vector2 tl = MmToPanel(0f,                detector.wallHeight );
        Vector2 tr = MmToPanel(detector.wallWidth, detector.wallHeight );

        SetLine(_wallBorderLines[0], bl, br);
        SetLine(_wallBorderLines[1], tl, tr);
        SetLine(_wallBorderLines[2], bl, tl);
        SetLine(_wallBorderLines[3], br, tr);
    }

    // ═══════════════════════════════════════════════════
    // ⚪ 白點 = 感測器位置
    // ═══════════════════════════════════════════════════
    private void UpdateSensorDot()
    {
        _sensorDot.gameObject.SetActive(showSensor);
        if (showSensor)
            _sensorDot.anchoredPosition = MmToPanel(detector.sensorX, detector.sensorY);
    }

    // ═══════════════════════════════════════════════════
    // 🟡 黃色扇線 = 掃描範圍
    // ═══════════════════════════════════════════════════
    private bool ShouldRebuildScanLines()
    {
        return !Mathf.Approximately(_prevSensorX,     detector.sensorX)    ||
               !Mathf.Approximately(_prevSensorY,     detector.sensorY)    ||
               !Mathf.Approximately(_prevWallWidth,   detector.wallWidth)  ||
               !Mathf.Approximately(_prevWallHeight,  detector.wallHeight) ||
               !Mathf.Approximately(_prevDisplayScale, displayScale)       ||
               _prevStartStep != detector.wallStartStep                    ||
               _prevEndStep   != detector.wallEndStep;
    }

    private void RebuildScanLines()
    {
        foreach (var t in _scanLines) if (t != null) Destroy(t.gameObject);
        _scanLines.Clear();

        if (showScanLines)
        {
            Vector2 sensorPanel = MmToPanel(detector.sensorX, detector.sensorY);
            float   scanLenMm   = detector.wallHeight * 1.5f;

            for (int step = detector.wallStartStep; step <= detector.wallEndStep; step += scanLineInterval)
            {
                float   aRad   = (step - 540) * 0.25f * Mathf.Deg2Rad;
                float   endX   = detector.sensorX + Mathf.Sin(aRad) * scanLenMm;
                float   endY   = detector.sensorY - Mathf.Cos(aRad) * scanLenMm;
                Vector2 endPnl = MmToPanel(endX, endY);

                var line = CreateLine($"ScanLine_{step}", _scanLineContainer, scanLineColor);
                SetLine(line, sensorPanel, endPnl);
                _scanLines.Add(line);
            }
        }

        _prevSensorX     = detector.sensorX;
        _prevSensorY     = detector.sensorY;
        _prevWallWidth   = detector.wallWidth;
        _prevWallHeight  = detector.wallHeight;
        _prevDisplayScale = displayScale;
        _prevStartStep   = detector.wallStartStep;
        _prevEndStep     = detector.wallEndStep;
    }

    // ═══════════════════════════════════════════════════
    // 🟨 黃色框 = 矩形偵測區域
    // ═══════════════════════════════════════════════════
    private void UpdateDetectionRect()
    {
        bool active = showDetectionRect && detector.enableDetectionRect;
        foreach (var l in _detRectLines) l.gameObject.SetActive(active);
        if (!active) return;

        Vector2 bl = MmToPanel(detector.rectX,                      detector.rectY                       );
        Vector2 br = MmToPanel(detector.rectX + detector.rectWidth,  detector.rectY                       );
        Vector2 tl = MmToPanel(detector.rectX,                      detector.rectY + detector.rectHeight  );
        Vector2 tr = MmToPanel(detector.rectX + detector.rectWidth,  detector.rectY + detector.rectHeight  );

        SetLine(_detRectLines[0], bl, br);
        SetLine(_detRectLines[1], tl, tr);
        SetLine(_detRectLines[2], bl, tl);
        SetLine(_detRectLines[3], br, tr);
    }

    // ═══════════════════════════════════════════════════
    // 🔴 紅點 = 即時觸碰點
    // ═══════════════════════════════════════════════════
    private void UpdateTouchDots()
    {
        var touches = detector.GetTouchPoints();

        if (!showTouchPoints)
        {
            foreach (var d in _touchDots) d.gameObject.SetActive(false);
            return;
        }

        while (_touchDots.Count < touches.Count)
            _touchDots.Add(CreateDot($"TouchDot_{_touchDots.Count}", _touchDotContainer, touchColor, touchDotSize));

        for (int i = 0; i < _touchDots.Count; i++)
        {
            if (i < touches.Count)
            {
                _touchDots[i].gameObject.SetActive(true);
                _touchDots[i].anchoredPosition = MmToPanel(touches[i].x, touches[i].y);
            }
            else _touchDots[i].gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════════
    // 公開 API
    // ═══════════════════════════════════════════════════
    public void RebuildAll()
    {
        foreach (var l in _wallBorderLines) if (l) Destroy(l.gameObject);
        foreach (var l in _scanLines)       if (l) Destroy(l.gameObject);
        foreach (var l in _detRectLines)    if (l) Destroy(l.gameObject);
        foreach (var d in _touchDots)       if (d) Destroy(d.gameObject);
        if (_sensorDot)         Destroy(_sensorDot.gameObject);
        if (_scanLineContainer) Destroy(_scanLineContainer.gameObject);
        if (_touchDotContainer) Destroy(_touchDotContainer.gameObject);

        _scanLines.Clear();
        _touchDots.Clear();
        _prevStartStep = -1;

        Canvas.ForceUpdateCanvases();
        BuildAllElements();
    }

    // ═══════════════════════════════════════════════════
    // ─── 核心座標轉換：mm → Canvas px ───
    //
    //  固定參考：REF_W=1920, REF_H=1080
    //  displayScale：整體縮放，從左下角 (0,0) 向內縮放
    //
    //  MmToPanel(x, y) = (x/1920 × panelW × scale,
    //                     y/1080 × panelH × scale)
    //
    //  wallPanel Pivot 必須設為 (0,0)（左下角）
    // ═══════════════════════════════════════════════════
    private Vector2 MmToPanel(float mmX, float mmY)
    {
        float pw = wallPanel.rect.width;
        float ph = wallPanel.rect.height;
        return new Vector2(
            mmX / REF_W * pw * displayScale,
            mmY / REF_H * ph * displayScale
        );
    }

    // ═══════════════════════════════════════════════════
    // UI 建立輔助函式
    // ═══════════════════════════════════════════════════
    private RectTransform CreateContainer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(wallPanel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private RectTransform CreateImageRT(string name, RectTransform parent, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return rt;
    }

    private RectTransform CreateLine(string name, RectTransform parent, Color color)
        => CreateImageRT(name, parent, color);

    private RectTransform CreateDot(string name, RectTransform parent, Color color, float size)
    {
        var rt = CreateImageRT(name, parent, color);
        rt.sizeDelta = Vector2.one * size;
        return rt;
    }

    private void SetLine(RectTransform line, Vector2 from, Vector2 to)
    {
        Vector2 dir    = to - from;
        float   length = dir.magnitude;
        if (length < 0.5f) { line.sizeDelta = Vector2.zero; return; }

        // 線條寬度也隨 scale 縮放，保持視覺比例一致
        float  scaledWidth = Mathf.Max(0.5f, lineWidth * displayScale);
        float  angle       = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        line.anchoredPosition = from + dir * 0.5f;
        line.sizeDelta        = new Vector2(scaledWidth, length);
        line.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }
}
