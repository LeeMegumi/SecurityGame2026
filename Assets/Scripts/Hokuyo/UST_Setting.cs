using UnityEngine;

public class UST_Setting : MonoBehaviour
{
    public CanvasGroup USTPanelCanvasGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Tab))
        {
            USTPanelCanvasGroup.alpha = USTPanelCanvasGroup.alpha == 0 ? 1 : 0;
            USTPanelCanvasGroup.interactable = USTPanelCanvasGroup.interactable == false ? true : false;
            USTPanelCanvasGroup.blocksRaycasts = USTPanelCanvasGroup.blocksRaycasts == false ? true : false;

        }
    }
}
