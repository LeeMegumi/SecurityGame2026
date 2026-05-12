using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class HealthBar : MonoBehaviour
{
    [Header("血條設定")]
    [SerializeField] private Image fillImage;        // 血條 Fill Image
    [SerializeField] private float smoothSpeed = 5f; // Lerp 平滑速度

    [Header("閃爍傷害提示")]
    [SerializeField] private float flashDuration = 0.15f;  // 閃爍淡入時間
    [SerializeField] private float flashMaxAlpha = 1f;  // 閃爍最大透明度

    [SerializeField] private int maxHealth;  // 最大血量（從 PlayerData 讀取）

    private float _targetFillAmount;   // 目標 Fill Amount（即時更新）


    public Animator healthLock_Anime;   // 簡單模式鎖血動畫（刺蝟盾牌）
    public Animator HurtSparkle_Anime;   //受擊動畫（閃爍）
    // ── 公開屬性 ──────────────────────────────────────────
    /// <summary>true = 存活，false = 死亡</summary>
    public bool IsAlive => PlayerData.instance.currentPlayercontent.health > 0f;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (fillImage != null)
            fillImage.fillAmount = 1f;
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
    public void SetHealth(int value)
    {
        int previous = PlayerData.instance.currentPlayercontent.health;

        switch (value)
        {
            case < 0: // 受傷
                if (Main.instance.currentMode == Main.GameMode.Eazy) // 簡單模式鎖血：血量不可低於 50
                {
                    int lockedHealth = Mathf.Max(previous + value, 50); // 套用傷害，但鎖定下限為 50
                    if (lockedHealth == 50 && previous >= 50) // 血量確實被鎖住了才播動畫
                    {
                        Debug.Log("血量被鎖住了，播放鎖血動畫");
                        healthLock_Anime.Play("Lock");
                    }
                    else if (lockedHealth > 50) // 還沒到鎖血線，正常受擊動畫
                    {
                        Debug.Log("血量還沒被鎖住，正常受擊動畫");
                        HurtSparkle_Anime.Play("HurtSparkle");
                    }
                    PlayerData.instance.currentPlayercontent.health = lockedHealth;
                }
                else // 一般模式，正常扣血
                {
                    PlayerData.instance.currentPlayercontent.health = Mathf.Clamp(previous + value, 0, maxHealth);
                    HurtSparkle_Anime.Play("HurtSparkle");
                }
                break;

            case > 0: // 治療
                PlayerData.instance.currentPlayercontent.health = Mathf.Min(previous + value, maxHealth); // 確保不超過最大血量
                break;
        }

        _targetFillAmount = (float)PlayerData.instance.currentPlayercontent.health / (float)maxHealth;
    }

    /// <summary>
    /// 玩家受到傷害時，扣血，當血量小於等於0時，觸發死亡
    /// 玩家擊中回血時，增加血量，當血量大於等於100時，停止回血
    /// </summary>
    void OnHealthChange(int value)
    {
        SetHealth(value); // 先執行血量變更

        // 扣血後才判斷是否死亡
        if (PlayerData.instance.currentPlayercontent.health <= 0)
        {
            if (Main.instance.IsGaming)
                GameEvents.current.GameOver();
        }
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