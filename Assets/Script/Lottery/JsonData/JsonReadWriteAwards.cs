using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class JsonReadWriteAwards : MonoBehaviour
{
    LotteryListInstantiate lotteryListinstantiate;
    public Lottery_Done lottery_done;
    [Header("物品設定")]
    public int _probability;
    public int _amount;
    [Header("物品資料")]
    public string _name;

    public string DataPath;

    public string Award_name;
    public Text Awardtext;

    private void Awake()
    {
        lotteryListinstantiate = GetComponent<LotteryListInstantiate>();
        lottery_done = GetComponent<Lottery_Done>();
    }
    void Start()
    {
        DataPath = PathByApplication(); //取得(遠端)路徑
        LoadFromJsonStart(DataPath + "/Awards.json");
        lotteryListinstantiate.LoadToObjs();
        GameEvents.current.OnAwardGet += Get_Award;
        //StartCoroutine(CheckFromJson_ExPath(DataPath + "/Awards.json"));
        //CreateFileWatcher(DataPath);
    }
    public void SaveToJson()
    {
        AwardLists awardsData = new AwardLists();
        awardsData.awardlists= new List<Award>();

        for (int i = 0; i < lottery_done.m_Award.Count; i++)
        {
            awardsData.awardlists.Add(lottery_done.m_Award[i]); //新增陣列 
            awardsData.awardlists[i].ID = i;
            //awardsData.awardlists[i]._probability = awardsData.awardlists[i]._amount <= 0 ? 0 : awardsData.awardlists[i]._probability;
            //若數量小於等於0 機率設置為0 反之機率不變
        }
        string json = JsonUtility.ToJson(awardsData, true);
        File.WriteAllText(Application.persistentDataPath + "/Awards.json", json);           
        Debug.Log("Save!");
    }
    public void LoadFromJsonStart(string awardjsonfile)
    {
        StreamReader awardRead = new StreamReader(awardjsonfile);
        string json = awardRead.ReadToEnd();
        awardRead.Close();
        AwardLists awardsData = JsonUtility.FromJson<AwardLists>(json);
        lottery_done.m_Award = awardsData.awardlists; //讀取資料到 暫存資料庫
    }
    public void Get_Award(string mode)
    {
        reload();
        switch(mode)
        {
            case "Normal":
                if (lottery_done.TotalAmount())
                {
                    int num = lottery_done.Choose();
                    while (lottery_done.m_Award[num]._amount <= 0)
                    {
                       // Debug.Log(lottery_done.m_Award[num]._name + num + "No!");
                        num = lottery_done.Choose();
                    }
                    //Debug.Log(lottery_done.m_Award[num]._name + num + "GET!");
                    Award_name = lottery_done.m_Award[num]._name;
                    lottery_done.m_Award[num]._amount -= 1;
                    lotteryListinstantiate.UpdateAmount(num);
                    SaveToJson_ExPath();
                    Debug.Log("GetRight" + num);
                }
                else
                {
                    Award_name = "Sold Out!";
                }
                Awardtext.text = Award_name;
                break;
            case "Bad":
                if (lottery_done.LittleAmount())
                {
                    int num = lottery_done.LittleChoose();
                    while (lottery_done.m_Award[num]._amount <= 0)
                    {
                        //Debug.Log(lottery_done.m_Award[num]._name + num + "No!");
                        num = lottery_done.LittleChoose();
                    }
                    //Debug.Log(lottery_done.m_Award[num]._name + num + "GET!");
                    Award_name = lottery_done.m_Award[num]._name;
                    lottery_done.m_Award[num]._amount -= 1;
                    lotteryListinstantiate.UpdateAmount(num);
                    SaveToJson_ExPath();
                    Debug.Log("GetWrong" + num);
                }
                else
                {
                    Award_name = "Sold Out!";
                }
                Awardtext.text = Award_name;
                
                break;
        }
        
    }

    string PathByApplication()
    {
        StreamReader file = new StreamReader(System.IO.Path.Combine(Application.persistentDataPath, "pathfile.json"));
        string flieContent = file.ReadLine();
        file.Close();
        return flieContent;
    }
    public void SaveToJson_ExPath()
    {
        AwardLists awardsData = new AwardLists();
        awardsData.awardlists = new List<Award>();

        for (int i = 0; i < lottery_done.m_Award.Count; i++)
        {
            awardsData.awardlists.Add(lottery_done.m_Award[i]); //新增陣列 
            awardsData.awardlists[i].ID = i;
            //awardsData.awardlists[i]._probability = awardsData.awardlists[i]._amount <= 0 ? 0 : awardsData.awardlists[i]._probability;
            //若數量小於等於0 機率設置為0 反之機率不變
        }



        string json = JsonUtility.ToJson(awardsData, true);
        StreamWriter awardflie = new StreamWriter(DataPath + "/Awards.json");
        awardflie.Write(json);
        awardflie.Flush();
        awardflie.Dispose();
        awardflie.Close();
    }
    public void ReLoadFromJson_ExPath(string awardjsonfile)
    {
        StreamReader awardRead = new StreamReader(awardjsonfile);
        string json = awardRead.ReadToEnd();
        awardRead.Close();
        AwardLists awardsData = JsonUtility.FromJson<AwardLists>(json);
        lottery_done.m_Award = awardsData.awardlists; //讀取資料到 暫存資料庫
    }
    
    public void reload()
    {
        ReLoadFromJson_ExPath(DataPath + "/Awards.json");
        lotteryListinstantiate.LoadToObjs_changed();
    }
}


