using UnityEngine;

/// <summary>
/// 漂浮廢墟行為控制器（Unity 6 · 球殼邊界 + 緩慢自轉版）
/// 由 FloatingDebrisSpawner 自動新增，不需手動掛載
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FloatingDebris : MonoBehaviour
{
    private Rigidbody _rb;

    // ── 漂浮邊界參數 ──
    private Vector3 _zoneCenter;
    private float _innerRadius;
    private float _outerRadius;
    private float _boundaryForce;
    private float _maxSpeed;
    private float _driftForce;
    private float _driftInterval;
    private float _nextDriftTime;

    // ── 自轉參數 ──
    private float _minAngularSpeed;       // rad/s
    private float _maxAngularSpeed;       // rad/s
    private float _rotChangeInterval;     // 換方向間隔（秒）
    private float _rotCorrection;         // 追蹤目標角速度的扭力強度
    private Vector3 _targetAngularVel;      // 目前目標角速度
    private float _nextRotChangeTime;

    private bool _ready;

    // 邊界觸發比例常數
    private const float OuterTriggerRatio = 0.82f;
    private const float InnerTriggerRatio = 1.22f;

    //─────────────────────────────────────────────────────────
    // 初始化（由 Spawner 呼叫）
    //─────────────────────────────────────────────────────────

    /// <summary>設定漂浮邊界參數</summary>
    public void Setup(Vector3 center, float inner, float outer,
                      float bForce, float mSpeed, float dForce, float dInterval)
    {
        _zoneCenter = center;
        _innerRadius = inner;
        _outerRadius = outer;
        _boundaryForce = bForce;
        _maxSpeed = mSpeed;
        _driftForce = dForce;
        _driftInterval = dInterval;
        _ready = true;
    }

    /// <summary>設定自轉參數（由 Spawner 在 Setup 之後呼叫）</summary>
    public void SetupRotation(float minDegPerSec, float maxDegPerSec,
                              float changeInterval, float correctionStrength)
    {
        _minAngularSpeed = minDegPerSec * Mathf.Deg2Rad;
        _maxAngularSpeed = maxDegPerSec * Mathf.Deg2Rad;
        _rotChangeInterval = changeInterval;
        _rotCorrection = correctionStrength;
    }

    //─────────────────────────────────────────────────────────
    // 從 Pool 取出時重置（OnEnable 每次取出都會執行）
    //─────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody>();

        // 隨機錯開漂移時間，避免全體同步
        _nextDriftTime = Time.time + Random.Range(0f, _driftInterval);

        // 設定初始目標自轉（隨機方向 + 隨機速度）
        _targetAngularVel = Random.onUnitSphere * Random.Range(_minAngularSpeed, _maxAngularSpeed);
        _nextRotChangeTime = Time.time + Random.Range(0f, _rotChangeInterval);

        // 直接給一個接近目標的初始角速度（讓物體一出現就在轉）
        if (_rb != null)
            _rb.angularVelocity = _targetAngularVel * 0.8f;
    }

    //─────────────────────────────────────────────────────────
    // 物理更新
    //─────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!_ready || _rb == null) return;

        ApplyBoundaryForce();
        ApplyRandomDrift();
        ApplySelfRotation();   // ← 新增
        ClampSpeed();
    }

    //─────────────────────────────────────────────────────────
    // 球形邊界力
    //─────────────────────────────────────────────────────────
    private void ApplyBoundaryForce()
    {
        Vector3 fromCenter = transform.position - _zoneCenter;
        float dist = fromCenter.magnitude;

        if (dist < 0.01f)
        {
            _rb.AddForce(Random.onUnitSphere * _boundaryForce, ForceMode.Force);
            return;
        }

        Vector3 dirOut = fromCenter / dist;

        float outerThresh = _outerRadius * OuterTriggerRatio;
        float innerThresh = _innerRadius * InnerTriggerRatio;

        if (dist > outerThresh)
        {
            float over = dist - outerThresh;
            _rb.AddForce(-dirOut * _boundaryForce * (1f + over * 0.6f), ForceMode.Force);
        }
        else if (dist < innerThresh)
        {
            float over = innerThresh - dist;
            _rb.AddForce(dirOut * _boundaryForce * (1f + over * 0.6f), ForceMode.Force);
        }
    }

    //─────────────────────────────────────────────────────────
    // 隨機漂移衝力
    //─────────────────────────────────────────────────────────
    private void ApplyRandomDrift()
    {
        if (Time.time < _nextDriftTime) return;

        _rb.AddForce(Random.insideUnitSphere * _driftForce, ForceMode.Impulse);

        float variance = _driftInterval * 0.35f;
        _nextDriftTime = Time.time + _driftInterval + Random.Range(-variance, variance);
    }

    //─────────────────────────────────────────────────────────
    // 緩慢自轉（核心新增邏輯）
    //─────────────────────────────────────────────────────────
    private void ApplySelfRotation()
    {
        // 定時漸進切換目標旋轉軸與速度
        if (Time.time >= _nextRotChangeTime)
        {
            float speed = Random.Range(_minAngularSpeed, _maxAngularSpeed);

            // Slerp 混合：保留部分舊方向，避免旋轉方向瞬間大幅跳變
            Vector3 newDir = Random.onUnitSphere;
            _targetAngularVel = Vector3.Slerp(
                _targetAngularVel.normalized,
                newDir,
                Random.Range(0.3f, 0.7f)   // 每次切換時方向改變量
            ) * speed;

            float variance = _rotChangeInterval * 0.3f;
            _nextRotChangeTime = Time.time + _rotChangeInterval
                                 + Random.Range(-variance, variance);
        }

        // 施加扭力，讓實際角速度平滑追蹤目標（PD 控制器概念）
        Vector3 angularError = _targetAngularVel - _rb.angularVelocity;
        _rb.AddTorque(angularError * _rotCorrection, ForceMode.Force);
    }

    //─────────────────────────────────────────────────────────
    // 線速度上限
    //─────────────────────────────────────────────────────────
    private void ClampSpeed()
    {
        if (_rb.linearVelocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * _maxSpeed;
    }
}