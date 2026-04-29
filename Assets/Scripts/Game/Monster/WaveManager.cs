using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WaveManager;

// ──────────────────────────────────────────
// 結算資料
// ──────────────────────────────────────────

[System.Serializable]
public class WaveResult
{
    [Header("關卡")]
    public string waveName = "";
    [Header("生成上限")]
    public int spawnLimit = 0;
    [Header("已生成")]
    public int totalSpawned = 0;
    [Header("已擊敗")]
    public int totalDefeated = 0;
    [Header("最後存活數量")]
    public int totalAlive = 0;
    [Header("通關使用時間")]
    public float timeUsed = 0f;
    [Header("是否有提早完成關卡")]
    public bool clearedEarly = false;
}

// ──────────────────────────────────────────

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        [Header("基本設定")]
        public string waveName = "Wave";
        public float duration = 40f;
        public GameObject monsterPrefab;

        [Header("生成模式")]
        public bool isSpawnWave = true;

        [Header("第一/三關：數量設定")]
        public int totalSpawnLimit = 40;
        public bool autoCalcTarget = false;
        public int targetAliveCount = 20;

        // ★ 物件池預熱數量
        // 第一/三關建議：targetAliveCount + 5（約 25）
        // 第二關建議：wave2InitialCount × 2^maxLevel（約 60）
        [Header("物件池設定")]
        [Tooltip("第一/三關建議: targetAliveCount+5；第二關建議: 60")]
        public int poolPrewarmCount = 25;
    }

    // ──────────────────────────────────────────
    [Header("關卡設定")]
    public WaveConfig[] waves;
    public ObjectSpawner spawner;

    [Header("第二關設定")]
    public float wave2SpawnDelay = 1f;
    public int wave2InitialCount = 3;
    public int wave2InitialLevel = 4;

    [Header("強制消滅設定")]
    public float dissolveWaitTime = 1.5f;

    [Header("地板控制器")]
    public FloorGenerator FloorController;

    public enum WaveState
    {
        NotStarted,
        Running,
        GameOver,
        GameWin
    }
    public WaveState waveState;

    // ──────────────────────────────────────────
    [Header("── 當前狀態 (Read Only) ──")]
    [SerializeField] private int _debugCurrentWave = 0;
    [SerializeField] private float _debugTimeRemaining = 0f;
    [SerializeField] private int _debugSpawnedCount = 0;
    [SerializeField] private int _debugDefeatedCount = 0;
    [SerializeField] private int _debugAliveCount = 0;

    [Header("── 各關結算紀錄 (Read Only) ──")]
    [SerializeField] private List<WaveResult> _waveResults = new List<WaveResult>();

    // 公開屬性供 HUD 取用
    public int CurrentWave => _currentWave + 1;
    public float TimeRemaining => _currentWave < waves.Length
                                       ? Mathf.Max(0f, waves[_currentWave].duration - _waveTimer)
                                       : 0f;
    public int SpawnedCount => _debugSpawnedCount;
    public int DefeatedCount => _debugDefeatedCount;
    public int AliveCount => _debugAliveCount;
    public List<WaveResult> WaveResults => _waveResults;

    // 內部狀態
    private int _currentWave = 0;
    private float _waveTimer = 0f;
    private bool _waveRunning = false;
    private bool _forceEnding = false;

    private List<GameObject> _currentWaveMonsters = new List<GameObject>();

    private int _wave2TotalUnits = 0;
    private int _wave2DefeatedUnits = 0;

    // ──────────────────────────────────────────
    // 初始化 & 物件池預熱
    // ──────────────────────────────────────────

    void Start()
    {
        _waveResults.Clear();
        foreach (var cfg in waves)
        {
            _waveResults.Add(new WaveResult
            {
                waveName = cfg.waveName,
                spawnLimit = cfg.totalSpawnLimit
            });
        }

        // ★ 三關全部預熱，在開場或 Loading 畫面執行，避免遊戲中卡頓
        InitializePools();

        GameEvents.current.OnGameStart += () => StartWave(0);  // 等遊戲開始事件觸發後再開始第一關
        GameEvents.current.OnGameOver += GameOverAction;
        GameEvents.current.OnGameWin += GameWinAction;
    }


    /// <summary>
    /// ★ 預熱所有關卡的物件池
    /// 若 Wave 1 & Wave 3 使用相同 prefab，池容量會累加（正確行為）
    /// </summary>
    void InitializePools()
    {
        waveState = WaveState.NotStarted; // 初始狀態
        foreach (var cfg in waves)
        {
            if (cfg.monsterPrefab == null)
            {
                Debug.LogWarning($"[WaveManager] {cfg.waveName} 的 monsterPrefab 未設定，跳過預熱！");
                continue;
            }
            EnemyObjectPool.Instance.PrewarmPool(cfg.monsterPrefab, cfg.poolPrewarmCount);
        }
    }

    // ──────────────────────────────────────────
    // Update：計時 & 通關判定
    // ──────────────────────────────────────────

    void Update()
    {
        if (!_waveRunning || _forceEnding ||!Main.instance.IsGaming) return;

        _waveTimer += Time.deltaTime;

        _debugCurrentWave = _currentWave + 1;
        _debugTimeRemaining = Mathf.Max(0f, waves[_currentWave].duration - _waveTimer);

        int aliveNow = CountAlive();
        _debugAliveCount = aliveNow;
        _debugSpawnedCount = GetCurrentSpawnedCount();
        _debugDefeatedCount = _debugSpawnedCount - aliveNow;

        WaveConfig cfg = waves[_currentWave];

        if (_waveTimer >= cfg.duration)
        {
            StartCoroutine(ForceEndWave());
            return;
        }

        if (cfg.isSpawnWave)
        {
            if (spawner.TotalSpawned >= cfg.totalSpawnLimit && aliveNow == 0)
                EndWave(clearedEarly: true);
        }
        else
        {
            if (_debugSpawnedCount > 0 && aliveNow == 0)
                EndWave(clearedEarly: true);
        }
    }

    // ──────────────────────────────────────────
    // 開始關卡
    // ──────────────────────────────────────────

    public void StartWave(int waveIndex)
    {
        if (waveIndex >= waves.Length)
        {
            Debug.Log("全部關卡完成！");
            waveState = WaveState.NotStarted; // 所有關卡完成後回到初始狀態
            _waveRunning = false;
            GameEvents.current.GameWin();  // 觸發遊戲勝利事件
            return;
        }

        _currentWave = waveIndex;
        _waveTimer = 0f;
        _waveRunning = true;
        _forceEnding = false;
        _currentWaveMonsters.Clear();

        _debugDefeatedCount = 0;
        _debugSpawnedCount = 0;
        _debugAliveCount = 0;
        _wave2TotalUnits = 0;
        _wave2DefeatedUnits = 0;

        WaveConfig cfg = waves[waveIndex];
        Debug.Log($"開始第 {waveIndex + 1} 關：{cfg.waveName}（時限 {cfg.duration}s）");

        GameEvents.current.ChangeStage();  // 通知遊戲階段改變
        waveState = WaveState.Running;  // 關卡執行中
        FloorController.PlayFlipAnimation();  // 每關開始時翻轉地板

        if (cfg.isSpawnWave && cfg.autoCalcTarget)
            cfg.targetAliveCount = CalcTargetAlive(cfg);  // 自動計算目標存活數量

        if (cfg.isSpawnWave)
            spawner.StartSpawning(cfg, this);  // 一般生成
        else
        {
            spawner.StopSpawning();  //停止一般生成
            StartCoroutine(Wave2StartRoutine(cfg));  // 第二關延遲生成，第一關和第三關不受影響
        }
    }

    // ──────────────────────────────────────────
    // 第二關延遲生成
    // ──────────────────────────────────────────

    IEnumerator Wave2StartRoutine(WaveConfig cfg)
    {
        yield return new WaitForSeconds(wave2SpawnDelay);
        if (!_waveRunning) yield break;

        spawner.SpawnImmediate(cfg.monsterPrefab, wave2InitialLevel, wave2InitialCount);
        _wave2TotalUnits += wave2InitialCount;
        Debug.Log($"第二關：生成 {wave2InitialCount} 隻 Lv{wave2InitialLevel} 敵人");
    }

    // ──────────────────────────────────────────
    // 敵人死亡通知
    // ──────────────────────────────────────────

    public void NotifyEnemyDefeated(EnemyBase enemy, bool triggeredByWall)
    {
        if (!_waveRunning || _forceEnding) return;

        WaveConfig cfg = waves[_currentWave];

        if (!cfg.isSpawnWave)
        {
            int nextLevel = enemy.currentEnemyLevel - 1;
            if (nextLevel >= 0)
            {
                int spawnCount = 2;
                spawner.SpawnImmediate(cfg.monsterPrefab, nextLevel, spawnCount);
                _wave2TotalUnits += spawnCount;
                Debug.Log($"分裂：Lv{enemy.currentEnemyLevel} → 生成 {spawnCount} 隻 Lv{nextLevel}");
            }
        }
    }

    // ──────────────────────────────────────────
    // 正常結關
    // ──────────────────────────────────────────

    void EndWave(bool clearedEarly)
    {
        if (_forceEnding) return;
        _waveRunning = false;
        spawner.StopSpawning();
        RecordResult(clearedEarly);

        Debug.Log($"第 {_currentWave + 1} 關結束！提早通關：{clearedEarly}");

        int next = _currentWave + 1;
        if (next < waves.Length)
            StartWave(next);
        else
        {
            GameEvents.current.GameWin();  // 觸發遊戲勝利事件
            Debug.Log("全部關卡完成！");
        }
            
    }

    // ──────────────────────────────────────────
    // 強制結關（時間到）
    IEnumerator ForceEndWave()
    {
        _forceEnding = true;
        _waveRunning = false;
        spawner.StopSpawning();

        Debug.Log($"第 {_currentWave + 1} 關時間到！強制消滅所有敵人...");

        List<GameObject> toDissolve = new List<GameObject>(_currentWaveMonsters);
        foreach (var m in toDissolve)
        {
            if (m == null || !m.activeInHierarchy) continue; // ★ 已歸還池的跳過

            EnemyBase eb = m.GetComponent<EnemyBase>();
            if (eb != null)
                eb.ForceDissolve(dissolveWaitTime);  // 內部會呼叫 ReturnToPool()
            else
                m.SetActive(false); // Fallback
        }

        // 等待特效播完（ForceDissolve 內的 DissolveRoutine 會在這之前歸還池）
        yield return new WaitForSeconds(dissolveWaitTime + 0.1f);

        RecordResult(clearedEarly: false);
        Debug.Log($"第 {_currentWave + 1} 關強制結束完成");

        int next = _currentWave + 1;
        if (next < waves.Length)
        {
            StartWave(next);
        }
        else
        {
            Debug.Log("全部關卡完成！");
            waveState = WaveState.NotStarted; // 所有關卡完成後回到初始狀態
            _waveRunning = false;
            GameEvents.current.GameWin();  // 觸發遊戲勝利事件
        }
            
    }

    // ──────────────────────────────────────────
    // 結算紀錄
    // ──────────────────────────────────────────

    void RecordResult(bool clearedEarly)
    {
        if (_currentWave >= _waveResults.Count) return;

        int finalAlive = CountAlive();
        int finalSpawned = GetCurrentSpawnedCount();
        int finalDefeated = finalSpawned - finalAlive;

        WaveResult r = _waveResults[_currentWave];
        r.totalSpawned = finalSpawned;
        r.totalDefeated = finalDefeated;
        r.totalAlive = finalAlive;
        r.timeUsed = _waveTimer;
        r.clearedEarly = clearedEarly;

        Debug.Log($"[結算] {r.waveName} → 生成:{r.totalSpawned} 清除:{r.totalDefeated} " +
                  $"存活:{r.totalAlive} 用時:{r.timeUsed:F1}s 提早:{r.clearedEarly}");
    }

    // ──────────────────────────────────────────
    // 工具方法
    // ──────────────────────────────────────────

    int CalcTargetAlive(WaveConfig cfg)
    {
        float safeInterval = Mathf.Max(spawner.maxInterval, 0.5f);
        int autoTarget = Mathf.Max(1, Mathf.RoundToInt(cfg.duration / safeInterval / 2f));
        autoTarget = Mathf.Clamp(autoTarget, 1, cfg.totalSpawnLimit);
        Debug.Log($"[AutoCalc] {cfg.waveName} targetAliveCount = {autoTarget}");
        return autoTarget;
    }

    /// <summary>
    /// ★ 改為判斷 activeInHierarchy（物件池物件停用後不會是 null）
    /// </summary>
    int CountAlive()
    {
        _currentWaveMonsters.RemoveAll(m => m == null || !m.activeInHierarchy);
        return _currentWaveMonsters.Count;
    }

    int GetCurrentSpawnedCount()
    {
        WaveConfig cfg = waves[_currentWave];
        return cfg.isSpawnWave ? (spawner != null ? spawner.TotalSpawned : 0) : _wave2TotalUnits;
    }

    public void RegisterSpawnedMonster(GameObject monster)
    {
        _currentWaveMonsters.Add(monster);
    }

    void GameOverAction()
    {
        waveState = WaveState.GameOver;
        _waveRunning = false;
        spawner.StopSpawning();
        Debug.Log("遊戲結束，停止所有關卡活動");
    }
    void GameWinAction()
    {
        waveState = WaveState.GameWin;
        _waveRunning = false;
        spawner.StopSpawning();
        Debug.Log("遊戲勝利，停止所有關卡活動");
    }
}