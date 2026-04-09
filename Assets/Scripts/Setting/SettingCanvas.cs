using UnityEngine;

public class SettingCanvas : MonoBehaviour
{

    public CanvasGroup USTPanelCanvasGroup;
    public CanvasGroup LotteryPanelCanvasGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
    }
}
