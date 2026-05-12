using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingCanvas : MonoBehaviour
{
    public WallTouchVisualizer walltouchvisualizer;
    public CanvasGroup USTPanelCanvasGroup;
    public CanvasGroup LotteryPanelCanvasGroup;
    [SerializeField]private RectTransform LEDOUTPUT_Rect;
    private bool OUTPUT_TextureFull;
    private bool menuDebug;

    public BoxCollider2D menucollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OUTPUT_TextureFull = true;
        menuDebug = false;
        menucollider.isTrigger = !menuDebug;

        LotteryPanelCanvasGroup.alpha = 1;
        LotteryPanelCanvasGroup.interactable = true ;
        LotteryPanelCanvasGroup.blocksRaycasts = true;
        USTPanelCanvasGroup.alpha = 0;
        USTPanelCanvasGroup.interactable = false;
        USTPanelCanvasGroup.blocksRaycasts = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            LotteryPanelCanvasGroup.alpha = LotteryPanelCanvasGroup.alpha == 0 ? 1 : 0;
            LotteryPanelCanvasGroup.interactable = LotteryPanelCanvasGroup.interactable == false ? true : false;
            LotteryPanelCanvasGroup.blocksRaycasts = LotteryPanelCanvasGroup.blocksRaycasts == false ? true : false;
            USTPanelCanvasGroup.alpha = 0;
            USTPanelCanvasGroup.interactable = false;
            USTPanelCanvasGroup.blocksRaycasts =false;
        }
        if (Input.GetKeyUp(KeyCode.CapsLock))
        {
            USTPanelCanvasGroup.alpha = USTPanelCanvasGroup.alpha == 0 ? 1 : 0;
            USTPanelCanvasGroup.interactable = USTPanelCanvasGroup.interactable == false ? true : false;
            USTPanelCanvasGroup.blocksRaycasts = USTPanelCanvasGroup.blocksRaycasts == false ? true : false;
            LotteryPanelCanvasGroup.alpha = 0;
            LotteryPanelCanvasGroup.interactable = false;
            LotteryPanelCanvasGroup.blocksRaycasts = false;
        }

        if (Input.GetKeyUp(KeyCode.U))
        {
            OUTPUT_TextureFull = !OUTPUT_TextureFull; //切換輸出畫面
            LEDOUTPUT_Rect.anchoredPosition = OUTPUT_TextureFull == true ? new Vector2(0, 0) : new Vector2(-384, 156);
            LEDOUTPUT_Rect.localScale = OUTPUT_TextureFull == true ? new Vector2(1, 1) : new Vector2(0.6f, 0.6f);
        }

        if (Input.GetKeyUp(KeyCode.V))
        {
            walltouchvisualizer.triggerVisualDebug = !walltouchvisualizer.triggerVisualDebug;
        }
        if(Input.GetKeyUp(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            menuDebug = !menuDebug; //切換輸出畫面
            menucollider.isTrigger = !menuDebug;
        }


        //設定UI的開關，按Tab開啟抽獎UI，按CapsLock開啟設定UI
        //按U開啟切換輸出畫面，按V開啟牆面觸碰可視化
        //N可以以切換遊戲階段
        //S可以偵測雷達眼偵測範圍
        //C可以完成雷達演範圍設定
        //R可以重新開始遊戲
        //F以資按模式快入開始
    }
}
