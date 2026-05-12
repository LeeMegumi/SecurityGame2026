using CartoonFX;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Bullet : MonoBehaviour
{
    public GameObject EnvironmentHitEffect;
    public GameObject BuffCardHitEffect;

    [Header("初始基本傷害")]
    public int constDamage;
    [Header("初始基本範圍")]
    public float constRange;
    [Header("子彈傷害")]
    public int currentDamage;
    [Header("傷害範圍")]
    public float currentRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Buff"))
        {
            gameObject.GetComponent<Collider>().enabled = false;
            var buffCard = other.GetComponent<BuffCard>();
            buffCard.ByBulletHit();
            BuffCardHitEffect.SetActive(true);
            return;
        }
        if (other.CompareTag("Enemy"))
        {
            gameObject.GetComponent<Collider>().enabled = false;
            SphereCollider sphereCol = GetComponent<SphereCollider>();
            float radius = sphereCol.radius;
            int randomindex = Random.Range(3, BGMCrossfadeManager.instance.Shot_audioClip.Length);
            BGMCrossfadeManager.instance.PlaySFX(BGMCrossfadeManager.instance.OneShotAudio[2], BGMCrossfadeManager.instance.Shot_audioClip[randomindex]);
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider radiusOther in hitColliders)
            {
                if (radiusOther.CompareTag("Enemy"))
                {
                    //Debug.Log(radiusOther.name);

                    var enemy = radiusOther.GetComponent<EnemyBase>();
                    GameEvents.current.ScoreGet(SetRendomScore(enemy._currentScoreValue));
                    enemy.TakeDamage(currentDamage);
                    enemy.PlayAudioClip(enemy.EnemyAudioClips[0]);
                    enemy._waveManager?.NotifyEnemyDefeated(enemy, triggeredByWall: true);
                    HitEffect_Pool.instance.Get(radiusOther.transform.position, Quaternion.identity, 2f, GetTextSize(currentDamage));
                    HitText_Pool.instance.Get(radiusOther.transform.position, Quaternion.identity, 2f, currentDamage);

                }
            }
         
            return;
        }
        if (other.CompareTag("Environment"))
        {
            gameObject.GetComponent<Collider>().enabled = false;
            EnvironmentHitEffect.SetActive(true);
            return;
        }
    }

    /// <summary>
    /// 子彈被啟用時，重置傷害、範圍、特效等狀態，確保每次使用子彈時都是從初始狀態開始
    /// </summary>
    public void _InitBullet()
    {
        gameObject.GetComponent<Collider>().enabled = true;

        EnvironmentHitEffect.SetActive(false);
        BuffCardHitEffect.SetActive(false);
        SetDamage();
        SetRange();
    }

    /// <summary>
    /// 根據是否啟用Buff來調整子彈的傷害，啟用Buff時傷害增加200%，停用Buff時傷害回復到原本的數值，並且在每次調整後都四捨五入到整數，以確保傷害值為整數。
    /// </summary>
    /// <param name="isBuffed"></param>
    public void SetDamage()
    {
        currentDamage = RandomDamage();
        if (Main.instance.CurrentDamageBuff)
        {
            //啟用時傷害增加200%，並四捨五入到整數
            currentDamage = Mathf.RoundToInt(currentDamage * 3f);
        }
        else
        {
            //停用時傷害回復到原本的數值，並四捨五入到整數
            currentDamage = Mathf.RoundToInt(RandomDamage());
        }
    }

    int SetRendomScore(int scoreValue)
    {
        float randomFactor = Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(scoreValue * randomFactor * .1f);
    }
    /// <summary>
    /// 設定基本傷害亂數數值，並且在每次調整後都四捨五入到整數，以確保傷害值為整數。
    /// </summary>
    /// <returns></returns>
    int RandomDamage()
    {
        //傷害在初始傷害的±20%範圍內隨機變動，並四捨五入到整數
        float randomFactor = Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(constDamage * randomFactor);
    }
    /// <summary>
    /// 根據是否啟用Buff來調整子彈的攻擊範圍，啟用Buff時攻擊範圍增加100%，停用Buff時攻擊範圍回復到原本的數值。
    /// </summary>
    public void SetRange(bool _init = false)
    {
        if (_init)
        {
            currentRange = RandomRange();
            GetComponent<SphereCollider>().radius = currentRange;
            return;
        }
        if (Main.instance.CurrentRangeBuff)
        {
            currentRange = RandomRange() * 5f;
            GetComponent<SphereCollider>().radius = currentRange;
        }
        else
        {
            currentRange = RandomRange();
            GetComponent<SphereCollider>().radius = currentRange;
        }
    }
    /// <summary>
    /// 設定基本範圍亂數數值，並且在每次調整後都四捨五入到整數，以確保範圍值為整數。
    /// </summary>
    /// <returns></returns>
    float RandomRange()
    {
        //範圍在初始範圍的±20%範圍內隨機變動，並四捨五入到整數
        float randomFactor = Random.Range(0.8f, 1.2f);
        return constRange * randomFactor;
    }

    /// <summary>
    /// 根據 X 值計算對應的 Y 值
    /// X 範圍：800 ~ 4800
    /// </summary>
    public float GetTextSize(float x)
    {
        if (x <= 1200f)
        {
            // 800~1200 → Y: 0.8~1（線性映射）
            float t = Mathf.InverseLerp(800f, 1200f, x);
            return Mathf.Lerp(0.8f, 1f, t);
        }
        else if (x >= 1500f)
        {
            // 1500 以上直接設為 1.5
            return 1.5f;
        }
        else
        {
            return 1f;
        }
    }

}
