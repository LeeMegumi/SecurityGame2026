using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefabToSpawn;          // 要生成的預製物件
    public float minInterval = 2f;            // 最短間隔（秒）
    public float maxInterval = 5f;            // 最長間隔（秒）

    [Header("Spawn Range")]
    public Vector3 rangeMin = new Vector3(-5f, 1f, 0f);
    public Vector3 rangeMax = new Vector3(5f, 1f, 20f);

    private float _timer;
    private float _nextSpawnTime;

    void Start()
    {
        SetNextSpawnTime();
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _nextSpawnTime)
        {
            SpawnObject();
            _timer = 0f;
            SetNextSpawnTime();
        }
    }

    void SpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("請指定要生成的 Prefab！");
            return;
        }

        Vector3 spawnPosition = new Vector3(
            Random.Range(rangeMin.x, rangeMax.x),
            Random.Range(rangeMin.y, rangeMax.y),  // y 固定為 1f，但仍保持彈性
            Random.Range(rangeMin.z, rangeMax.z)
        );

        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }

    void SetNextSpawnTime()
    {
        _nextSpawnTime = Random.Range(minInterval, maxInterval);
    }

    // 在 Editor 中顯示生成範圍（Gizmos）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Vector3 center = (rangeMin + rangeMax) / 2f;
        Vector3 size = rangeMax - rangeMin;
        size.y = Mathf.Max(size.y, 0.05f); // 防止 y 差為 0 導致 Gizmos 消失
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}