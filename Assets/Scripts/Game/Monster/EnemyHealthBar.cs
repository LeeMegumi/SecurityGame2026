using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("敵人設定")]
    public EnemyBase enemyBase; // 參照 EnemyBase 以讀取最大血量

    [Header("血條設定")]
    [SerializeField] private Transform fill_Sprite;        // 血條 Fill Image
    [SerializeField] private float smoothSpeed = 5f; // Lerp 平滑速度

    public int maxHealth;  // 最大血量（從 EnemyBase 讀取）

    private float _targetFillAmount;   // 目標 Fill Amount（即時更新）

    // ── 公開屬性 ──────────────────────────────────────────
    /// <summary>true = 存活，false = 死亡</summary>
    public bool IsAlive => enemyBase._currentHealth > 0f;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (fill_Sprite != null)
            fill_Sprite.localScale = Vector3.one;
    }

    private void Start()
    {
        
    }
    private int currentHealth() => enemyBase._currentHealth;
    private void Update()
    {
        // 平滑地將 Fill Amount 插值到目標值
        if (fill_Sprite != null)
        {
            fill_Sprite.localScale = Vector3.Lerp(
                fill_Sprite.localScale,
                new Vector3(_targetFillAmount, 1f, 1f),
                Time.deltaTime * smoothSpeed
            );
        }
    }

    // ─────────────────────────────────────────────────────
    /// <summary>設定當前血量（外部呼叫此方法即可）</summary>
    public void SetHealth(float newHealth)
    {
        enemyBase._currentHealth = (int)Mathf.Clamp(newHealth, 0f, maxHealth);
        _targetFillAmount = (float)currentHealth() / (float)maxHealth;
    }

    public void _initHealthBar()
    {
        maxHealth = enemyBase.levelConfigs[enemyBase.currentEnemyLevel].maxHealth;
        enemyBase._currentHealth = maxHealth;
        _targetFillAmount = 1f;
    }
}
