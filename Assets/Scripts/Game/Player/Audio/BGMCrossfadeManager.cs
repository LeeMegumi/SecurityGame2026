using System.Collections;
using UnityEngine;

/// <summary>
/// BGM 淡入淡出轉場管理器
/// 使用方式：呼叫 Crossfade(fadeOut, fadeIn, duration) 即可讓兩個 AudioSource 做聲音融接
/// </summary>
public class BGMCrossfadeManager : MonoBehaviour
{
    // 單例模式，方便全局存取
    public static BGMCrossfadeManager instance { get; private set; }

    // 記錄當前正在播放的 AudioSource
    private AudioSource _currentSource;

    // 記錄當前進行中的 Coroutine，避免重疊執行
    private Coroutine _crossfadeCoroutine;

    public AudioSource[] BGM_audioSource;


    public AudioSource[] OneShotAudio;

    public AudioClip[] Shot_audioClip;
    //0 OneShotAudio[0] UITrigger音效


    private void Awake()
    {
        // 單例初始化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// 執行 BGM 淡入淡出融接
    /// </summary>
    /// <param name="fadeOutSource">要淡出的 AudioSource（當前播放中的 BGM）</param>
    /// <param name="fadeInSource">要淡入的 AudioSource（即將播放的 BGM）</param>
    /// <param name="duration">轉場時間（秒）</param>
    public void Crossfade(AudioSource fadeOutSource, AudioSource fadeInSource, float duration)
    {
        // 若已有轉場進行中，先中斷
        if (_crossfadeCoroutine != null)
        {
            StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = null;
        }

        _crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(fadeOutSource, fadeInSource, duration));
    }

    /// <summary>
    /// 只淡出某個 AudioSource（不淡入任何新音樂）
    /// </summary>
    /// <param name="fadeOutSource">要淡出的 AudioSource</param>
    /// <param name="duration">淡出時間（秒）</param>
    public void FadeOut(AudioSource fadeOutSource, float duration)
    {
        if (_crossfadeCoroutine != null)
        {
            StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = null;
        }

        _crossfadeCoroutine = StartCoroutine(FadeOutRoutine(fadeOutSource, duration));
    }

    /// <summary>
    /// 只淡入某個 AudioSource（從靜音開始播放）
    /// </summary>
    /// <param name="fadeInSource">要淡入的 AudioSource</param>
    /// <param name="duration">淡入時間（秒）</param>
    /// <param name="targetVolume">目標音量（預設為 1）</param>
    public void FadeIn(AudioSource fadeInSource, float duration, float targetVolume = 1f)
    {
        if (_crossfadeCoroutine != null)
        {
            StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = null;
        }

        _crossfadeCoroutine = StartCoroutine(FadeInRoutine(fadeInSource, duration, targetVolume));
    }

    // ─── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// 淡入淡出融接核心邏輯
    /// </summary>
    private IEnumerator CrossfadeRoutine(AudioSource fadeOutSource, AudioSource fadeInSource, float duration)
    {
        // 防呆：若傳入的 Source 為 null 則跳過
        if (fadeOutSource == null && fadeInSource == null)
        {
            Debug.LogWarning("[BGMCrossfadeManager] fadeOutSource 與 fadeInSource 皆為 null，取消轉場。");
            yield break;
        }

        // 記錄淡出起始音量
        float fadeOutStartVolume = fadeOutSource != null ? fadeOutSource.volume : 0f;

        // 確保淡入的 AudioSource 從靜音開始播放
        if (fadeInSource != null)
        {
            // 若尚未播放（Play on Awake 可能已啟動，此處處理未啟動的情況）
            if (!fadeInSource.isPlaying)
            {
                fadeInSource.volume = 0f;
                fadeInSource.Play();
            }
            else
            {
                // 已在播放（Play on Awake），從當前音量歸零後淡入
                fadeInSource.volume = 0f;
            }
        }

        // 若 duration 為 0，直接切換
        if (duration <= 0f)
        {
            if (fadeOutSource != null)
            {
                fadeOutSource.volume = 0f;
                fadeOutSource.Stop();
            }
            if (fadeInSource != null)
            {
                fadeInSource.volume = 1f;
            }
            _currentSource = fadeInSource;
            yield break;
        }

        // 執行融接
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 使用 SmoothStep 讓音量曲線更自然（S 形曲線）
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (fadeOutSource != null)
                fadeOutSource.volume = Mathf.Lerp(fadeOutStartVolume, 0f, smoothT);

            if (fadeInSource != null)
                fadeInSource.volume = Mathf.Lerp(0f, 1f, smoothT);

            yield return null;
        }

