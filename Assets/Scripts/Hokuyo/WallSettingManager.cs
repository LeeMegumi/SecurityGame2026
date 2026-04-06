using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════
//  WallSettingData — JSON 序列化資料結構
//  對應 WallTouchDetector 的所有可調整參數
// ═══════════════════════════════════════════════════════════
[System.Serializable]
public class WallSettingData
{
    [Header("感測器位置 (mm)")]
    public float sensorX        = 960f;
    public float sensorY        = 1080f;

    [Header("牆面總尺寸 (mm)")]
    public float wallWidth      = 1920f;
    public float wallHeight     = 1080f;

    [Header("矩形偵測區域 (mm)")]
    public float rectX          = 0f;
    public float rectY          = 0f;
    public float rectWidth      = 1920f;
    public float rectHeight     = 1080f;

    [Header("觸碰判定 (mm)")]
    public float touchThreshold = 30f;
    public float clusterRadius  = 80f;
}

// ═══════════════════════════════════════════════════════════
//  WallSettingManager
//
//  功能：
//    1. 啟動時從 StreamingAssets/WallSetting.json 載入設定
//       → 若檔案不存在，自動以 WallTouchDetector 當前值建立
//    2. 將載入的設定套用到 WallTouchDetector
//    3. 將當前數值顯示於 Canvas UI 的 Text 欄位
//    4. 點擊 Save Button 時：
//       → 讀取有填入值的 InputField
//       → 更新 WallTouchDetector 的參數（即時生效）
//       → 儲存到 WallSetting.json
//       → 刷新 UI Text 顯示
//       → 清空所有 InputField
//
//  UI 掛載說明（Inspector）：
//    - 每個參數對應一個 Text（顯示當前值）
//    - 每個參數對應一個 InputField（輸入新值）
//    - 未填寫的 InputField 保留原本數值不變
//    - 若專案未使用 TMP，將 Text 替換為 Text、
//      InputField 替換為 InputField 即可
// ═══════════════════════════════════════════════════════════
public class WallSettingManager : MonoBehaviour
{
    // ── 參考元件 ───────────────────────────────────────────
    [Header("─── 參考元件 ───")]
    [Tooltip("場景中的 WallTouchDetector")]
    public WallTouchDetector detector;

    // ── 感測器位置 ─────────────────────────────────────────
    [Header("─── 感測器位置 ───")]
    public Text      txtSensorX;
    public InputField inputSensorX;

    public Text      txtSensorY;
    public InputField inputSensorY;

    // ── 牆面總尺寸 ─────────────────────────────────────────
    [Header("─── 牆面總尺寸 ───")]
    public Text      txtWallWidth;
    public InputField inputWallWidth;

    public Text      txtWallHeight;
    public InputField inputWallHeight;

    // ── 矩形偵測區域 ───────────────────────────────────────
    [Header("─── 矩形偵測區域 ───")]
    public Text      txtRectX;
    public InputField inputRectX;

    public Text      txtRectY;
    public InputField inputRectY;

    public Text      txtRectWidth;
    public InputField inputRectWidth;

    public Text      txtRectHeight;
    public InputField inputRectHeight;

    // ── 觸碰判定 ───────────────────────────────────────────
    [Header("─── 觸碰判定 ───")]
    public Text      txtTouchThreshold;
    public InputField inputTouchThreshold;

    public Text      txtClusterRadius;
    public InputField inputClusterRadius;

    // ── Save Button & 狀態訊息 ─────────────────────────────
    [Header("─── 儲存按鈕 & 狀態 ───")]
    [Tooltip("Save Button，點擊後觸發儲存流程")]
    public Button    saveButton;
    [Tooltip("（可選）儲存結果的提示文字 Text")]
    public Text  txtStatus;

    // ── 私有成員 ───────────────────────────────────────────
    private WallSettingData _data = new WallSettingData();
    private string          _filePath;
    private const string    FILE_NAME = "WallSetting.json";
    // ⚠️ 注意：使用者原本輸入 WallSetting.josn（typo），此處統一修正為 .json

