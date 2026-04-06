using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 牆面觸控偵測器 v3 — 適用「感測器垂直安裝於牆面上方、掃描整面牆」
///
/// ━━━━ 安裝方式與幾何說明 ━━━━
///
///  感測器旋轉 90°，掃描面改為「垂直面」，從牆面頂端朝下掃描整面牆：
///
///  ┌──────────────────────────────────┐ ← 牆面頂端
///  │           ● Sensor               │   (感測器在此，scan 面垂直朝下)
///  │          /|\                     │
///  │         / | \   step < 540 = 左  │
///  │        /  |  \  step > 540 = 右  │
///  │       /   ↓   \  step 540 = 正下 │
///  │      ●    ●    ●  ← 各 step 打到 │
///  │          ...       的牆面點       │
///  └──────────────────────────────────┘ ← 牆面底端
///
///  ★ X 由「step 角度」決定（左右位置）
///  ★ Y 由「距離量測值」決定（上下位置）
///    → 距離小 = 觸碰位置靠近頂端（Y 大）
///    → 距離大 = 觸碰位置靠近底端（Y 小）
///
///  座標公式：
///    angle = (step - 540) × 0.25°
///    X = sensorX + distance × sin(angle)
///    Y = sensorY - distance × cos(angle)
///
/// ━━━━ 快捷鍵 ━━━━
///  C : 執行校正（牆面無人時）
///  S : 掃描 step 距離（協助確認有效範圍）
/// </summary>
public class WallTouchDetector : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public HokuyoManager hokuyo;

    // ──────────────────────────────────────────────────
    [Header("─── 感測器位置（牆面座標 mm，原點左下角）───")]
    [Tooltip("感測器 X 位置：通常裝在牆面水平中央 = wallWidth / 2")]
    public float sensorX = 960f;
    [Tooltip("感測器 Y 位置：通常裝在牆面頂端 = wallHeight，若感測器突出 50mm 可填 wallHeight + 50")]
    public float sensorY = 1080f;

    [Header("─── 掃描有效 step 範圍 ───")]
    [Tooltip("對應牆面左緣的 step（按 S 鍵查看後填入）")]
    public int wallStartStep = 200;
    [Tooltip("對應牆面右緣的 step")]
    public int wallEndStep   = 880;

    // ──────────────────────────────────────────────────
    [Header("─── 牆面總尺寸 (mm) ───")]
    public float wallWidth  = 1920f;
    public float wallHeight = 1080f;

    // ──────────────────────────────────────────────────
    [Header("─── 矩形偵測區域（mm，原點牆面左下角）───")]
    [Tooltip("勾選後只有矩形內的觸碰才會觸發事件")]
    public bool  enableDetectionRect = false;
    public float rectX      = 0f;
    public float rectY      = 0f;
    public float rectWidth  = 1920f;
    public float rectHeight = 1080f;

    // ──────────────────────────────────────────────────
    [Header("─── 觸碰判定 ───")]
    [Tooltip("手比基準距離近多少 mm 才視為觸碰（越小越靈敏）")]
    public float touchThreshold = 30f;
    [Tooltip("合併鄰近點的半徑 (mm)，防止同一手指產生多個點")]
    public float clusterRadius  = 80f;

    // ── 校正基準線 ─────────────────────────────────────
    private long[] _baseline;
    public  bool   IsCalibrated { get; private set; } = false;

    // ── Unity Events ───────────────────────────────────
    [Header("─── Unity Events ───")]
    /// <summary>新觸碰：List(Vector2) 單位 mm，原點牆面左下角</summary>
    public UnityEvent<List<Vector2>> onTouchDown;
    /// <summary>觸碰持續更新</summary>
    public UnityEvent<List<Vector2>> onTouchMove;
    /// <summary>所有觸碰離開</summary>
    public UnityEvent onTouchUp;

    private List<Vector2> _lastTouchPoints = new List<Vector2>();

    // ── UST-10LX 規格常數 ──────────────────────────────
    private const float ANGLE_PER_STEP = 0.25f;   // degree / step
    private const int   CENTER_STEP    = 540;      // step 540 = 正前方（正下方）

    // 方便外部讀取偵測矩形
    public Rect DetectionRect => new Rect(rectX, rectY, rectWidth, rectHeight);

    // ─────────────────────────────────────────────────
    void Start()
    {
        // 預設 sensorX / sensorY 對應牆面中央頂端
        if (sensorX == 0) sensorX = wallWidth  * 0.5f;
        if (sensorY == 0) sensorY = wallHeight;
    }

    void Update()
    {
        if (!hokuyo.IsConnected) return;
        var distances = hokuyo.GetDistances();
        if (distances.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.C)) Calibrate(distances);
        if (Input.GetKeyDown(KeyCode.S)) ScanWallSteps(distances);
        if (!IsCalibrated) return;

        DetectTouch(distances);
    }

    // ─────────────────────────────────────────────────
    // 校正
    // ─────────────────────────────────────────────────
    [ContextMenu("Calibrate (牆面無人時執行)")]
    public void Calibrate() => Calibrate(hokuyo.GetDistances());

    private void Calibrate(List<long> distances)
    {
        if (distances.Count == 0)
        {
            Debug.LogWarning("[WallTouch] 尚未收到數據，請確認連線");
            return;
        }
        int count = wallEndStep - wallStartStep + 1;
        _baseline = new long[count];

        for (int i = 0; i < count; i++)
        {
            int  step = wallStartStep + i;
            long d    = (step < distances.Count && distances[step] > 0)
                ? distances[step]
                : 9999L;
            _baseline[i] = d;
        }

        IsCalibrated = true;
        Debug.Log($"[WallTouch] ✅ 校正完成  step {wallStartStep}~{wallEndStep}，共 {count} 點");
        LogBaseline();
    }

    // ─────────────────────────────────────────────────
    // 觸碰偵測
    // ─────────────────────────────────────────────────
    private void DetectTouch(List<long> distances)
    {
        var rawPoints = new List<Vector2>();
        int count = wallEndStep - wallStartStep + 1;

        for (int i = 0; i < count; i++)
        {
            int  step = wallStartStep + i;
            if (step >= distances.Count) continue;
            long dist = distances[step];
            if (dist <= 0) continue;

            // 觸碰判定：距離明顯小於基準線
            if (dist < _baseline[i] - (long)touchThreshold)
            {
                Vector2 wallPos = StepDistToWallPos(step, dist);

                // 基本牆面邊界過濾
                if (wallPos.x < 0 || wallPos.x > wallWidth) continue;
                if (wallPos.y < 0 || wallPos.y > wallHeight) continue;

                // 矩形區域過濾
                if (enableDetectionRect && !IsInsideRect(wallPos)) continue;

                rawPoints.Add(wallPos);
            }
        }

        var clustered = ClusterPoints(rawPoints, clusterRadius);

        if (clustered.Count > 0)
        {
            if (_lastTouchPoints.Count == 0) onTouchDown?.Invoke(clustered);
            onTouchMove?.Invoke(clustered);
        }
        else if (_lastTouchPoints.Count > 0)
        {
            onTouchUp?.Invoke();
        }
        _lastTouchPoints = clustered;
    }

    // ─────────────────────────────────────────────────
    // ★★★ 核心幾何公式（垂直安裝，掃描整面牆）★★★
    //
    //  感測器在 (sensorX, sensorY)
    //  step 540 = 正下方
    //  angle = (step - 540) × 0.25°（負 = 左，正 = 右）
    //
    //  X = sensorX + distance × sin(angle)   ← 角度決定左右
    //  Y = sensorY - distance × cos(angle)   ← 距離決定上下
    // ─────────────────────────────────────────────────
    private Vector2 StepDistToWallPos(int step, long distMm)
    {
        float angleRad = (step - CENTER_STEP) * ANGLE_PER_STEP * Mathf.Deg2Rad;
        float d        = (float)distMm;

        float x = sensorX + d * Mathf.Sin(angleRad);
        float y = sensorY - d * Mathf.Cos(angleRad);

        return new Vector2(x, y);
    }

    // ─────────────────────────────────────────────────
    // 矩形過濾
    // ─────────────────────────────────────────────────
    private bool IsInsideRect(Vector2 p)
    {
        return p.x >= rectX && p.x <= rectX + rectWidth
            && p.y >= rectY && p.y <= rectY + rectHeight;
    }

    // ─────────────────────────────────────────────────
    // 矩形動態修改 API
    // ─────────────────────────────────────────────────
    public void SetDetectionRect(float x, float y, float width, float height)
    {
        rectX = x; rectY = y; rectWidth = width; rectHeight = height;
        enableDetectionRect = true;
        Debug.Log($"[WallTouch] 偵測矩形→ ({x},{y}) {width}×{height} mm");
    }
    public void DisableDetectionRect() => enableDetectionRect = false;
    public void EnableDetectionRect()  => enableDetectionRect = true;

    // ─────────────────────────────────────────────────
    // 合併鄰近點
    // ─────────────────────────────────────────────────
    private List<Vector2> ClusterPoints(List<Vector2> points, float radius)
    {
        var result = new List<Vector2>();
        var used   = new bool[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            if (used[i]) continue;
            var cluster = new List<Vector2> { points[i] };
            used[i] = true;
            for (int j = i + 1; j < points.Count; j++)
                if (!used[j] && Vector2.Distance(points[i], points[j]) < radius)
                { cluster.Add(points[j]); used[j] = true; }
            Vector2 c = Vector2.zero;
            foreach (var p in cluster) c += p;
            result.Add(c / cluster.Count);
        }
        return result;
    }

    // ─────────────────────────────────────────────────
    // 公開 API
    // ─────────────────────────────────────────────────
    public List<Vector2> GetTouchPoints() => new List<Vector2>(_lastTouchPoints);

    /// <summary>
    /// 正規化觸碰點（0~1），相對於偵測矩形（啟用時）或牆面總範圍
    /// </summary>
    public List<Vector2> GetNormalizedTouchPoints()
    {
        float baseX = enableDetectionRect ? rectX      : 0f;
        float baseY = enableDetectionRect ? rectY      : 0f;
        float rangeW = enableDetectionRect ? rectWidth  : wallWidth;
        float rangeH = enableDetectionRect ? rectHeight : wallHeight;
        var norm = new List<Vector2>();
        foreach (var p in _lastTouchPoints)
            norm.Add(new Vector2((p.x - baseX) / rangeW, (p.y - baseY) / rangeH));
        return norm;
    }

    // ─────────────────────────────────────────────────
    // 掃描工具（按 S 鍵）
    // ─────────────────────────────────────────────────
    private void ScanWallSteps(List<long> distances)
    {
        Debug.Log("═══ Step 距離掃描 ═══（找到牆面有效範圍後填入 wallStartStep / wallEndStep）");
        for (int i = 0; i <= 1080; i += 10)
        {
            if (i >= distances.Count) break;
            long d = distances[i];
            Vector2 pos = StepDistToWallPos(i, d);
            Debug.Log($"  Step {i,4}  dist={d,6}mm  →  牆面座標 ({pos.x:F0}, {pos.y:F0}) mm");
        }
    }

    private void LogBaseline()
    {
        Debug.Log("─── 校正基準線（部分抽樣）───");
        int step_count = wallEndStep - wallStartStep + 1;
        for (int i = 0; i < step_count; i += step_count / 5)
        {
            int step = wallStartStep + i;
            Vector2 pos = StepDistToWallPos(step, _baseline[i]);
            Debug.Log($"  Step {step,4}  baseline={_baseline[i],6}mm  →  牆面座標 ({pos.x:F0}, {pos.y:F0}) mm");
        }
    }

    // ─────────────────────────────────────────────────
    // Gizmos：Scene 視窗可視化
    // ─────────────────────────────────────────────────
   /* void OnDrawGizmos()
    {
        float s = 0.001f; // mm → m（Unity 單位）

        // 牆面總範圍（綠色半透明）
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawCube(
            new Vector3(wallWidth * 0.5f * s, wallHeight * 0.5f * s, -0.002f),
            new Vector3(wallWidth * s, wallHeight * s, 0.001f));
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(
            new Vector3(wallWidth * 0.5f * s, wallHeight * 0.5f * s, 0),
            new Vector3(wallWidth * s, wallHeight * s, 0.001f));

        // 感測器位置（白色球）
        Vector3 sensorPos = new Vector3(sensorX * s, sensorY * s, 0.01f);
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(sensorPos, 0.03f);

        // 掃描扇形示意（黃色線條，每 50 step 一條）
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        for (int step = wallStartStep; step <= wallEndStep; step += 50)
        {
            float aRad = (step - CENTER_STEP) * ANGLE_PER_STEP * Mathf.Deg2Rad;
            float scanLen = wallHeight * 1.5f * s;
            Vector3 dir = new Vector3(Mathf.Sin(aRad), -Mathf.Cos(aRad), 0) * scanLen;
            Gizmos.DrawLine(sensorPos, sensorPos + dir);
        }

        // 偵測矩形（黃色框）
        if (enableDetectionRect)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3((rectX + rectWidth * 0.5f) * s, (rectY + rectHeight * 0.5f) * s, 0.002f),
                new Vector3(rectWidth * s, rectHeight * s, 0.001f));
        }

        // 觸碰點（紅色球）
        Gizmos.color = Color.red;
        foreach (var p in _lastTouchPoints)
            Gizmos.DrawSphere(new Vector3(p.x * s, p.y * s, 0.01f), 0.04f);
    }*/
}
