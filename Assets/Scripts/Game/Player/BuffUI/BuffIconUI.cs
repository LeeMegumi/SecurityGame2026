using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在單一 Buff Icon Prefab 上。
/// 負責：順時針倒數遮罩、最後 N 秒閃爍、結束後通知 BuffManager。
/// </summary>
public class BuffIconUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;        // 顯示 Buff 圖示
    [SerializeField] private Image maskImage;        // Image Type = Filled，做倒數遮罩
    [SerializeField] private CanvasGroup canvasGroup; // 用於淡入與閃爍

    // 執行時設定
    public BuffData data;
    private float remainingTime;
    private bool isRunning;
    private Coroutine flickerCoroutine;

    // 結束回呼（BuffManager 監聽）
    public System.Action<BuffIconUI> OnExpired;

    // ───────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────

    /// <summary>
    /// 由 BuffManager 呼叫，初始化並開始倒數。
    /// </summary>
    public void Initialize(BuffData buffData)
    {
        data          = buffData;
        remainingTime = buffData.duration;
        isRunning     = true;

        iconImage.sprite  = buffData.icon;
        iconImage.enabled = true;

        // 遮罩初始為完全覆蓋（fillAmount = 0 → 無遮罩顯示 icon）
        // 說明：用一張與 icon 同尺寸的深色圖，fillMethod = Radial360，順時針
        maskImage.fillMethod  = Image.FillMethod.Radial360;
        maskImage.fillOrigin  = (int)Image.Origin360.Top;   // 從12點鐘方向開始
        maskImage.fillClockwise = true;
        maskImage.fillAmount  = 0f;
        maskImage.enabled     = true;

        canvasGroup.alpha = 1f;

        // 淡入
        StopAllCoroutines();
        StartCoroutine(FadeIn(0.2f));
        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// 外部強制移除（例如 Dispel）。
    /// </summary>
    public void ForceExpire()
    {
        if (!isRunning) return;
        StopAllCoroutines();
        StartCoroutine(FadeOutAndDestroy());
    }

    // ───────────────────────────────────────────
    // 倒數主流程
    // ───────────────────────────────────────────

    private IEnumerator CountdownRoutine()
    {
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            // 遮罩進度：已過時間比例，順時針展開代表「剩餘時間縮短」
            float elapsed  = data.duration - remainingTime;
            maskImage.fillAmount = elapsed / data.duration;

            // 進入閃爍區間
            if (remainingTime <= data.flickerThreshold && flickerCoroutine == null)
                flickerCoroutine = StartCoroutine(FlickerRoutine());

            yield return null;
        }

        // 確保遮罩填滿
        maskImage.fillAmount = 1f;
        isRunning = false;

        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    // ───────────────────────────────────────────
    // 閃爍
    // ───────────────────────────────────────────

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            canvasGroup.alpha = 0.1f;
            yield return new WaitForSeconds(data.flickerInterval);
            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(data.flickerInterval);
        }
    }

    // ───────────────────────────────────────────
    // 淡入 / 淡出
    // ───────────────────────────────────────────

    private IEnumerator FadeIn(float duration)
    {
        canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float duration = 0.25f;
        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
            yield return null;
        }

        OnExpired?.Invoke(this);
        Destroy(gameObject);
    }
}
