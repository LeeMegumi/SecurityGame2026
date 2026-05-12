using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Main : MonoBehaviour
{

    public static Main instance { get; private set; }
    [Header("是否生成3D物件")]
    public bool SpawnBulletAllow;

    [Header("UI動畫")]
    public Animator StageUI_Animator;
    [Header("倒數3D動畫")]
    public Animator Counter3D_Animator;

    [Header("前導動畫")]
    public VideoPlayer UIVideo;
    public VideoClip[] videoClip;

    public GameObject MissionCompleteVFX;
    public GameObject MissionFailedVFX;

    [Header("遊戲模式")]
    public GameMode currentMode = GameMode.Eazy;
    [Header("流程階段")]
    public GameStage currentStage = GameStage.Home;

    public bool CurrentDamageBuff;
    public bool CurrentRangeBuff;


    public GameObject invincibleIcon;  //無敵圖示，選擇資安模式會顯示，代表玩家不會被敵人攻擊到

    public float ReadyTime = 3f; //準備階段的倒數時間    

    [Header("遊戲狀態")]
    public bool IsGaming;  //是否正在遊戲中，死亡、勝利 等狀態都會變成false
    public enum GameMode
    {
        Eazy,
        Hard
    }

    public enum  GameStage
    {
        Home,
        Tutorial,
        Choose,
        Videos,
        Ready,
        Game,
        Failed,
        Complete,
        Answer,
        Award

    }

    [Header("結果畫面，背景")]
    public Image resultBG_Image;
    [Header("結果畫面，文字")]
    public Image resultText_Image;

    [Header("結果畫面，背景Sprties")]
    public Sprite[] resultBG_Sprites; //0=一般模式失敗背景，1=一般模式成功背景，2=資安模式成功背景
    [Header("結果畫面，文字Sprties")]
    public Sprite[] resultText_Sprites; //0=失敗文字，1=成功文字


    public bool TipA;
    public bool TipB;
    public Animation TipA_Anim;
    public Animation TipB_Anim;

    public Animation AnswerResultAnime;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameEvents.current.OnUITriggered += UICanvasTriggeredAction;  //訂閱透過Trigger切換UI Animator("Name")事件
        GameEvents.current.OnChangeInvincible += TriggerGameMode;  //訂閱遊戲模式切換事件
        GameEvents.current.OnBuff_SetDamage += SwitchAttackDamage;  //訂閱攻擊力增強Buff事件
        GameEvents.current.OnBuff_SetRange += SwitchAttackRange;  //訂閱攻擊範圍增強Buff事件
        GameEvents.current.OnGameOver += GameOverAction;  //訂閱任務失敗事件
        GameEvents.current.OnGameWin += GameWinAction;  //訂閱任務完成事件
        GameEvents.current.OnAnswerQuestion += TriggerAnswerQuestion;  //訂閱答題事件
        currentStage = GameStage.Home;
        GameEvents.current.ChangeInvincible(true); //預設進入選擇階段後是無敵模式，直到選擇困難模式才會變成非無敵...
        ReadyTime = 5f;
        UIVideo.loopPointReached +=  (VideoPlayer vp) =>TriggerNextStage();  //VideoStage結束後自動進入下一階段
        BGMCrossfadeManager.instance.FadeIn(BGMCrossfadeManager.instance.BGM_audioSource[0], .5f); //淡入首頁BGM，時間0.5秒
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Q))
        {
            currentStage = GameStage.Answer;
            GamingQuestions.Instance.SetQuestionAndAnswer();  //在進入Answer階段前先設定好題目和答案
            StageUI_Animator.Play("ResultToAnswer");
            BGMCrossfadeManager.instance.Crossfade(BGMCrossfadeManager.instance.GetCurrentSource(), BGMCrossfadeManager.instance.BGM_audioSource[1], 2f); //切換到Tutorial階段的BGM，淡入時間2秒
            //GameEvents.current.AwardGet("Bad");
        }

        if(Input.GetKeyUp(KeyCode.N))
        {
            TriggerNextStage();
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            //快速開始，進入Ready階段
            UICanvasTriggeredAction("VideoTrigger");
            BGMCrossfadeManager.instance.FadeOut(BGMCrossfadeManager.instance.GetCurrentSource(), 2f); //淡出當前BGM，時間2秒

        }
        switch (currentStage)   
        {
            case GameStage.Home:
                
                break;
            case GameStage.Tutorial:
                
                break;
            case GameStage.Choose:
               
                break;
            case GameStage.Videos:
                
                break;
            case GameStage.Ready:
                //倒數3秒後進入遊戲
                ReadyTime -= Time.deltaTime;
                if(ReadyTime <= 0)
                {
                    currentStage = GameStage.Game;
                    GameStartAction();
                    GameEvents.current.GameStart();  // 觸發遊戲開始事件，waveManager會訂閱這個事件並開始生成敵人
                }
                break;
            case GameStage.Game:
 
                break;
            case GameStage.Answer:

                break;
        }


    }
    /// <summary>
    /// 當UI Trigger方塊被卡牌擊中後，執行相對應"動畫"和動作。
    /// </summary>
    /// <param name="TriggerName"></param>
    void UICanvasTriggeredAction(string TriggerName)
    {
        switch(TriggerName)
        {
            case "HomeTrigger":
                currentStage = GameStage.Tutorial;
                StageUI_Animator.Play("HomeToTutorial");
                BGMCrossfadeManager.instance.Crossfade(BGMCrossfadeManager.instance.BGM_audioSource[0], BGMCrossfadeManager.instance.BGM_audioSource[1], 2f); //切換到Tutorial階段的BGM，淡入時間2秒
                break;
            case "TutorialTrigger":
                currentStage = GameStage.Choose;
                StageUI_Animator.Play("TutorialToChoose");
                BGMCrossfadeManager.instance.FadeOut(BGMCrossfadeManager.instance.BGM_audioSource[1], 2f); //淡出Tutorial階段的BGM，時間2秒

                break;
            case "HardTrigger":
                currentStage = GameStage.Videos;
                GameEvents.current.ChangeInvincible(false);
                UIVideo.clip = videoClip[1];
                UIVideo.Play();
                StageUI_Animator.Play("ChooseToVideo");
                BGMCrossfadeManager.instance.FadeOut(BGMCrossfadeManager.instance.GetCurrentSource(), 2f); //淡出Tutorial階段的BGM，時間2秒

                break;
            case "EazyTrigger":
                currentStage = GameStage.Videos;
                GameEvents.current.ChangeInvincible(true);
                UIVideo.Play();
                UIVideo.clip = videoClip[0];
                StageUI_Animator.Play("ChooseToVideo");
                BGMCrossfadeManager.instance.FadeOut(BGMCrossfadeManager.instance.GetCurrentSource(), 2f); //淡出Tutorial階段的BGM，時間2秒
                break;
            case "VideoTrigger":
                StageUI_Animator.Play("VideoToReady");
                Counter3D_Animator.Play("CountStart");
                currentStage = GameStage.Ready;
                break;
            case "ResultTrigger":
                GamingQuestions.Instance.SetQuestionAndAnswer();  //在進入Answer階段前先設定好題目和答案
                BGMCrossfadeManager.instance.Crossfade(BGMCrossfadeManager.instance.GetCurrentSource(), BGMCrossfadeManager.instance.BGM_audioSource[1], 2f); //淡出Tutorial階段的BGM，時間2秒
                StageUI_Animator.Play("ResultToAnswer");
                currentStage = GameStage.Answer;
                break;
            case "AwardTrigger":
                GamingQuestions.Instance.SetQuestionAndAnswer();  //在進入Answer階段前先設定好題目和答案
                StageUI_Animator.Play("ResultToAnswer");
                StageUI_Animator.Play("AwardToAnswer");
                currentStage = GameStage.Answer;
                break;

        }
    }
    /// <summary>
    /// 透過程式碼快速鍵"N"觸發流程階段的切換，主要用於影片結束後自動進入下一階段，以及從結果頁面進入答題頁面。
    /// </summary>
    void TriggerNextStage()
    {
        switch(currentStage)
        {
            case GameStage.Home:
                currentStage = GameStage.Tutorial;
                StageUI_Animator.Play("HomeToTutorial");
                BGMCrossfadeManager.instance.Crossfade(BGMCrossfadeManager.instance.BGM_audioSource[0], BGMCrossfadeManager.instance.BGM_audioSource[1], 2f); //切換到Tutorial階段的BGM，淡入時間2秒
                break;
            case GameStage.Tutorial:
                currentStage = GameStage.Choose;
                StageUI_Animator.Play("TutorialToChoose");
                break;
            case GameStage.Choose:
                currentStage = GameStage.Videos;
                GameEvents.current.ChangeInvincible(true);
                UIVideo.clip = videoClip[1];
                UIVideo.Play();
                StageUI_Animator.Play("ChooseToVideo");
                BGMCrossfadeManager.instance.FadeOut(BGMCrossfadeManager.instance.GetCurrentSource(), 2f); //淡出Tutorial階段的BGM，時間2秒
                break;
            case GameStage.Videos:
                StageUI_Animator.Play("VideoToReady");
                Counter3D_Animator.Play("CountStart");
                currentStage = GameStage.Ready;
                break;
            case GameStage.Complete:
            case GameStage.Failed:
                currentStage = GameStage.Answer;
                GamingQuestions.Instance.SetQuestionAndAnswer();  //在進入Answer階段前先設定好題目和答案
                StageUI_Animator.Play("ResultToAnswer");
                break;
            case GameStage.Answer:
                //...
                break; 
            case GameStage.Award:
                currentStage = GameStage.Answer;
                GamingQuestions.Instance.SetQuestionAndAnswer();  //在進入Answer階段前先設定好題目和答案
                StageUI_Animator.Play("AwardToAnswer");
                break;
        }
    }

    void TriggerAnswerQuestion(string Answer)
    {
        if(currentStage!= GameStage.Answer) return;  //確保只有在Answer階段才會觸發答題事件
        AnswerResultAnime.Play(Answer == "Correct" ? "Correct" : "Wrong");
        BGMCrossfadeManager.instance.PlaySFX(BGMCrossfadeManager.instance.OneShotAudio[0]
            ,Answer == "Correct" ? BGMCrossfadeManager.instance.Shot_audioClip[1] : BGMCrossfadeManager.instance.Shot_audioClip[0]); //播放正確或錯誤的音效，正確音效音量較大
        //判斷答案是否正確，並觸發相對應的事件
        currentStage = GameStage.Award;
        StageUI_Animator.Play("AnswerToAward");

        GameEvents.current.AwardGet(Answer == "Correct" ? "Normal" : "Bad");
    }

    void TriggerGameMode(bool isEazy)
    {
        if (isEazy)
        {
            invincibleIcon.SetActive(true);
            currentMode = GameMode.Eazy;
        }
        else
        {
            invincibleIcon.SetActive(false);
            currentMode = GameMode.Hard;
        }
    }

    /// <summary>
    /// 增加攻擊力啟用，用於UI上的Buff icon顯示，實際攻擊力的增加在Bullet.cs裡的Buff效果裡實作。
    /// </summary>
    /// <param name="Higher"></param>
    void SwitchAttackDamage(bool Higher)=> CurrentDamageBuff= Higher;
    /// <summary>
    /// 增加攻擊範圍，用於UI上的Buff icon顯示，實際攻擊範圍的增加在Bullet.cs裡的Buff效果裡實作。
    /// </summary>
    /// <param name="Higher"></param>
    void SwitchAttackRange(bool Higher)=> CurrentRangeBuff= Higher;

    private void OnDestroy()
    {
        GameEvents.current.OnUITriggered -= UICanvasTriggeredAction;
        UIVideo.loopPointReached -= (VideoPlayer vp) => { TriggerNextStage(); };  //VideoStage結束後自動進入下一階段;
    }

    void GameStartAction()
    {
        IsGaming = true;
        SpawnBulletAllow = true;
        StageUI_Animator.Play("ReadyToGame");
    }

    void GameOverAction()
    {
        currentStage = GameStage.Failed;
        IsGaming = false;
        SpawnBulletAllow = false;
        MissionFailedVFX.SetActive(true);
        //替換UI動畫為失敗Sprite，文字、背景
        resultBG_Image.sprite = resultBG_Sprites[0];
        resultText_Image.sprite = resultText_Sprites[0];
        //失敗音效
        StageUI_Animator.Play("GameToResult");

    }
    public void GameWinAction()
    {
        currentStage = GameStage.Complete;
        

        IsGaming = false;
        SpawnBulletAllow = false;
        MissionCompleteVFX.SetActive(true);
        //替換UI動畫為成功Sprite，文字、背景
        resultBG_Image.sprite =  currentMode == GameMode.Eazy ? resultBG_Sprites[1] : resultBG_Sprites[2];
        resultText_Image.sprite = resultText_Sprites[1];
        //成功音效
        StageUI_Animator.Play("GameToResult");
    }
}
