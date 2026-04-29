using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 從圓心向外擴張的氣波衝擊系統
/// 掛在和 FloatingDebrisSpawner 相同位置的 GameObject 上（或同一中心點的空物件）
/// </summary>
public class ShockwaveEmitter : MonoBehaviour
{
    [Header("── 衝擊波設定 ──")]
    [SerializeField] private float waveSpeed = 25f;   // 波面擴張速度（單位/秒）
    [SerializeField] private float waveThickness = 10f;   // 波面環帶寬度（越寬影響時間越長）
    [SerializeField] private float startRadius = 0f;    // 起始半徑（建議設為 innerRadius）
    [SerializeField] private float maxRadius = 160f;  // 超過此半徑後波消失

    [Header("── 衝擊力道 ──")]
    [SerializeField] private float baseForce = 60f;   // 基礎衝力大小（Impulse）
    [SerializeField] private float minForceFactor = 0.4f;  // 波到邊緣時的力道衰減比例（0~1）
    [SerializeField] private float spinForce = 0.25f; // 擊中時附帶翻滾力道的比例

    [Header("── 自動觸發 ──")]
    [SerializeField] private bool autoFire = false;
    [SerializeField] private float autoInterval = 6f;     // 自動發射間隔（秒）

    [Header("── 視覺球（選填）──")]
    [SerializeField] private GameObject waveSphere; // 指定一個半透明球體作為波面視覺

    // ── 內部狀態 ──
    private bool _active;
    private float _currentRadius;
    private readonly HashSet<Rigidbody> _hitSet = new(); // 記錄此波已擊中的物件（避免重複施力）

    //─────────────────────────────────────────────────────────
    // 生命週期
    //─────────────────────────────────────────────────────────
    private void Start()
    {
        if (waveSphere != null) waveSphere.SetActive(false);
        if (autoFire) StartCoroutine(AutoFireRoutine());
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            Fire();
        }
        if (!_active) return;

        _currentRadius += waveSpeed * Time.deltaTime;

        DetectAndPush();
        UpdateVisual();

        if (_currentRadius >= maxRadius)
            EndWave();
    }

    //─────────────────────────────────────────────────────────
    // 核心：偵測波面環帶內的物件並施力
    //─────────────────────────────────────────────────────────
    private void DetectAndPush()
    {
        float outerEdge = _currentRadius + waveThickness * 0.5f;
        float innerEdge = Mathf.Max(0f, _currentRadius - waveThickness * 0.5f);

        // 取出波面外圈半徑內所有 Collider
        Collider[] hits = Physics.OverlapSphere(transform.position, outerEdge);

        foreach (Collider col in hits)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null || _hitSet.Contains(rb)) continue;

            float dist = Vector3.Distance(transform.position, rb.position);
            if (dist < innerEdge) continue; // 尚未進入波面環帶

            // ── 計算向外推力方向 ──
            Vector3 dir = rb.position - transform.position;
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Random.onUnitSphere;

            // ── 力道隨距離線性衰減 ──
            float t = Mathf.Clamp01(dist / maxRadius);
            float forceMag = baseForce * Mathf.Lerp(1f, minForceFactor, t);

            // 向外衝力
            rb.AddForce(dir * forceMag, ForceMode.Impulse);

            // 附帶隨機翻滾（讓物件被震到後旋轉更混亂）
            if (spinForce > 0f)
                rb.AddTorque(Random.insideUnitSphere * forceMag * spinForce, ForceMode.Impulse);

            _hitSet.Add(rb); // 標記此物件已被此波影響
        }
    }

    //─────────────────────────────────────────────────────────
    // 視覺球縮放更新
    //─────────────────────────────────────────────────────────
    private void UpdateVisual()
    {
        if (waveSphere == null) return;
        waveSphere.transform.localScale = Vector3.one * (_currentRadius * 2f);
    }

    private void EndWave()
    {
        _active = false;
        _hitSet.Clear();
        if (waveSphere != null) waveSphere.SetActive(false);
    }

    //─────────────────────────────────────────────────────────
    // 公開方法
    //─────────────────────────────────────────────────────────

    /// <summary>手動觸發衝擊波（可由其他腳本、按鈕或 Animation Event 呼叫）</summary>
    public void Fire()
    {
        _currentRadius = startRadius;
        _active = true;
        _hitSet.Clear();

        if (waveSphere != null)
        {
            waveSphere.SetActive(true);
            waveSphere.transform.localScale = Vector3.one * (startRadius * 2f);
        }
    }

    /// <summary>定時自動觸發</summary>
    private IEnumerator AutoFireRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoInterval + Random.Range(-autoInterval * 0.2f, autoInterval * 0.2f));
            Fire();
        }
    }

    //─────────────────────────────────────────────────────────
    // Scene 視圖 Gizmo（播放時可看到波面擴張）
    //─────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_active) return;

        // 波面外環
        Gizmos.color = new Color(0.4f, 1f, 0.9f, 0.08f);
        Gizmos.DrawSphere(transform.position, _currentRadius + waveThickness * 0.5f);

        // 波面線框
        Gizmos.color = new Color(0.4f, 1f, 0.9f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, _currentRadius);
        Gizmos.DrawWireSphere(transform.position, _currentRadius + waveThickness * 0.5f);
        if (_currentRadius > waveThickness * 0.5f)
            Gizmos.DrawWireSphere(transform.position, _currentRadius - waveThickness * 0.5f);
    }
}