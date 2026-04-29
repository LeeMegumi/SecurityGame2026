using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 漂浮廢墟生成器（Unity 6 · 球殼區域版）
/// 掛在場景中的空 GameObject 上，建議位置設在戰鬥平台中心點
/// </summary>
public class FloatingDebrisSpawner : MonoBehaviour
{
    [Header("── 物件設定 ──")]
    [SerializeField] private GameObject[] debrisPrefabs;   // 自訂義 Prefab（留空使用預設 Cube）
    [SerializeField] private int debrisCount = 30;
    [SerializeField] private float minScale = 0.2f;
    [SerializeField] private float maxScale = 1.4f;

    [Header("── 球殼範圍 ──")]
    [SerializeField] private float innerRadius = 5f;       // 內球半徑（戰鬥平台禁區）
    [SerializeField] private float outerRadius = 14f;      // 外球半徑（漂浮邊界）

    [Header("── 物理 / 彈性 ──")]
    [SerializeField, Range(0f, 1f)] private float bounciness = 0.75f;
    [SerializeField, Range(0f, 1f)] private float friction = 0.05f;
    [SerializeField] private float initialSpeedMin = 0.5f;
    [SerializeField] private float initialSpeedMax = 2.5f;

    [Header("── 漂浮行為 ──")]
    [SerializeField] private float maxSpeed = 3.5f;
    [SerializeField] private float boundaryForce = 10f;
    [SerializeField] private float driftForce = 0.6f;
    [SerializeField] private float driftInterval = 2.5f;

    [Header("── 自轉設定 ──")]
    [SerializeField] private float minAngularSpeed = 8f;   // 最低自轉速度（度/秒）
    [SerializeField] private float maxAngularSpeed = 25f;  // 最高自轉速度（度/秒）
    [SerializeField] private float rotationChangeInterval = 5f;   // 換方向間隔（秒）
    [SerializeField] private float rotationCorrection = 2f;   // 追蹤強度（越大越快貼近目標）

    // ── 內部狀態 ──
    private ObjectPool<GameObject>[] _pools;
    private PhysicsMaterial _bounceMat;
    private readonly Dictionary<GameObject, int> _activePoolMap = new();
    private bool _useFallbackCube;

    //─────────────────────────────────────────────────────────
    // 初始化
    //─────────────────────────────────────────────────────────
    private void Awake()
    {
        ValidateRadii();

        _useFallbackCube = (debrisPrefabs == null || debrisPrefabs.Length == 0);
        if (_useFallbackCube)
        {
            Debug.LogWarning("[DebrisSpawner] debrisPrefabs 未設定，自動改用預設 Cube。");
            debrisPrefabs = new GameObject[1];
        }

        _bounceMat = CreateBounceMaterial();
        InitializePools();
    }

    private void Start()
    {
        for (int i = 0; i < debrisCount; i++)
            SpawnOne();
    }

    private void ValidateRadii()
    {
        if (innerRadius < 0f) innerRadius = 0f;
        if (outerRadius <= innerRadius)
        {
            Debug.LogWarning("[DebrisSpawner] outerRadius 必須大於 innerRadius，已自動修正。");
            outerRadius = innerRadius + 1f;
        }
    }

