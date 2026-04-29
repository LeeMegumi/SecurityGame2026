using UnityEngine;
using UnityEngine.Rendering;

public class PlayerData : MonoBehaviour
{
    public static PlayerData instance { get; private set; }

    [SerializeField] private bool constInvincible;
    [SerializeField] private int constSocre;
    [SerializeField] public int constHealth;
    [SerializeField] private int constKilled;
    [SerializeField] private float constTime;
    [SerializeField] private int constAttackvalue;
    [SerializeField] private int constAttackrange;

    [Header("玩家資料")]
    [SerializeField] public PlayerContent currentPlayercontent = new PlayerContent();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        _Init();
    }
    // Update is called once per frame
    void Update()
    {

    }
    /// <summary>
    /// 初始化玩家資料
    /// </summary>
    public void _Init()
    {
        currentPlayercontent.invincible = constInvincible;
        currentPlayercontent.health = constHealth;
        currentPlayercontent.score = constSocre;
        currentPlayercontent.killed = constKilled;
        currentPlayercontent.time = constTime;
        currentPlayercontent.AttackValue = constAttackvalue;
        currentPlayercontent.AttackRange = constAttackrange;
    }
   
    void OnScoreChange(int value)
    {
        currentPlayercontent.score += value;
    }
    [System.Serializable]
    public class PlayerContent
    {
        public bool invincible;  //無敵狀態
        public int score;  //總分
        public int health;  //城牆可靠度-血量
        public int killed;  //擊殺數量
        public float time;  //費時

        public int AttackValue;  //攻擊傷害數值
        public int AttackRange;  //攻擊範圍

    }
}
