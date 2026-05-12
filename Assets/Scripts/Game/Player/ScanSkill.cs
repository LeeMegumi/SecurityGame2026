using UnityEngine;

public class ScanSkill : MonoBehaviour
{
    public int constDamage = 1000;
    public int currentDamage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentDamage = RandomDamage();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            SphereCollider sphereCol = GetComponent<SphereCollider>();
            int randomindex = Random.Range(3, BGMCrossfadeManager.instance.Shot_audioClip.Length);
            BGMCrossfadeManager.instance.PlaySFX(BGMCrossfadeManager.instance.OneShotAudio[2], BGMCrossfadeManager.instance.Shot_audioClip[randomindex]);

            currentDamage = RandomDamage();
            var enemy = other.GetComponent<EnemyBase>();
            GameEvents.current.ScoreGet(SetRendomScore(enemy._currentScoreValue));
            enemy.TakeDamage(currentDamage);
            enemy._waveManager?.NotifyEnemyDefeated(enemy, triggeredByWall: true);
            HitEffect_Pool.instance.Get(other.transform.position, Quaternion.identity, 2f, GetTextSize(currentDamage));
            HitText_Pool.instance.Get(other.transform.position, Quaternion.identity, 2f, currentDamage);
            return;
        }
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

    int SetRendomScore(int x)
    {
        //分數在初始分數的±20%範圍內隨機變動，並四捨五入到整數
        float randomFactor = Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(x * randomFactor);
    }
}