    //─────────────────────────────────────────────────────────
    // 建立每個 Prefab 的獨立 Pool
    //─────────────────────────────────────────────────────────
    private void InitializePools()
    {
        _pools = new ObjectPool<GameObject>[debrisPrefabs.Length];
        int capacityPerPool = Mathf.Max(4, debrisCount / debrisPrefabs.Length + 4);

        for (int i = 0; i < debrisPrefabs.Length; i++)
        {
            int capturedIdx = i;
            bool isFallback = _useFallbackCube;
            GameObject prefab = _useFallbackCube ? null : debrisPrefabs[i];

            _pools[i] = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject obj = isFallback
                        ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                        : Instantiate(prefab, transform);
                    obj.transform.SetParent(transform);
                    obj.SetActive(false);
                    return obj;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj =>
                {
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                },
                actionOnDestroy: Destroy,
                collectionCheck: false,
                defaultCapacity: capacityPerPool,
                maxSize: debrisCount + 10
            );
        }
    }

    //─────────────────────────────────────────────────────────
    // 球殼內均勻隨機採樣
    // 使用立方根法確保體積均勻分佈，避免靠近內球面處過度密集
    //─────────────────────────────────────────────────────────
    private Vector3 RandomPointInShell()
    {
        // 在 [innerRadius³, outerRadius³] 之間均勻取樣後開三次方，
        // 確保每單位體積的採樣機率相等
        float r = Mathf.Pow(
            Random.Range(
                Mathf.Pow(innerRadius, 3f),
                Mathf.Pow(outerRadius, 3f)
            ),
            1f / 3f
        );
        return Random.onUnitSphere * r;
    }

    //─────────────────────────────────────────────────────────
    // 生成單顆碎片
    //─────────────────────────────────────────────────────────
    private void SpawnOne()
    {
        int prefabIdx = Random.Range(0, _pools.Length);
        GameObject obj = _pools[prefabIdx].Get();
        _activePoolMap[obj] = prefabIdx;

        // 球殼內隨機位置
        obj.transform.position = transform.position + RandomPointInShell();
        obj.transform.rotation = Random.rotation;
        obj.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);

        // 彈性材質（套到所有子 Collider）
        foreach (var col in obj.GetComponentsInChildren<Collider>())
            col.material = _bounceMat;

        // Rigidbody（Prefab 沒有時自動新增）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearDamping = 0.08f;
        rb.angularDamping = 0.01f;
        rb.mass = Mathf.Max(0.1f, obj.transform.localScale.x);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = Random.onUnitSphere
                                    * Random.Range(initialSpeedMin, initialSpeedMax);

        // 漂浮腳本（Prefab 沒有時自動新增）
        FloatingDebris fd = obj.GetComponent<FloatingDebris>();
        if (fd == null) fd = obj.AddComponent<FloatingDebris>();
        fd.Setup(transform.position, innerRadius, outerRadius,
                 boundaryForce, maxSpeed, driftForce, driftInterval);
        fd.SetupRotation(minAngularSpeed, maxAngularSpeed,
                 rotationChangeInterval, rotationCorrection);
    }

    //─────────────────────────────────────────────────────────
    // 公開方法：回收 / 重新生成
    //─────────────────────────────────────────────────────────

    /// <summary>回收單顆碎片</summary>
    public void ReleaseDebris(GameObject obj)
    {
        if (!_activePoolMap.TryGetValue(obj, out int idx)) return;
        _pools[idx].Release(obj);
        _activePoolMap.Remove(obj);
    }

    /// <summary>回收所有碎片</summary>
    public void ReleaseAll()
    {
        foreach (var kv in _activePoolMap)
            _pools[kv.Value].Release(kv.Key);
        _activePoolMap.Clear();
    }

    /// <summary>回收後重新生成（重開戰鬥用）</summary>
    public void RespawnAll()
    {
        ReleaseAll();
        for (int i = 0; i < debrisCount; i++)
            SpawnOne();
    }

    //─────────────────────────────────────────────────────────
    // 輔助 / 生命週期
    //─────────────────────────────────────────────────────────
    private PhysicsMaterial CreateBounceMaterial()
    {
        var mat = new PhysicsMaterial("DebrisBounce");
        mat.bounciness = bounciness;
        mat.dynamicFriction = friction;
        mat.staticFriction = friction;
        mat.bounceCombine = PhysicsMaterialCombine.Maximum;
        mat.frictionCombine = PhysicsMaterialCombine.Minimum;
        return mat;
    }

    // Scene 視圖顯示兩個球殼
    private void OnDrawGizmosSelected()
    {
        // 外球（藍色）
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.08f);
        Gizmos.DrawSphere(transform.position, outerRadius);
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, outerRadius);

        // 內球（橘色：禁區）
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.08f);
        Gizmos.DrawSphere(transform.position, innerRadius);
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }

    private void OnDestroy()
    {
        if (_pools == null) return;
        foreach (var pool in _pools)
            pool?.Dispose();
    }
}