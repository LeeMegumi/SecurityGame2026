using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LotteryListInstantiate : MonoBehaviour
{
    private Lottery_Done LotteryDone;
    public List<GameObject> currentList;
    public RectTransform LotteryListPageRect;
    public GameObject LotteryList;
    public int currentListnum = 0;
    public int currentpage = 1;

    public Button Right;
    public Button Left;

    private void Awake()
    {
        LotteryDone = GetComponent<Lottery_Done>();
    }
    private void Start()
    {
        Left.interactable = false;
        currentpage = 1; //初始頁面
        
    }

    int currentListPosY()
    {
        return 220 - (currentListnum * 50) + (currentListnum / 8) * 400;
    }

    int currentListPosX()
    {
        return (currentListnum / 8) * 1500;
    }
    int totalpage()
    {
        return ((currentListnum - 1) / 8) + 1;
    }
    public void ListAddBT()
    {
        GameObject ListOBJ = Instantiate(LotteryList, LotteryListPageRect); //新增 物件清單列
        AwardData awardData = ListOBJ.GetComponent<AwardData>(); //取得列 物件資料 欄位

        ListOBJ.transform.localPosition = new Vector3(currentListPosX(), currentListPosY(), 0); //設置物件位置
        awardData.TextData[0].text = currentListnum.ToString(); //寫入物件資料 列ID (新增物件不會有其他資料 僅升成ID
        awardData.TextData[1].text = "New Award"; //獎項名稱
        awardData.TextData[4].text = "0%"; //獎項機率
        awardData.TextData[7].text = "0"; //獎項存量
        LotteryDone.ADDnewAwaeds(); //新增資料庫 清單列
        LotteryDone.m_Award[LotteryDone.m_Award.Count - 1]._name = awardData.TextData[1].text; //寫入資料庫 列ID
        LotteryDone.m_Award[LotteryDone.m_Award.Count - 1]._probability = int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[4].text, @"[^0-9]+", "")); //寫入資料庫 列ID
        LotteryDone.m_Award[LotteryDone.m_Award.Count - 1]._amount = int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[7].text, @"[^0-9]+", "")); //寫入資料庫 列ID
        currentList.Add(ListOBJ);
        currentListnum++;

        if (totalpage() > currentpage)
        {
            LotteryListPageRect.gameObject.transform.localPosition += new Vector3(-1500 * (totalpage() - currentpage), 0, 0);
            currentpage = totalpage();
        }
        Right.interactable = totalpage() > currentpage ? true : false; //更新按鈕開關
        Left.interactable = currentpage > 1 ? true : false;
    }
    public void ListReduceBT()
    {
        if (currentListnum > 0)
        {
            Destroy(currentList[currentList.Count - 1]);
            currentList.Remove(currentList[currentList.Count - 1]);
            LotteryDone.ReduceAwaeds(); //新增資料庫 清單列
            currentListnum--;

            if (currentpage > totalpage())
            {
                currentpage--;
                LotteryListPageRect.gameObject.transform.localPosition += new Vector3(1500, 0, 0);
            }
            Right.interactable = totalpage() > currentpage ? true : false; //更新按鈕開關
            Left.interactable = currentpage <= 1 ? false : true;
        }
    }

    public void RightBT()
    {
        if (totalpage() > currentpage)
        {
            LotteryListPageRect.gameObject.transform.localPosition -= new Vector3(1500, 0, 0);
            currentpage++;
            Left.interactable = true;
            Right.interactable = currentpage >= totalpage() ? false : true;
        }

    }
    public void LeftBT()
    {
        if (currentpage > 1 && totalpage() > 1)
        {
            LotteryListPageRect.gameObject.transform.localPosition += new Vector3(1500, 0, 0);
            currentpage--;
            Right.interactable = true;
            Left.interactable = currentpage <= 1 ? false : true;
        }

    }
    public void SaveToObjs()
    {
        GameEvents.current.SaveAwardOBJ();
        for (int i = 0; i < LotteryDone.m_Award.Count; i++)
        {
            GameObject ListOBJ = currentList[i]; //新增 物件清單列
            AwardData awardData = ListOBJ.GetComponent<AwardData>(); //取得列 物件資料 欄位
            LotteryDone.m_Award[i]._name = awardData.TextData[1].text; //寫入資料庫 列名字
            LotteryDone.m_Award[i]._probability = int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[4].text, @"[^0-9]+", "")); //寫入資料庫 列機率
            LotteryDone.m_Award[i]._amount = int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[7].text, @"[^0-9]+", "")); //寫入資料庫 列數量
        }
    }
    //UI空格存入->暫存再-> 修正至realvalue陣列
    public void LoadToObjs()
    {
        currentListnum = LotteryDone.m_Award.Count; //讀取暫存資料陣列數 更新列
        for (int p = 1; p <= totalpage(); p++) 
        {
            if (p == totalpage() && currentListnum % 8 != 0)  
            {
                for (int l = 220; l > currentListPosY(); l -= 50)
                {
                    GameObject ListOBJ = Instantiate(LotteryList, LotteryListPageRect);
                    ListOBJ.transform.localPosition = new Vector3((p-1) * 1500, l, 0);
                    currentList.Add(ListOBJ);
                }
            }else
            {
                for (int l = 220; l > -180; l -= 50)
                {
                    GameObject ListOBJ = Instantiate(LotteryList, LotteryListPageRect);
                    ListOBJ.transform.localPosition = new Vector3((p-1) * 1500, l, 0);
                    currentList.Add(ListOBJ);
                }
            }
            
        }
        
        //生成載入資料數據

        for (int i = 0; i < LotteryDone.m_Award.Count; i++)
        {
            GameObject ListOBJ = currentList[i]; //新增 物件清單列
            AwardData awardData = ListOBJ.GetComponent<AwardData>(); //取得列 物件資料 欄位
            awardData.TextData[0].text = LotteryDone.m_Award[i].ID.ToString(); //寫入資料庫 列ID
            awardData.TextData[1].text = LotteryDone.m_Award[i]._name; //寫入資料庫 列名稱
            awardData.TextData[4].text = LotteryDone.m_Award[i]._probability.ToString() + "%";//寫入資料庫 列機率
            awardData.TextData[7].text = LotteryDone.m_Award[i]._amount.ToString() + "Per"; //寫入資料庫 列存量
        }
        //填入資料數據

        Right.interactable = totalpage() > currentpage ? true : false; //更新按鈕開關
    }
    //UI BT 數值存入介面和 資料庫

    public void UpdateAmount(int choose)
    {
        AwardData awardData = currentList[choose].GetComponent<AwardData>(); //取得"抽取"列 物件資料 欄位

        awardData.TextData[7].text = (int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[7].text, @"[^0-9]+", "")) - 1).ToString()+"Per";
        LotteryDone.m_Award[choose]._amount = int.Parse(System.Text.RegularExpressions.Regex.Replace(awardData.TextData[7].text, @"[^0-9]+", "")); //寫入資料庫 列數量
    }
   
    /// <summary>
    /// 
    /// </summary>
    public void LoadToObjs_changed()
    {
        for (int i = 0; i < currentList.Count; i++)
        {
            Destroy(currentList[i]);
        }
        currentList.Clear();
        LoadToObjs();
    }
   
}
