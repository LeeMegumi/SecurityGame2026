using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UITo3DSpawner : MonoBehaviour
{
    [Header("UI 參考")]
    public RectTransform targetRectTransform; // 拖入你的 Canvas UI RectTransform
    public Canvas canvas;                     // 拖入 Canvas

    [Header("Cube 設定")]
    public LayerMask raycastLayerMask = ~(1 << 6);     // 要碰撞的 Layer (預設全部)

    [Header("冷卻設定")]
    [Tooltip("每個觸發點的冷卻時間（秒），冷卻期間該位置半徑內不會重複生成")]
    public float cooldownDuration = 1.0f;

    [Header("多點防彈跳設定")]
    [Tooltip("2D 螢幕座標的防彈跳半徑（像素），同一半徑內的重複觸發會被忽略")]
    public float debounceRadius = 10f;
    [Tooltip("同時允許觸發的最大點數")]
    public int maxSimultaneousTriggers = 5;


    

    // 每個活躍觸發點的紀錄結構
    private struct TriggerRecord
    {
        public Vector2 screenPos; // 觸發時的 2D 螢幕座標
        public float spawnTime; // 觸發時的 Time.time
    }

    private readonly List<TriggerRecord> _activeTriggers = new List<TriggerRecord>();

    [Header("偵錯")]
    public bool spawnOnClick = true;

    [Header("相機")]
    public Camera viewCam;

    private void Start()
    {
        raycastLayerMask = ~((1 << 6)|(1 << 7));
    }

    void Update()
    {
        if (spawnOnClick && Input.GetMouseButtonDown(0))
        {
            SpawnBulletAtUIPosition(Input.mousePosition);
        }

    }

    public void SpawnBulletAtUIPosition(Vector2 TouchPos)
    {
        if(!Main.instance.SpawnBulletAllow) {
            Debug.Log("遊戲尚未開始，禁止生成");
            return;
        }

        // ─── 清除所有已過期的觸發點紀錄 ──────────────────────────
        _activeTriggers.RemoveAll(t => Time.time - t.spawnTime >= cooldownDuration);

        // ─── 防彈跳檢查：新觸發點是否在某個現有觸發點的半徑內 ────
        foreach (var trigger in _activeTriggers)
        {
            if (Vector2.Distance(TouchPos, trigger.screenPos) < debounceRadius)
            {
                float remaining = cooldownDuration - (Time.time - trigger.spawnTime);
                //Debug.Log($"[防彈跳] 位置 {TouchPos} 距離現有觸發點太近（半徑 {debounceRadius}px 內），" +
                          //$"冷卻剩餘 {remaining:F2} 秒");
                return;
            }
        }

        // ─── 最大同時觸發點數量限制 ────────────────────────────────
        if (_activeTriggers.Count >= maxSimultaneousTriggers)
        {
            //Debug.Log($"[防彈跳] 已達最大同時觸發點數量上限 ({maxSimultaneousTriggers})，略過此次觸發");
            return;
        }

        // ─── 從 2D 螢幕座標射出 Ray ────────────────────────────────
        Ray ray = viewCam.ScreenPointToRay(TouchPos);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        // ─── Raycast 找到 3D 物件 ──────────────────────────────────
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastLayerMask))
        {
            //Debug.Log($"碰到物件: {hit.collider.name}，世界座標: {hit.point}");

            // ─── 在 hit point 生成 Cube ────────────────────────────
            SpawnCube(hit.point, hit.normal);

            // ✅ 成功生成後，將此觸發點加入活躍列表
            _activeTriggers.Add(new TriggerRecord
            {
                screenPos = TouchPos,
                spawnTime = Time.time
            });

            Debug.Log($"[多點觸控] 目前活躍觸發點數: {_activeTriggers.Count}/{maxSimultaneousTriggers}");
        }
        else
        {
            Debug.LogWarning("Raycast 沒有碰到任何 3D 物件，請確認場景中有帶 Collider 的物件。");
        }
    }

    void SpawnCube(Vector3 position, Vector3 surfaceNormal)
    {
        AttackEffectSpawner.instance.Get(position, Quaternion.identity, 2f); ;

        Debug.Log($"Cube 生成於: {position}");
    }

    Camera GetCanvasCamera()
    {
        // Screen Space - Overlay → camera 為 null
        // Screen Space - Camera / World Space → 使用指定 camera
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;

        return null;
    }

    // ── 對外查詢用屬性 ─────────────────────────────────────────────
    /// <summary>目前活躍（冷卻中）的觸發點數量</summary>
    public int ActiveTriggerCount
    {
        get
        {
            _activeTriggers.RemoveAll(t => Time.time - t.spawnTime >= cooldownDuration);
            return _activeTriggers.Count;
        }
    }

    /// <summary>是否還能接受新的觸發點（未超過上限且無過期清理問題）</summary>
    public bool CanAcceptNewTrigger => ActiveTriggerCount < maxSimultaneousTriggers;
}
