using UnityEngine;
using static BuffCard;

public class BuffCard : MonoBehaviour
{
    [Header("關卡資訊")]
    public WaveManager waveManager;

    [Header("卡片資訊")]
    public SkinnedMeshRenderer cardMesh;

    [Header("卡片材質")]
    public Material[] StageAcardMat;
    public Material[] StageBcardMat;
    public Material[] StageCcardMat;

    public Vector2 constSpawnTimeLimit;
    public float SpwanCounter;

    public Animator CardAnimator;

    public bool isActive;
    public enum CardType
    {
        DamageUp,
        RangeUp,
        Scan,
        ShieldHeal

    }
    [Header("卡片功能")]
    public CardType cardType;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameEvents.current.OnGameStart += ResetCardSpwanTime;  //訂閱遊戲開始事件，重設卡牌生成計時器
        GameEvents.current.OnChangeStage += ChangeStageAction;  //訂閱遊戲階段改變事件，重設卡牌
        ResetCardSpwanTime();
    }

    // Update is called once per frame
    void Update()
    {
        //已存在的卡片不再生成
        if (isActive || !Main.instance.IsGaming) return;

        //生成計時器
        SpwanCounter -= Time.deltaTime;
        if (SpwanCounter < 0) 
        {
            isActive = true;
            SetCard();  //根據關卡設定卡片類型和材質
            CardAnimator.CrossFadeInFixedTime("Apear", 0.8f); //播放出場動畫
        }
    }
    public void ByBulletHit()
    {
        //已存在的卡片被子彈擊中才會觸發
        if (!isActive) return;
        CardAnimator.CrossFadeInFixedTime("Get", 0.8f);
        //重置生成計時器
        ResetCardSpwanTime();
        //觸發Buff效果
        BuffTrigger();
    }
    void SetCard()
    {
        Material[] CardFace = cardMesh.materials;
        switch (waveManager.CurrentWave)
        {
            case 1:
                cardType = CardType.DamageUp;
                CardFace[1] = StageAcardMat[Random.Range(0, StageAcardMat.Length)];
                break;
            case 2:
                cardType = CardType.RangeUp;
                CardFace[1] = StageBcardMat[Random.Range(0, StageBcardMat.Length)];
                break;
            case 3:
                int randomCard = Random.Range(0, StageCcardMat.Length);
                CardFace[1] = StageCcardMat[randomCard];
                if (randomCard == 0 || randomCard == 1)
                {
                    cardType = CardType.Scan;
                }
                else
                {
                    cardType = CardType.ShieldHeal;
                }
                break;
            default:
                {
                    cardType = CardType.DamageUp;
                    CardFace[1] = StageAcardMat[Random.Range(0, StageAcardMat.Length)];
                }        
                break;
        }
        cardMesh.materials = CardFace;
    }

    /// <summary>
    /// 觸發不同的Buff效果，DamageUp增加子彈傷害，RangeUp增加子彈傷害範圍，Scan掃描敵人，ShieldHeal回復護盾
    /// </summary>
    void BuffTrigger()
    {
        switch(cardType)
        {
            case CardType.DamageUp:
                //執行攻擊力提升的相關程式碼
                GameEvents.current.Buff_SetDamage(true); //觸發攻擊力提升的事件
                break;
            case CardType.RangeUp:
                //執行攻擊範圍提升的相關程式碼
                GameEvents.current.Buff_SetRange(true); //觸發攻擊範圍提升的事件
                break; 
            case CardType.Scan:
            case CardType.ShieldHeal:
                //執行掃描敵人的相關程式碼
                ScanSkill_Pool.instance.Get(Vector3.zero, Quaternion.identity, 5); //從物件池生成掃描技能特效
                //執行回復護盾的相關程式碼
                GameEvents.current.HealShield(); //觸發回復護盾的事件
                break;
        }

    }
    private void ResetCardSpwanTime()
    {
        SpwanCounter = Random.Range(constSpawnTimeLimit.x, constSpawnTimeLimit.y);
        isActive = false;
    }
    void ChangeStageAction()
    {
        if (isActive)
        {
            CardAnimator.CrossFadeInFixedTime("Apear", 0.8f); //播放出場動畫
            SetCard();  //根據關卡設定卡片類型和材質
            
        }
    }
}
