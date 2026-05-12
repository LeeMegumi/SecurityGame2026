using System.Collections;
using UnityEngine.UI;  // 若是使用舊版 Text 改成 
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Text scoreText;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.8f;   // 滾動總時長（秒）
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int displayScore = 0;    // 當前畫面上顯示的分數
    private int targetScore = 0;    // 要滾到的目標分數

    private Coroutine animCoroutine;


    private void Start()
    {
        GameEvents.current.OnScoreGet += OnScoreChange;  // 訂閱事件
        GameEvents.current.OnKilled += OnKillChange;  // 訂閱事件
    }
    // ─── 原本你的加分方法，改成這樣呼叫 ───────────────────────────────
    public void OnScoreChange(int value)
    {
        PlayerData.instance.currentPlayercontent.score += value;
        targetScore = PlayerData.instance.currentPlayercontent.score;

        // 若正在播放就停掉，從目前顯示值繼續往新目標滾
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimateScore(displayScore, targetScore));
    }
    public void OnKillChange(int value) => PlayerData.instance.currentPlayercontent.killed += value;

    // ─── 滾數字的 Coroutine ──────────────────────────────────────────
    private IEnumerator AnimateScore(int from, int to)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float curved = animCurve.Evaluate(t);             // 套用緩動曲線

            displayScore = Mathf.RoundToInt(Mathf.Lerp(from, to, curved));
            scoreText.text = displayScore.ToString("N0");     // 加千分位逗號

            yield return null;   // 等下一幀
        }

        // 確保最後數值完全正確
        displayScore = to;
        scoreText.text = displayScore.ToString("N0");
        animCoroutine = null;
    }
}