using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    // ──────────────────────────────────────────
    [Header("等級設定")]
    public int currentEnemyLevel = 4;

    [Header("各等級屬性（Level 0 → 4，陣列索引對應等級）")]
    public EnemyLevelConfig[] levelConfigs = new EnemyLevelConfig[5];

    // ──────────────────────────────────────────
    [Header("── 狀態檢視 ──")]
    [SerializeField] public int _currentHealth;
    [SerializeField] public float _currentSpeed;
    [SerializeField] public int _currentWallDamage;
    [SerializeField] public int _currentScoreValue;

    [Header("血條設定")]
    public EnemyHealthBar healthBar;
    public Animator EnemyAnimator;

    public SkinnedMeshRenderer[] EnemyMeshRenderer;
    public float dissolveValue; // Dissolve 數值For Animator
    [Header("動畫時間設定")]
    public float deathAnimDuration = 0.8f;   // 死亡動畫播放秒數，依實際動畫調整


    public GameObject[] EnemyOBJ;

    // ★ 物件池所需：記錄自己來自哪個 Prefab 的池
    [HideInInspector] public GameObject SourcePrefab;

    // 內部狀態
    public WaveManager _waveManager;
    private bool _isDead = false;
    private bool _isForceDead = false;

    [Header("── 特殊狀態檢視 ──")]
    public bool WeakMonster = false;

    private AudioSource EnemyAudioSource;
    public AudioClip[] EnemyAudioClips;

    // ──────────────────────────────────────────
    // 初始化（由物件池取出後呼叫）
    // ★ 增加 sourcePrefab 參數 & 完整狀態重置
    // ──────────────────────────────────────────
    private void Start()
    {
        GameEvents.current.OnGameOver += GameOverAction; // 訂閱遊戲結束事件
        EnemyAudioSource = GetComponent<AudioSource>();
    }
    public void Initialize(int level, WaveManager manager, GameObject sourcePrefab = null)
    {
        // 記錄來源 prefab（第一次設定後就不會再改，pool 不會換 prefab）
        if (sourcePrefab != null)
            SourcePrefab = sourcePrefab;

        currentEnemyLevel = level;
        _waveManager = manager;

        // ★ 狀態重置（物件池重用時必須完整清除上一次的殘留）
        _isDead = false;
        _isForceDead = false;

        ClearEnemyOBJ(); 
        GameObject temp = EnemyOBJ[Random.Range(0, EnemyOBJ.Length)];
        temp.SetActive(true);

        ApplyLevelConfig();
        initDissolveValue();  // ★ 初始化 Dissolve 數值（確保每次重用都從完整狀態開始）
        healthBar?._initHealthBar();  // 血條重置

        //第三關角色特殊技能
        if (_waveManager.CurrentWave == 3 && temp.name.Contains("Weak")) 
        {
            //觸發技能
            if (!Main.instance.TipA && _waveManager.CurrentWave == 3)
            {
                Main.instance.TipA = true;
                Main.instance.TipA_Anim.Play("Tip_Show");
            }
            GameEvents.current.EnemyWeakSkill();
            WeakMonster = true;
            var pos = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, -40f));
            EnemyEffectSpawner.instance.Get(pos, lifetime: 2.5f);
            gameObject.transform.localPosition = pos;
            gameObject.transform.localScale = Vector3.one * 3.5f;          
            EnemyAnimator.Play("Attack");
            temp.GetComponentInChildren<Animator>().Play("Weak");  //顯示弱化icon動畫
            return;

        }
        // ★ Animator 重置回預設狀態（Idle/Walk）
        EnemyAnimator.Play("Run");


    }

    void ApplyLevelConfig()
    {
        if (levelConfigs == null || levelConfigs.Length == 0) return;

        int idx = Mathf.Clamp(currentEnemyLevel, 0, levelConfigs.Length - 1);
        EnemyLevelConfig cfg = levelConfigs[idx];

        transform.localScale = Vector3.one * cfg.scale;
        _currentSpeed = cfg.moveSpeed;
        _currentHealth = cfg.maxHealth;
        _currentWallDamage = cfg.wallDamage;
        _currentScoreValue = cfg.scoreValue;
    }

    void Update()
    {
        if (_isDead || _isForceDead)
        {
            updateDissolve(); return;
        }
            

        var animInfo = EnemyAnimator.GetCurrentAnimatorStateInfo(0);
        if (animInfo.IsName("HitA_Normal") || animInfo.IsName("HitB_Normal") ||
            animInfo.IsName("HitA_Weak") || animInfo.IsName("HitB_Weak") || animInfo.IsName("Death"))
            return;

        if(WeakMonster) return;
        transform.Translate(Vector3.back * _currentSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────────
    // ★ 物件池歸還（取代所有 Destroy(gameObject)）
    // ──────────────────────────────────────────

    void ReturnToPool()
    {
        if (SourcePrefab != null)
            EnemyObjectPool.Instance.Return(SourcePrefab, gameObject);
        else
        {
            // Fallback：若沒有設定來源 prefab（理論上不應發生）
            Debug.LogWarning($"[EnemyBase] {name} SourcePrefab 未設定，改用 Destroy！");
            Destroy(gameObject);
        }
    }

    // ──────────────────────────────────────────
    // 受傷
    // ──────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (_isDead || _isForceDead)
        {
            GameEvents.current.Killed(1); // ★ 已死狀態仍然觸發擊殺事件，但分數會有隨機浮動
            GameEvents.current.ScoreGet(SetRendomScore(_currentScoreValue)); // ★ 已死狀態仍然給分，但分數會有隨機浮動
            dissolveValue = .999f; // ★ 已死狀態持續觸發 Dissolve 效果
            return;
        }
        if(WeakMonster)
        {
            string[] HitAnim_Weak = new string[] { "HitA_Weak", "HitB_Weak" };
            
            EnemyAnimator.Play(HitAnim_Weak[Random.Range(0, HitAnim_Weak.Length)]);
        }
        else
        {
            string[] HitAnim_Normal = new string[] { "HitA_Normal", "HitB_Normal" };
            EnemyAnimator.Play(HitAnim_Normal[Random.Range(0, HitAnim_Normal.Length)]);
        }    
        _currentHealth -= amount;
        healthBar.SetHealth(_currentHealth);

        if (_currentHealth <= 0f)
        {
            EnemyAnimator.Play("Death");
            OnDefeated();
        }
           
    }

    // ──────────────────────────────────────────
    // 被玩家消滅 → 播死亡動畫後歸還池
    // ──────────────────────────────────────────

    void OnDefeated()
    {
        if (_isDead) return;
        _isDead = true;

        _waveManager?.NotifyEnemyDefeated(this, triggeredByWall: false);

        // ★ 播放死亡動畫，動畫結束後歸還物件池
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        EnemyAnimator?.Play("Death");
        int randomIndex = Random.Range(1, EnemyAudioClips.Length);
        PlayAudioClip(EnemyAudioClips[randomIndex]); // 播放死亡音效
        yield return new WaitForSeconds(deathAnimDuration);
        ReturnToPool();
    }

    // ──────────────────────────────────────────
    // 碰撞觸發
    // ──────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (_isDead || _isForceDead) return;

        if (other.CompareTag("Bullet"))
        {
            // 受傷邏輯已在 Bullet.cs 處理，這裡不需要再扣血或觸發動畫
        }

        if (other.CompareTag("Wall"))
        {
            if (!Main.instance.TipB && _waveManager.CurrentWave == 3)
            { Main.instance.TipB = true; Main.instance.TipB_Anim.Play("Tip_Show"); }
            Debug.Log("Take Damage : " + _currentWallDamage);
            _isDead = true;

            GameEvents.current.PlayerHealthChange(_currentWallDamage);
            other.GetComponent<Defend_Action>().Hurt();
            _waveManager?.NotifyEnemyDefeated(this, triggeredByWall: true);

            // ★ 改為歸還池，不 Destroy
            ReturnToPool();
        }
    }

    // ──────────────────────────────────────────
    // 時間到強制消滅（不觸發分裂）→ 歸還池
    // ──────────────────────────────────────────

    public void ForceDissolve(float dissolveWaitTime)
    {
        if (_isForceDead) return;
        _isForceDead = true;
        _isDead = true;

        StartCoroutine(DissolveRoutine(dissolveWaitTime));
    }

    IEnumerator DissolveRoutine(float waitTime)
    {
        // 可在此觸發 Dissolve 特效動畫：EnemyAnimator?.SetTrigger("Dissolve");
        yield return new WaitForSeconds(waitTime);
        // ★ 改為歸還池，不 Destroy
        ReturnToPool();
    }

    private void OnDisable()
    {
        // ★ 確保物件被停用時狀態重置（萬一忘了在 Initialize 重置）
        _isDead = false;
        _isForceDead = false;
        dissolveValue = 1f; // Reset Dissolve 數值
    }
    private void ClearEnemyOBJ()
    {
        for (int i = 0; i < EnemyOBJ.Length; i++)
        {
            EnemyOBJ[i].SetActive(false);
        }
    }

    private void updateDissolve()
    {
        if (dissolveValue >= 0 && dissolveValue <= 1)
        {
            dissolveValue -= Time.deltaTime * .8f;
            if (dissolveValue < 0f) dissolveValue = 0f;
            if (EnemyMeshRenderer != null && EnemyMeshRenderer.Length > 0)
            {
                var mpb = new MaterialPropertyBlock();
                for (int i = 0; i < EnemyMeshRenderer.Length; i++)
                {
                    var rend = EnemyMeshRenderer[i];
                    if (rend == null) continue;
                    rend.GetPropertyBlock(mpb);
                    mpb.SetFloat("_Health", dissolveValue);
                    rend.SetPropertyBlock(mpb);
                }
            }
        }

    }
    private void initDissolveValue()
    {
        if (EnemyMeshRenderer != null && EnemyMeshRenderer.Length > 0)
        {
            var mpb = new MaterialPropertyBlock();
            for (int i = 0; i < EnemyMeshRenderer.Length; i++)
            {
                var rend = EnemyMeshRenderer[i];
                if (rend == null) continue;
                rend.GetPropertyBlock(mpb);
                dissolveValue = 1f; // ★ 初始化為完整狀態，溶解狀態從 1 開始
                mpb.SetFloat("_Health", dissolveValue);
                float Hight = transform.localScale.x * 2.5f;  // ★ 根據等級調整溶解效果的高度參數（假設 shader 需要這個參數來控制溶解位置）
                mpb.SetFloat("_CuteoffHeight", Hight);
                rend.SetPropertyBlock(mpb);
            }
        }
    }

    /// <summary>
    /// 玩家死去時，停止一切行動（包含移動、受傷、碰撞等）
    /// </summary>
    private void GameOverAction()
    {
        _currentSpeed = 0; // 停止移動
        string[] danceAnime = new string[] { "DanceA", "DanceB", "DanceC", "DanceD" };
        EnemyAnimator.Play(danceAnime[Random.Range(0, danceAnime.Length)]);// 跳舞
    }

    int SetRendomScore(int scoreValue)
    {
        float randomFactor = Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(scoreValue * randomFactor);
    }
    public void PlayAudioClip(AudioClip clip)
    {
        if (EnemyAudioSource != null && clip != null)
        {
            EnemyAudioSource.PlayOneShot(clip);
        }
    }
    void OnDestroy()
    {
        GameEvents.current.OnGameOver -= GameOverAction; // 取消訂閱，避免記憶體洩漏
    }
}