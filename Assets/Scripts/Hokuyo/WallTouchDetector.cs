using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WallTouchDetector : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public HokuyoManager hokuyo;

    [Header("─── 感測器位置（牆面座標 mm，原點左下角）───")]
    public float sensorX = 960f;
    public float sensorY = 1080f;

    [Header("─── 掃描有效 step 範圍 ───")]
    public int wallStartStep = 200;
    public int wallEndStep = 880;

    [Header("─── 牆面總尺寸 (mm) ───")]
    public float wallWidth = 1920f;
    public float wallHeight = 1080f;

    [Header("─── 矩形偵測區域（mm，原點牆面左下角）───")]
    public bool enableDetectionRect = false;
    public float rectX = 0f;
    public float rectY = 0f;
    public float rectWidth = 1920f;
    public float rectHeight = 1080f;

    [Header("─── 觸碰判定 ───")]
    [Tooltip("單一 step 相對基準距離至少要近多少 mm，才視為有效候選點")]
    public float touchThreshold = 30f;
    [Tooltip("合併鄰近點的半徑 (mm)，防止同一手指產生多個點")]
    public float clusterRadius = 80f;
    [Tooltip("最少需要連續觸發幾個 step 才算有效")]
    public int minTriggeredSteps = 3;
    [Tooltip("允許相鄰 step 間最多間隔幾個 step，仍視為同一群")]
    public int maxGapSteps = 1;

    private long[] _baseline;
    public bool IsCalibrated { get; private set; } = false;

    [Header("─── Unity Events ───")]
    public UnityEvent<List<Vector2>> onTouchDown;
    public UnityEvent<List<Vector2>> onTouchMove;
    public UnityEvent onTouchUp;

    private List<Vector2> _lastTouchPoints = new List<Vector2>();

    private const float ANGLE_PER_STEP = 0.25f;
    private const int CENTER_STEP = 540;

    public Rect DetectionRect => new Rect(rectX, rectY, rectWidth, rectHeight);

    void Start()
    {
        if (sensorX == 0) sensorX = wallWidth * 0.5f;
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

    [ContextMenu("Calibrate (牆面無人時執行)")]
    public void Calibrate() => Calibrate(hokuyo.GetDistances());

    private void Calibrate(List<long> distances)
    {
        if (distances.Count == 0) return;
        int count = wallEndStep - wallStartStep + 1;
        _baseline = new long[count];
        for (int i = 0; i < count; i++)
        {
            int step = wallStartStep + i;
            long d = (step < distances.Count && distances[step] > 0) ? distances[step] : 9999L;
            _baseline[i] = d;
        }
        IsCalibrated = true;
        Debug.Log($"[WallTouch] ✅ 校正完成 step {wallStartStep}~{wallEndStep}，共 {count} 點");
    }

    private void DetectTouch(List<long> distances)
    {
        var candidatePoints = new List<Vector2>();
        int count = wallEndStep - wallStartStep + 1;
        for (int i = 0; i < count; i++)
        {
            int step = wallStartStep + i;
            if (step >= distances.Count) continue;
            long dist = distances[step];
            if (dist <= 0) continue;
            if (dist < _baseline[i] - (long)touchThreshold)
            {
                Vector2 wallPos = StepDistToWallPos(step, dist);
                if (wallPos.x < 0 || wallPos.x > wallWidth) continue;
                if (wallPos.y < 0 || wallPos.y > wallHeight) continue;
                if (enableDetectionRect && !IsInsideRect(wallPos)) continue;
                candidatePoints.Add(wallPos);
            }
        }

        var grouped = ClusterConsecutiveSteps(candidatePoints, distances);
        var clustered = ClusterPoints(grouped, clusterRadius);

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

    private List<Vector2> ClusterConsecutiveSteps(List<Vector2> rawPoints, List<long> distances)
    {
        if (rawPoints.Count == 0) return rawPoints;
        var result = new List<Vector2>();
        var sorted = new List<(int step, Vector2 pos)>();
        int count = wallEndStep - wallStartStep + 1;
        for (int i = 0; i < count; i++)
        {
            int step = wallStartStep + i;
            if (step >= distances.Count) continue;
            long dist = distances[step];
            if (dist <= 0) continue;
            if (dist < _baseline[i] - (long)touchThreshold)
            {
                Vector2 wallPos = StepDistToWallPos(step, dist);
                if (wallPos.x < 0 || wallPos.x > wallWidth) continue;
                if (wallPos.y < 0 || wallPos.y > wallHeight) continue;
                if (enableDetectionRect && !IsInsideRect(wallPos)) continue;
                sorted.Add((step, wallPos));
            }
        }

        int idx = 0;
        while (idx < sorted.Count)
        {
            var cluster = new List<Vector2> { sorted[idx].pos };
            int startStep = sorted[idx].step;
            int lastStep = startStep;
            idx++;

            while (idx < sorted.Count && sorted[idx].step - lastStep <= maxGapSteps + 1)
            {
                cluster.Add(sorted[idx].pos);
                lastStep = sorted[idx].step;
                idx++;
            }

            int triggeredSteps = lastStep - startStep + 1;
            if (triggeredSteps >= minTriggeredSteps)
            {
                Vector2 c = Vector2.zero;
                foreach (var p in cluster) c += p;
                result.Add(c / cluster.Count);
            }
        }
        return result;
    }

    private Vector2 StepDistToWallPos(int step, long distMm)
    {
        float angleRad = (step - CENTER_STEP) * ANGLE_PER_STEP * Mathf.Deg2Rad;
        float d = (float)distMm;
        float x = sensorX + d * Mathf.Sin(angleRad);
        float y = sensorY - d * Mathf.Cos(angleRad);
        return new Vector2(x, y);
    }

    private bool IsInsideRect(Vector2 p)
    {
        return p.x >= rectX && p.x <= rectX + rectWidth && p.y >= rectY && p.y <= rectY + rectHeight;
    }

    public void SetDetectionRect(float x, float y, float width, float height)
    {
        rectX = x; rectY = y; rectWidth = width; rectHeight = height;
        enableDetectionRect = true;
    }

    public void DisableDetectionRect() => enableDetectionRect = false;
    public void EnableDetectionRect() => enableDetectionRect = true;

    private List<Vector2> ClusterPoints(List<Vector2> points, float radius)
    {
        var result = new List<Vector2>();
        var used = new bool[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            if (used[i]) continue;
            var cluster = new List<Vector2> { points[i] };
            used[i] = true;
            for (int j = i + 1; j < points.Count; j++)
            {
                if (!used[j] && Vector2.Distance(points[i], points[j]) < radius)
                {
                    cluster.Add(points[j]);
                    used[j] = true;
                }
            }
            Vector2 c = Vector2.zero;
            foreach (var p in cluster) c += p;
            result.Add(c / cluster.Count);
        }
        return result;
    }

    public List<Vector2> GetTouchPoints() => new List<Vector2>(_lastTouchPoints);

    private void ScanWallSteps(List<long> distances)
    {
        for (int i = 0; i <= 1080; i += 10)
        {
            if (i >= distances.Count) break;
            long d = distances[i];
            Vector2 pos = StepDistToWallPos(i, d);
            Debug.Log($"Step {i} dist={d} → ({pos.x:F0}, {pos.y:F0})");
        }
    }
}