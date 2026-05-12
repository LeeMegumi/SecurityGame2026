using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// 負責管理 BestScore.json 的讀寫，以及首頁最佳紀錄 Text UI 的顯示。
/// 請將此腳本掛載在首頁場景的任意 GameObject 上，並在 Inspector 中指定四個 Text UI。
/// </summary>
public class BestScoreManager : MonoBehaviour
{
    // ── 首頁顯示用的四個 Text UI（請在 Inspector 中指定）──
    public Text Best_Health_Text;
    public Text Best_Killed_Text;
    public Text Best_Time_Text;
    public Text Best_Score_Text;

    // JSON 檔案名稱
    private const string FILE_NAME = "BestScore.json";

    // 單例（方便 FinalData 直接呼叫）
    public static BestScoreManager instance;

    // ── 最佳紀錄資料結構 ──
    [System.Serializable]
    public class BestScoreData
    {
        public int health = 0;
        public int killed = 0;
        public float time = 0f;
        public int score = 0;
    }

    // 目前快取的最佳紀錄（可供外部讀取）
    public BestScoreData CurrentBest { get; private set; } = new BestScoreData();

    // ── 取得 StreamingAssets 內的 JSON 完整路徑 ──
    private string FilePath => Path.Combine(Application.streamingAssetsPath, FILE_NAME);

    // ════════════════════════════════════════════
    void Awake()
    {
        // 簡易單例（首頁場景用，不需跨場景保留）
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 場景載入時自動呼叫：讀取 JSON 並更新首頁 Text UI。
    /// </summary>
    void Start()
    {
        LoadAndDisplayBestScore();
    }

    // ════════════════════════════════════════════
    /// <summary>
    /// 讀取 BestScore.json，並將資料顯示在首頁四個 Text 上。
    /// 可在需要重新整理首頁資料時從外部呼叫。
    /// </summary>
    public void LoadAndDisplayBestScore()
    {
        CurrentBest = LoadBestScore();
        RefreshHomeUI();
    }

    // ── 讀取 JSON ──
    public BestScoreData LoadBestScore()
    {
        if (!File.Exists(FilePath))
        {
            Debug.Log("[BestScoreManager] 找不到 BestScore.json，將使用預設值。");
            return new BestScoreData();
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            BestScoreData data = JsonUtility.FromJson<BestScoreData>(json);
            Debug.Log("[BestScoreManager] 成功讀取 BestScore.json");
            return data ?? new BestScoreData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BestScoreManager] 讀取 JSON 失敗：{e.Message}");
            return new BestScoreData();
        }
    }

    // ── 寫入 JSON ──
    public void SaveBestScore(BestScoreData data)
    {
        try
        {
            // 確保 StreamingAssets 資料夾存在（Editor 模式下可能不存在）
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
            CurrentBest = data;
            Debug.Log("[BestScoreManager] BestScore.json 已更新。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BestScoreManager] 寫入 JSON 失敗：{e.Message}");
        }
    }

    // ── 更新首頁 Text UI ──
    private void RefreshHomeUI()
    {
        if (Best_Health_Text != null)
            Best_Health_Text.text = CurrentBest.health.ToString();

        if (Best_Killed_Text != null)
            Best_Killed_Text.text = CurrentBest.killed.ToString();

        if (Best_Time_Text != null)
            Best_Time_Text.text = CurrentBest.time.ToString("F2");  // 保留兩位小數

        if (Best_Score_Text != null)
            Best_Score_Text.text = CurrentBest.score.ToString();
    }
}