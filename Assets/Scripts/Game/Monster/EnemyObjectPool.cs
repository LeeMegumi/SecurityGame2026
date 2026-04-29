using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵人物件池管理器（Singleton）
/// 以 Prefab reference 為 Key，管理多組物件池
/// 支援三關各自預熱、自動擴充
/// </summary>
public class EnemyObjectPool : MonoBehaviour
{
    public static EnemyObjectPool Instance { get; private set; }

    // Key: prefab, Value: 可用物件佇列
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    // 每個 prefab 的池容器節點（在 Hierarchy 整理用）
    private readonly Dictionary<GameObject, Transform> _poolRoots = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ──────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────

    /// <summary>
    /// 預熱：在遊戲開始前建立 count 個非啟動物件，避免執行時 GC
    /// </summary>
    public void PrewarmPool(GameObject prefab, int count)
    {
        if (prefab == null) return;
        EnsurePoolExists(prefab);

        for (int i = 0; i < count; i++)
        {
            var obj = CreateInstance(prefab);
            obj.SetActive(false);
            _pools[prefab].Enqueue(obj);
        }
        Debug.Log($"[Pool] 預熱完成：{prefab.name} × {count}，" +
                  $"目前池容量：{_pools[prefab].Count}");
    }

    /// <summary>
    /// 取出物件：啟動並設定位置，池空時自動擴充並印 Warning
    /// </summary>
    public GameObject Get(GameObject prefab, Transform parent, Vector3 localPos)
    {
        if (prefab == null) return null;
        EnsurePoolExists(prefab);

        GameObject obj;
        if (_pools[prefab].Count > 0)
        {
            obj = _pools[prefab].Dequeue();
        }
        else
        {
            // 池已空 → 自動擴充（建議增加預熱數量）
            Debug.LogWarning($"[Pool] {prefab.name} 池已空，自動擴充一個物件！" +
                             $"請考慮增加 poolPrewarmCount。");
            obj = CreateInstance(prefab);
        }

        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 歸還物件：停用並放回池，由 EnemyBase.ReturnToPool() 呼叫
    /// </summary>
    public void Return(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null) return;
        EnsurePoolExists(prefab);

        obj.SetActive(false);
        obj.transform.SetParent(_poolRoots[prefab]);
        _pools[prefab].Enqueue(obj);
    }

    // ──────────────────────────────────────────
    // 內部工具
    // ──────────────────────────────────────────

    void EnsurePoolExists(GameObject prefab)
    {
        if (_pools.ContainsKey(prefab)) return;

        _pools[prefab] = new Queue<GameObject>();
        var root = new GameObject($"[Pool] {prefab.name}");
        root.transform.SetParent(transform);
        _poolRoots[prefab] = root.transform;
    }

    GameObject CreateInstance(GameObject prefab)
    {
        var root = _poolRoots.ContainsKey(prefab) ? _poolRoots[prefab] : transform;
        var obj = Instantiate(prefab, root);
        obj.SetActive(false);
        return obj;
    }
}