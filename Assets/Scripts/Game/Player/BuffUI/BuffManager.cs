using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    [Header("Buff 資料")]
    [SerializeField] private BuffData damageBuffData;
    [SerializeField] private BuffData rangeBuffData;

    [Header("UI 設定")]
    [SerializeField] private GameObject buffIconPrefab;   // BuffIconUI Prefab
    [SerializeField] private RectTransform buffContainer; // HorizontalLayoutGroup（反向排列）

    [Header("滑入動畫")]
    [SerializeField] private float slideInDuration = 0.35f;
    [SerializeField] private float slideInDistance = 80f;

    // ───────────────────────────────────────────
    // 執行時狀態
    // ───────────────────────────────────────────

    // 每種 Buff 同時只存一個實例（可改為 List 支援疊加）
    private readonly Dictionary<string, BuffIconUI> activeBuffs = new();

    // ───────────────────────────────────────────
    // 生命週期
    // ───────────────────────────────────────────

    private void Start()
    {
        GameEvents.current.OnBuff_SetDamage += HandleDamageBuff;
        GameEvents.current.OnBuff_SetRange += HandleRangeBuff;
    }

    

    // ───────────────────────────────────────────
    // 事件處理
    // ───────────────────────────────────────────

    private void HandleDamageBuff(bool active)
    {
        if (active) ShowBuff(damageBuffData);
        else HideBuff(damageBuffData.buffId);
    }

    private void HandleRangeBuff(bool active)
    {
        if (active) ShowBuff(rangeBuffData);
        else HideBuff(rangeBuffData.buffId);
    }

    // ───────────────────────────────────────────
    // 顯示 / 隱藏
    // ───────────────────────────────────────────

    private void ShowBuff(BuffData data)
    {
        // 若已存在，重新計時（刷新）
        if (activeBuffs.TryGetValue(data.buffId, out var existing))
        {
            existing.Initialize(data);
            return;
        }

        // 建立新 Icon
        var go = Instantiate(buffIconPrefab, buffContainer);
        var icon = go.GetComponent<BuffIconUI>();

        icon.OnExpired += OnBuffExpired;
        icon.Initialize(data);

        activeBuffs[data.buffId] = icon;

        // 滑入動畫（由右至左：從正偏移滑至 0）
        StartCoroutine(SlideIn(go.GetComponent<RectTransform>()));
    }

    private void HideBuff(string buffId)
    {
        if (activeBuffs.TryGetValue(buffId, out var icon))
            icon.ForceExpire();
    }

    // ───────────────────────────────────────────
    // 回呼：Icon 到期
    // ───────────────────────────────────────────

    private void OnBuffExpired(BuffIconUI icon)
    {
        // 從字典中移除（ForceExpire 或自然到期都會走這裡）
        foreach (var kvp in activeBuffs)
        {
            if (kvp.Value == icon)
            {
                activeBuffs.Remove(kvp.Key);

                switch(icon.data.buffId)
                {
                    case "DamageBuff":
                        GameEvents.current.Buff_SetDamage(false);
                        break;
                    case "RangeBuff":
                        GameEvents.current.Buff_SetRange(false);
                        break;
                }
            }
        }
    }

    // ───────────────────────────────────────────
    // 滑入動畫（右 → 左）
    // ───────────────────────────────────────────

    private IEnumerator SlideIn(RectTransform rt)
    {
        Vector2 startPos = rt.anchoredPosition + new Vector2(slideInDistance, 0f);
        Vector2 endPos = rt.anchoredPosition;

        rt.anchoredPosition = startPos;

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.deltaTime;
            float ease = EaseOutCubic(Mathf.Clamp01(t / slideInDuration));
            rt.localPosition = Vector2.Lerp(startPos, endPos, ease);
            yield return null;
        }

        rt.localPosition = endPos;
    }

    private static float EaseOutCubic(float x)
        => 1f - Mathf.Pow(1f - x, 3f);


    private void OnDestroy()
    {
        GameEvents.current.OnBuff_SetDamage -= HandleDamageBuff;
        GameEvents.current.OnBuff_SetRange -= HandleRangeBuff;
    }
}
