using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIParticle_Pool : MonoBehaviour
{
    public static UIParticle_Pool instance { get; private set; }

    [Header("Effect Pool Settings")]
    [Tooltip("要池化的預製件")]
    [SerializeField] private GameObject UIParticlePrefab;

    [Tooltip("初始預先生成的數量")]
    [SerializeField] private int initialSize = 5;

    [Tooltip("池子不足時是否自動擴充")]
    [SerializeField] private bool autoExpand = true;

    private readonly Queue<GameObject> Effect_pool = new Queue<GameObject>();
    [SerializeField] private Transform BornPos;

    // 追蹤每個物件對應的自動歸還 Coroutine
    // 當物件提前手動 Return() 時，可正確取消計時
    private readonly Dictionary<GameObject, Coroutine> _activeCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        // Singleton 設定
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InitializePool();
    }

    /// <summary>
    /// 從池中取出一個物件並啟用它。
    /// </summary>
    /// <param name="position">初始位置</param>
    /// <param name="rotation">初始旋轉</param>
    /// <param name="lifetime">
    ///   自動歸還秒數。
    ///   &lt;= 0 表示不自動歸還，需手動呼叫 Return()。
    /// </param>
    /// <returns>已啟用的 GameObject，池空且不允許擴充時回傳 null</returns>
    public GameObject Get(Vector3 position = default, Quaternion rotation = default, float lifetime = 5f)
    {
        if (Effect_pool.Count == 0)
        {
            if (autoExpand)
            {
                Debug.LogWarning($"[AttackEffectSpawner] 池子已空，自動擴充一個 {UIParticlePrefab.name}");
                CreateObject();
            }
            else
            {
                Debug.LogError("[AttackEffectSpawner] 池子已空且不允許自動擴充！");
                return null;
            }
        }

        GameObject obj = Effect_pool.Dequeue();
        obj.transform.localPosition = position;
        obj.SetActive(true);
        obj.GetComponent<UI_Particle>().RandomApear();

        obj.GetComponent<IPoolable>()?.OnGetFromPool();

        // 若有指定 lifetime，啟動自動歸還計時器
        if (lifetime > 0f)
        {
            Coroutine coroutine = StartCoroutine(AutoReturnRoutine(obj, lifetime));
            _activeCoroutines[obj] = coroutine;
        }

        return obj;
    }

    /// <summary>
    /// 將物件歸還池中並停用它。
    /// 若物件有進行中的自動歸還計時，會一併取消。
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // 取消這個物件的自動歸還 Coroutine（若存在）
        CancelAutoReturn(obj);

        obj.GetComponent<IPoolable>()?.OnReturnToPool();

        obj.SetActive(false);
        obj.GetComponent<UI_Particle>().HideAll();
        // 重製物件狀態（位置、旋轉、父物件等），確保下次 Get() 時是乾淨的狀態
        obj.transform.SetParent(BornPos);
        Effect_pool.Enqueue(obj);
    }

    // ─────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────

    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
            CreateObject();

        Debug.Log($"[HitEffect_Pool] 初始化完成：預製件={UIParticlePrefab.name}，數量={initialSize}");
    }

    private void CreateObject()
    {
        GameObject obj = Instantiate(UIParticlePrefab, BornPos);
        //初始化狀態
        obj.GetComponent<UI_Particle>().HideAll();
        obj.SetActive(false);
        Effect_pool.Enqueue(obj);
    }

    /// <summary>
    /// 若有進行中的自動歸還計時，取消它並清除記錄。
    /// </summary>
    private void CancelAutoReturn(GameObject obj)
    {
        if (_activeCoroutines.TryGetValue(obj, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            _activeCoroutines.Remove(obj);
        }
    }

    /// <summary>
    /// 等待 <paramref name="delay"/> 秒後自動歸還物件。
    /// </summary>
    private IEnumerator AutoReturnRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Coroutine 正常結束時，先清除字典記錄再歸還
        // 避免 Return() 內的 CancelAutoReturn() 重複處理
        _activeCoroutines.Remove(obj);
        Return(obj);
    }
}