    // ═══════════════════════════════════════════════════════
    void Start()
    {
        _filePath = Path.Combine(Application.streamingAssetsPath, FILE_NAME);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClick);

        LoadSettings();
    }

    // ═══════════════════════════════════════════════════════
    // 載入設定流程
    // ═══════════════════════════════════════════════════════
    private void LoadSettings()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                _data = JsonUtility.FromJson<WallSettingData>(json);
                Debug.Log($"[WallSetting] ✅ 已從 JSON 載入設定：{_filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WallSetting] JSON 解析失敗，改用 Detector 當前值：{ex.Message}");
                _data = CreateDataFromDetector();
            }
        }
        else
        {
            Debug.Log("[WallSetting] 未找到 WallSetting.json，從 Detector 建立預設檔案");
            _data = CreateDataFromDetector();
            SaveToFile(); // 自動建立初始 JSON
        }

        ApplyToDetector();
        RefreshUI();
        SetStatus("設定已載入", true);
    }

    // ═══════════════════════════════════════════════════════
    // Save Button 點擊事件
    // ═══════════════════════════════════════════════════════
    public void OnSaveButtonClick()
    {
        int updatedCount = 0;

        // 感測器位置
        if (TryParseInput(inputSensorX,        out float vSensorX))   { _data.sensorX        = vSensorX;   updatedCount++; }
        if (TryParseInput(inputSensorY,        out float vSensorY))   { _data.sensorY        = vSensorY;   updatedCount++; }

        // 牆面總尺寸
        if (TryParseInput(inputWallWidth,      out float vWallW))     { _data.wallWidth      = vWallW;     updatedCount++; }
        if (TryParseInput(inputWallHeight,     out float vWallH))     { _data.wallHeight     = vWallH;     updatedCount++; }

        // 矩形偵測區域
        if (TryParseInput(inputRectX,          out float vRectX))     { _data.rectX          = vRectX;     updatedCount++; }
        if (TryParseInput(inputRectY,          out float vRectY))     { _data.rectY          = vRectY;     updatedCount++; }
        if (TryParseInput(inputRectWidth,      out float vRectW))     { _data.rectWidth      = vRectW;     updatedCount++; }
        if (TryParseInput(inputRectHeight,     out float vRectH))     { _data.rectHeight     = vRectH;     updatedCount++; }

        // 觸碰判定
        if (TryParseInput(inputTouchThreshold, out float vThreshold)) { _data.touchThreshold = vThreshold; updatedCount++; }
        if (TryParseInput(inputClusterRadius,  out float vCluster))   { _data.clusterRadius  = vCluster;   updatedCount++; }

        if (updatedCount == 0)
        {
            SetStatus("⚠️ 請至少填入一個數值再儲存", false);
            return;
        }

        ApplyToDetector();  // 即時套用到 WallTouchDetector
        SaveToFile();       // 寫入 JSON
        RefreshUI();        // 更新 Text 顯示
        ClearAllInputs();   // 清空 InputField

        SetStatus($"✅ 已儲存 {updatedCount} 項設定", true);
        Debug.Log($"[WallSetting] 儲存完成，更新 {updatedCount} 個參數");
    }

    // ═══════════════════════════════════════════════════════
    // 公開 API — 外部腳本可呼叫
    // ═══════════════════════════════════════════════════════

    /// <summary>重新從 JSON 讀取（放棄目前 InputField 中未儲存的輸入）</summary>
    public void ReloadFromFile() => LoadSettings();

    /// <summary>取得目前設定資料的副本</summary>
    public WallSettingData GetCurrentData() => _data;

    // ═══════════════════════════════════════════════════════
    // 私有工具方法
    // ═══════════════════════════════════════════════════════

    // 嘗試解析 InputField 的文字為 float
    // 空字串 / 解析失敗 → 回傳 false（保留原值）
    private bool TryParseInput(InputField field, out float result)
    {
        result = 0f;
        if (field == null) return false;
        string text = field.text.Trim();
        if (string.IsNullOrEmpty(text)) return false;

        // 使用 InvariantCulture 確保小數點用「.」而非「,」
        bool ok = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        if (!ok) Debug.LogWarning($"[WallSetting] 「{text}」無法解析為數字，已略過此欄位");
        return ok;
    }

    // 從 WallTouchDetector 讀取當前值，建立 WallSettingData
    private WallSettingData CreateDataFromDetector()
    {
        if (detector == null) return new WallSettingData();
        return new WallSettingData
        {
            sensorX        = detector.sensorX,
            sensorY        = detector.sensorY,
            wallWidth      = detector.wallWidth,
            wallHeight     = detector.wallHeight,
            rectX          = detector.rectX,
            rectY          = detector.rectY,
            rectWidth      = detector.rectWidth,
            rectHeight     = detector.rectHeight,
            touchThreshold = detector.touchThreshold,
            clusterRadius  = detector.clusterRadius
        };
    }

    // 將 _data 套用到 WallTouchDetector（即時生效）
    private void ApplyToDetector()
    {
        if (detector == null) { Debug.LogError("[WallSetting] 請在 Inspector 指定 WallTouchDetector！"); return; }

        detector.sensorX        = _data.sensorX;
        detector.sensorY        = _data.sensorY;
        detector.wallWidth      = _data.wallWidth;
        detector.wallHeight     = _data.wallHeight;
        detector.rectX          = _data.rectX;
        detector.rectY          = _data.rectY;
        detector.rectWidth      = _data.rectWidth;
        detector.rectHeight     = _data.rectHeight;
        detector.touchThreshold = _data.touchThreshold;
        detector.clusterRadius  = _data.clusterRadius;
    }

    // 將 _data 的當前數值更新到所有 Text 元件
    private void RefreshUI()
    {
        SetText(txtSensorX,        _data.sensorX        );
        SetText(txtSensorY,        _data.sensorY        );
        SetText(txtWallWidth,      _data.wallWidth      );
        SetText(txtWallHeight,     _data.wallHeight     );
        SetText(txtRectX,          _data.rectX          );
        SetText(txtRectY,          _data.rectY          );
        SetText(txtRectWidth,      _data.rectWidth      );
        SetText(txtRectHeight,     _data.rectHeight     );
        SetText(txtTouchThreshold, _data.touchThreshold );
        SetText(txtClusterRadius,  _data.clusterRadius  );
    }

    // 將 _data 寫入 StreamingAssets/WallSetting.json
    private void SaveToFile()
    {
        try
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);

            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(_filePath, json);
            Debug.Log($"[WallSetting] 已寫入：{_filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[WallSetting] 寫入失敗：{ex.Message}");
            SetStatus("❌ 儲存失敗，請查看 Console", false);
        }
    }

    // 清空所有 InputField
    private void ClearAllInputs()
    {
        ClearInput(inputSensorX);
        ClearInput(inputSensorY);
        ClearInput(inputWallWidth);
        ClearInput(inputWallHeight);
        ClearInput(inputRectX);
        ClearInput(inputRectY);
        ClearInput(inputRectWidth);
        ClearInput(inputRectHeight);
        ClearInput(inputTouchThreshold);
        ClearInput(inputClusterRadius);
    }

    // 輔助：設定 Text，保留小數點 2 位
    private void SetText(Text t, float value)
    {
        if (t != null) t.text = value.ToString("F2", CultureInfo.InvariantCulture);
    }

    // 輔助：清空 InputField
    private void ClearInput(InputField f)
    {
        if (f != null) f.text = string.Empty;
    }

    // 輔助：設定狀態訊息
    private void SetStatus(string msg, bool success)
    {
        if (txtStatus == null) return;
        txtStatus.text  = msg;
        txtStatus.color = success
            ? new Color(0.2f, 0.9f, 0.4f) // 綠色
            : new Color(1f,   0.8f, 0.2f); // 黃色（警告）
    }
}
