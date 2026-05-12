using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 結算畫面腳本。
/// 在 Win 的情況下，會與 BestScore.json 比對，
/// 若有項目破紀錄則顯示對應的 Best 圖像 Icon，並儲存新紀錄。
/// </summary>
public class FinalData : MonoBehaviour
{
    [Header("結算數值 Text")]
    public Text Health_Text;
    public Text Killed_Text;
    public Text Time_Text;
    public Text Score_Text;

    [Header("Best 圖像 Icon（對應順序：Health / Killed / Time / Score）")]
    public Image Best_Health_Icon;
    public Image Best_Killed_Icon;
    public Image Best_Time_Icon;
    public Image Best_Score_Icon;

    // ════════════════════════════════════════════
    void Start()
    {
        // 預設隱藏所有 Best Icon
        SetAllBestIconsActive(false);

        // 訂閱遊戲事件
        GameEvents.current.OnGameOver += () => resultUpdateFromData(false);
        GameEvents.current.OnGameWin += () => resultUpdateFromData(true);
    }

    // ════════════════════════════════════════════
    void resultUpdateFromData(bool win)
    {
        // 先隱藏所有 Best Icon
        SetAllBestIconsActive(false);

        if (win)
        {
            // ── 取得當前局資料 ──
            var current = PlayerData.instance.currentPlayercontent;

            int newHealth = current.health;
            int newKilled = current.killed;
            float newTime = current.time;
            int newScore = current.score;

            // ── 更新 Text UI ──
            Health_Text.text = newHealth.ToString();
            Killed_Text.text = newKilled.ToString();
            Time_Text.text = newTime.ToString("F2");
            Score_Text.text = newScore.ToString();

            // ── 讀取舊紀錄（透過 BestScoreManager 或直接讀檔）──
            BestScoreManager.BestScoreData best = GetBestScore();

            bool updatedAny = false;

            // 比對 Health（越高越好）
            if (newHealth > best.health)
            {
                best.health = newHealth;
                ShowBestIcon(Best_Health_Icon);
                updatedAny = true;
            }

            // 比對 Killed（越高越好）
            if (newKilled > best.killed)
            {
                best.killed = newKilled;
                ShowBestIcon(Best_Killed_Icon);
                updatedAny = true;
            }

            // 比對 Time（越高越好；如果您的設計是「越短越好」請將 > 改為 <）
            if (newTime < best.time)
            {
                best.time = newTime;
                ShowBestIcon(Best_Time_Icon);
                updatedAny = true;
            }

            // 比對 Score（越高越好）
            if (newScore > best.score)
            {
                best.score = newScore;
                ShowBestIcon(Best_Score_Icon);
                updatedAny = true;
            }

            // ── 若有任何破紀錄，儲存更新後的 JSON ──
            if (updatedAny)
                SaveBestScore(best);
        }
        else
        {
            // 遊戲失敗：Health 與 Time 無效
            Health_Text.text = "0";
            Killed_Text.text = PlayerData.instance.currentPlayercontent.killed.ToString();
            Time_Text.text = "不可計算";
            Score_Text.text = PlayerData.instance.currentPlayercontent.score.ToString();
        }
    }

    // ════════════════════════════════════════════
    // ── 輔助：讀取 BestScore（優先用 BestScoreManager 單例，否則直接讀檔）──
    private BestScoreManager.BestScoreData GetBestScore()
    {
        if (BestScoreManager.instance != null)
            return BestScoreManager.instance.LoadBestScore();

        // Fallback：BestScoreManager 不在此場景時，直接讀檔
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "BestScore.json");
        if (System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                return JsonUtility.FromJson<BestScoreManager.BestScoreData>(json)
                       ?? new BestScoreManager.BestScoreData();
            }
            catch { /* 讀取失敗則使用預設 */ }
        }
        return new BestScoreManager.BestScoreData();
    }

    // ── 輔助：寫入 BestScore（優先用 BestScoreManager 單例，否則直接寫檔）──
    private void SaveBestScore(BestScoreManager.BestScoreData data)
    {
        if (BestScoreManager.instance != null)
        {
            BestScoreManager.instance.SaveBestScore(data);
            return;
        }

        // Fallback：直接寫檔
        try
        {
            string dir = Application.streamingAssetsPath;
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            string path = System.IO.Path.Combine(dir, "BestScore.json");
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Debug.Log("[FinalData] BestScore.json 已更新（Fallback）。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FinalData] 寫入 JSON 失敗：{e.Message}");
        }
    }

    // ── 輔助：顯示單一 Icon ──
    private void ShowBestIcon(Image icon)
    {
        if (icon != null)
            icon.gameObject.SetActive(true);
    }

    // ── 輔助：一次設定所有 Icon 的顯示狀態 ──
    private void SetAllBestIconsActive(bool active)
    {
        if (Best_Health_Icon != null) Best_Health_Icon.gameObject.SetActive(active);
        if (Best_Killed_Icon != null) Best_Killed_Icon.gameObject.SetActive(active);
        if (Best_Time_Icon != null) Best_Time_Icon.gameObject.SetActive(active);
        if (Best_Score_Icon != null) Best_Score_Icon.gameObject.SetActive(active);
    }
}