        // 確保最終值精準
        if (fadeOutSource != null)
        {
            fadeOutSource.volume = 0f;
            fadeOutSource.Stop(); // 停止播放節省資源（Loop 已停止）
        }

        if (fadeInSource != null)
        {
            fadeInSource.volume = 1f;
        }

        _currentSource = fadeInSource;
        _crossfadeCoroutine = null;

        Debug.Log($"[BGMCrossfadeManager] 轉場完成 → 現在播放：{(fadeInSource != null ? fadeInSource.name : "無")}");
    }

    /// <summary>
    /// 單獨淡出邏輯
    /// </summary>
    private IEnumerator FadeOutRoutine(AudioSource source, float duration)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        _crossfadeCoroutine = null;
    }

    /// <summary>
    /// 單獨淡入邏輯
    /// </summary>
    private IEnumerator FadeInRoutine(AudioSource source, float duration, float targetVolume)
    {
        if (source == null) yield break;

        source.volume = 0f;
        if (!source.isPlaying) source.Play();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            source.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        source.volume = targetVolume;
        _currentSource = source;
        _crossfadeCoroutine = null;
    }

    // ─── 單次音效播放 ───────────────────────────────────────────────────────────

    /// <summary>
    /// 播放單次音效（使用指定的 AudioSource，不影響 BGM）
    /// </summary>
    /// <param name="sfxSource">負責播放音效的 AudioSource（建議專用，不開 Loop）</param>
    /// <param name="clip">要播放的 AudioClip</param>
    /// <param name="volume">播放音量（預設為 1）</param>
    public void PlaySFX(AudioSource sfxSource, AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("[BGMCrossfadeManager] PlaySFX：sfxSource 為 null。");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("[BGMCrossfadeManager] PlaySFX：clip 為 null。");
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// 在指定世界座標播放單次音效（自動建立臨時 AudioSource，播完自動銷毀）
    /// 適合 3D 空間音效，例如爆炸、腳步聲等
    /// </summary>
    /// <param name="clip">要播放的 AudioClip</param>
    /// <param name="position">世界座標位置</param>
    /// <param name="volume">播放音量（預設為 1）</param>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[BGMCrossfadeManager] PlaySFXAtPoint：clip 為 null。");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    /// <summary>
    /// 延遲播放單次音效
    /// </summary>
    /// <param name="sfxSource">負責播放音效的 AudioSource</param>
    /// <param name="clip">要播放的 AudioClip</param>
    /// <param name="delay">延遲秒數</param>
    /// <param name="volume">播放音量（預設為 1）</param>
    public void PlaySFXDelayed(AudioSource sfxSource, AudioClip clip, float delay, float volume = 1f)
    {
        if (sfxSource == null || clip == null)
        {
            Debug.LogWarning("[BGMCrossfadeManager] PlaySFXDelayed：sfxSource 或 clip 為 null。");
            return;
        }

        StartCoroutine(PlaySFXDelayedRoutine(sfxSource, clip, delay, volume));
    }

    /// <summary>
    /// 延遲播放音效的 Coroutine
    /// </summary>
    private IEnumerator PlaySFXDelayedRoutine(AudioSource sfxSource, AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);
        sfxSource.PlayOneShot(clip, volume);
    }

    // ─── 工具方法 ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 取得當前播放中的 AudioSource
    /// </summary>
    public AudioSource GetCurrentSource() => _currentSource;

    /// <summary>
    /// 是否正在轉場中
    /// </summary>
    public bool IsTransitioning() => _crossfadeCoroutine != null;
}