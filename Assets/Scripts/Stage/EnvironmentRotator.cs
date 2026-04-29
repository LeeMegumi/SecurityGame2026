using UnityEngine;

/// <summary>
/// 環境旋轉器 — 掛在戰鬥平台或場景視覺父物件上
/// 只旋轉純視覺物件，不要掛在含有 Rigidbody 子物件的節點上
/// </summary>
public class EnvironmentRotator : MonoBehaviour
{
    [Header("── 旋轉設定 ──")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 旋轉軸（預設 Y 軸）
    [SerializeField] private float rotationSpeed = 5f;         // 度 / 秒（建議 3~8）
    [SerializeField] private bool clockwise = false;     // 是否順時針

    [Header("── 速度呼吸感（選填）──")]
    [SerializeField] private bool breathingEffect = true;
    [SerializeField] private float breathingAmplitude = 1.5f;   // 速度波動幅度（度/秒）
    [SerializeField] private float breathingFrequency = 0.2f;   // 波動頻率（越小越緩慢）

    [Header("── 震動觸發（選填）──")]
    [SerializeField] private float shockwaveSpeedBoost = 15f;   // 衝擊波發出時的瞬間加速
    [SerializeField] private float boostDuration = 0.8f;  // 加速持續秒數

    private float _baseSpeed;
    private float _currentBoost;
    private float _boostTimer;

    private void Start()
    {
        _baseSpeed = rotationSpeed;
    }

    private void Update()
    {
        // 呼吸感速度
        float speed = _baseSpeed;
        if (breathingEffect)
            speed += Mathf.Sin(Time.time * breathingFrequency * Mathf.PI * 2f) * breathingAmplitude;

        // 震動加速（衰減）
        if (_boostTimer > 0f)
        {
            _boostTimer -= Time.deltaTime;
            float boostRatio = Mathf.Clamp01(_boostTimer / boostDuration);
            speed += _currentBoost * boostRatio;
        }

        float direction = clockwise ? -1f : 1f;
        transform.Rotate(rotationAxis.normalized * speed * direction * Time.deltaTime,
                         Space.Self);
    }

    /// <summary>
    /// 被衝擊波觸發時呼叫，使環境短暫加速旋轉
    /// 可在 ShockwaveEmitter.Fire() 裡呼叫此方法
    /// </summary>
    public void TriggerBoost()
    {
        _currentBoost = shockwaveSpeedBoost;
        _boostTimer = boostDuration;
    }
}