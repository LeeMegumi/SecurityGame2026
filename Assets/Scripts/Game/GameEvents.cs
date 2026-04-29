using System;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents current;
    //--------------------------------獎勵相關事件--------------------------------
    /// <summary>
    /// 庫存設定進行存檔
    /// </summary>
    public event Action OnSaveAwardOBJ;
    /// <summary>
    /// 從獎品池內取出獎項，根據"Bad"跟"Normal"，做區分。
    /// </summary>
    public event Action<string> OnAwardGet;
    //--------------------------------UI相關事件--------------------------------
    /// <summary>
    /// CanvasUI的觸發方塊被擊中時執行，根據不同"名稱"的方塊執行不同動作，在Main.cs
    /// </summary>
    public event Action<string> OnUITriggered;
    //-------------------------------玩家狀態相關事件--------------------------------
    /// <summary>
    /// 在UI選擇資安模式，改變無敵狀態
    /// </summary>
    public event Action<bool> OnChangeInvincible;
    /// <summary>
    /// 城牆被攻擊時，觸發玩家扣血
    /// 或者玩家擊中回血時，觸發玩家回血
    /// </summary>
    public event Action<int> OnPlayerHealthChange;
    /// <summary>
    /// 得分事件，當玩家擊殺敵人或擊中敵人
    /// </summary>
    public event Action<int> OnScoreGet;
    /// <summary>
    /// 當玩家擊殺敵人事件
    /// </summary>
    public event Action<int> OnKilled;
    /// <summary>
    /// 改變攻擊範圍事件
    /// </summary>
    public event Action<bool> OnBuff_SetRange;
    /// <summary>
    /// 改變攻擊傷害事件
    /// </summary>
    public event Action<bool> OnBuff_SetDamage;

    //-------------------------------遊戲階段相關--------------------------------
    public event Action OnChangeStage;  //遊戲階段改變事件
    public event Action OnGameStart;  //遊戲開始事件
    public event Action OnGameOver;  //遊戲結束事件，失敗
    public event Action OnGameWin;  //遊戲結束事件，勝利
    public event Action<string> OnAnswerQuestion; //答題事件，根據"Correct"跟"Wrong"，做區分。
    //-------------------------------技能相關--------------------------------
    public event Action OnEnemyWeakShield;
    public event Action OnHealShield;

    


    private void Awake()
    {
        current = this;
    }
    //--------------------------------獎勵相關事件--------------------------------
    public void SaveAwardOBJ() => OnSaveAwardOBJ?.Invoke();
    public void AwardGet(string s) => OnAwardGet?.Invoke(s);
    //--------------------------------UI相關事件--------------------------------
    public void UITriggered(string s) => OnUITriggered?.Invoke(s);
    //-------------------------------玩家狀態相關事件--------------------------------
    public void ChangeInvincible(bool b) => OnChangeInvincible?.Invoke(b);
    public void PlayerHealthChange(int i) => OnPlayerHealthChange?.Invoke(i);
    public void ScoreGet(int i) => OnScoreGet?.Invoke(i);
    public void Killed(int i) => OnKilled?.Invoke(i);
    public void Buff_SetRange(bool b) => OnBuff_SetRange?.Invoke(b);
    public void Buff_SetDamage(bool b) => OnBuff_SetDamage?.Invoke(b);

    public void ChangeStage() => OnChangeStage?.Invoke();
    public void GameStart() => OnGameStart?.Invoke();
    public void GameOver() => OnGameOver?.Invoke();
    public void GameWin() => OnGameWin?.Invoke();

    public void EnemyWeakSkill() => OnEnemyWeakShield?.Invoke();

    public void HealShield() => OnHealShield?.Invoke();

    public void AnswerQuestion(string s) => OnAnswerQuestion?.Invoke(s);
}
