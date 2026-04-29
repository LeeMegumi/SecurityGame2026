using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("生成位置設定")]
    public Transform spawnTransform;

    [Header("生成範圍")]
    public Vector3 rangeMin = new Vector3(-5f, 0f, 0f);
    public Vector3 rangeMax = new Vector3(5f, 0f, 20f);

    [Header("動態間隔設定（第一/三關）")]
    public float minInterval = 0.01f;
    public float maxInterval = 2f;

    private WaveManager.WaveConfig _currentConfig;
    private WaveManager _waveManager;
    private bool _isSpawning = false;
    private float _spwanTimer = 0f;
    private float _nextSpawnTime = 1f;

    public int TotalSpawned { get; private set; }

    // ──────────────────────────────────────────
    // 第一/三關：開始定時生成
    // ──────────────────────────────────────────

    public void StartSpawning(WaveManager.WaveConfig config, WaveManager manager)
    {
        _currentConfig = config;
        _waveManager = manager;
        _isSpawning = true;
        TotalSpawned = 0;
        _spwanTimer = 0f;
        _nextSpawnTime = maxInterval;
    }

    public void StopSpawning()
    {
        _isSpawning = false;
    }

    void Update()
    {
        if (!_isSpawning || _currentConfig == null) return;

        if (TotalSpawned >= _currentConfig.totalSpawnLimit)
        {
            _isSpawning = false;
            return;
        }

        _spwanTimer += Time.deltaTime;
        if (_spwanTimer >= _nextSpawnTime)
        {
            SpawnOne(_currentConfig.monsterPrefab, level: 4);
            _spwanTimer = 0f;
            SetNextSpawnTime();
        }
    }

    // ──────────────────────────────────────────
    // 第二關：即時生成（分裂用）
    // ──────────────────────────────────────────

    public void SpawnImmediate(GameObject prefab, int level, int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOne(prefab, level);
    }

    // ──────────────────────────────────────────
    // 核心生成方法（改用物件池）
    // ──────────────────────────────────────────

    GameObject SpawnOne(GameObject prefab, int level)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab 未設定！");
            return null;
        }

        Vector3 pos = new Vector3(
            Random.Range(rangeMin.x, rangeMax.x),
            Random.Range(rangeMin.y, rangeMax.y),
            Random.Range(rangeMin.z, rangeMax.z)
        );

        // ★ 改用物件池取出，不再 Instantiate
        GameObject obj = EnemyObjectPool.Instance.Get(prefab, spawnTransform, pos);
        if (obj == null) return null;

        // ★ 初始化時傳入 prefab，讓 EnemyBase 知道要歸還到哪個池
        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy != null)
            enemy.Initialize(level, _waveManager, prefab);

        TotalSpawned++;
        _waveManager?.RegisterSpawnedMonster(obj);

        EnemyEffectSpawner.instance.Get(pos, lifetime: 2.5f);
        return obj;
    }

    // ──────────────────────────────────────────
    // 動態間隔計算
    // ──────────────────────────────────────────

    void SetNextSpawnTime()
    {
        int aliveCount = spawnTransform != null ? spawnTransform.childCount : 0;
        int targetAlive = _currentConfig?.targetAliveCount ?? 10;

        float t = Mathf.Clamp01((float)aliveCount / Mathf.Max(targetAlive, 1));
        _nextSpawnTime = aliveCount < targetAlive
            ? minInterval
            : Mathf.Lerp(minInterval, maxInterval, t);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = (rangeMin + rangeMax) / 2f;
        Vector3 size = rangeMax - rangeMin;
        size.y = Mathf.Max(size.y, 0.05f);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = Matrix4x4.identity;
    }
}