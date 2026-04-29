using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("血條設定")]
    [SerializeField] private Image fillImage;        // 血條 Fill Image
    [SerializeField] private float smoothSpeed = 5f; // Lerp 平滑速度

    [Header("閃爍傷害提示")]
    [SerializeField] private Image damageFlashImage; // 透明閃爍 Image（疊加在畫面上）
    [SerializeField] private float flashDuration = 0.15f;  // 閃爍淡入時間
    [SerializeField] private float flashMaxAlpha = 1f;  // 閃爍最大透明度

    [SerializeField] private int maxHealth;  // 最大血量（從 PlayerData 讀取）

    private float _targetFillAmount;   // 目標 Fill Amount（即時更新）
    private Coroutine _flashCoroutine;

    // ── 公開屬性 ──────────────────────────────────────────
    /// <summary>true = 存活，false = 死亡</summary>
    public bool IsAlive => PlayerData.instance.currentPlayercontent.health > 0f;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (fillImage != null)
            fillImage.fillAmount = 1f;

        // 確保閃爍圖片一開始是完全透明
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }
    }

    private void Start()
    {
        GameEvents.current.OnPlayerHealthChange += OnHealthChange;  //受到傷害時執行
        maxHealth = PlayerData.instance.constHealth;  // 從 PlayerData 讀取最大血量
        _targetFillAmount = 1f;
    }
    private void Update()
    {
        // 平滑地將 Fill Amount 插值到目標值
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(
                fillImage.fillAmount,
                _targetFillAmount,
                Time.deltaTime * smoothSpeed
            );

            // 根據血量比例更新顏色（綠色 → 紅色）
           fillImage.color = GetHealthColor(_targetFillAmount);
        }
    }

    // ─────────────────────────────────────────────────────
    /// <summary>設定當前血量（外部呼叫此方法即可）</summary>
    public void SetHealth(float newHealth)
    {
        float previous = PlayerData.instance.currentPlayercontent.health;
        PlayerData.instance.currentPlayercontent.health = (int)Mathf.Clamp(newHealth, 0f, maxHealth);
        _targetFillAmount = (float)PlayerData.instance.currentPlayercontent.health / (float)maxHealth;

        // 若血量有減少（受傷），觸發閃爍效果
        if (newHealth < previous)
            TriggerDamageFlash();
    }

    /// <summary>
    /// 玩家受到傷害時，扣血，當血量小於等於0時，停止扣血
    /// 玩家擊中回血時，增加血量，當血量大於等於100時，停止回血
    /// </summary>
    /// <param name="value"></param>
    void OnHealthChange(int value)
    {
        int playerHealth = PlayerData.instance.currentPlayercontent.health;
        if (playerHealth <= 0)
        {
            if(Main.instance.IsGaming)
            {
                GameEvents.current.GameOver(); // 觸發玩家死亡事件
            }
            return; // 已死亡且嘗試扣血，忽略
        }
        switch (value)
        {
            case < 0: // 受傷
                playerHealth += value; // value 是負數，所以是扣血
                SetHealth(playerHealth + value);
                PlayerData.instance.currentPlayercontent.health = playerHealth;
                break;
            case > 0: // 治療
                playerHealth += value;
                playerHealth = Mathf.Max(playerHealth, maxHealth); // 確保不超過最大血量
                SetHealth(playerHealth + value);
                break;
        }
        
    }

    // ─────────────────────────────────────────────────────
    private void TriggerDamageFlash()
    {
        if (damageFlashImage == null) return;

        // 若正在閃爍中，中斷舊的重新觸發
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 淡入
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(0f, flashMaxAlpha, elapsed / flashDuration));
            yield return null;
        }

        // 淡出
        elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(flashMaxAlpha, 0f, elapsed / flashDuration));
            yield return null;
        }

        SetFlashAlpha(0f);
        _flashCoroutine = null;
    }

    private void SetFlashAlpha(float alpha)
    {
        Color c = damageFlashImage.color;
        c.a = alpha;
        damageFlashImage.color = c;
    }

    /// <summary>
    /// （HSV 插值，全程保持飽和鮮豔）
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    private Color GetHealthColor(float ratio)
    {
        // Hue: 0 = 紅色, 0.33 = 綠色
        // 直接在 Hue 上插值，Saturation 和 Value 保持高飽和度與亮度
        float hue = Mathf.Lerp(0f, 0.33f, ratio);
        return Color.HSVToRGB(hue, 1f, 1f);
    }
}