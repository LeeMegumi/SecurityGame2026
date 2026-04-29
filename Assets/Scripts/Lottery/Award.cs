using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Award
{
    [Header("物品設定")]
    public int ID;
    public int _probability; //機率 0~100
    public int _amount; //數量
    [Header("物品資料")]
    public string _name; //名稱
}
//獎項資料
public class AwardLists
{
    public List<Award> awardlists=new List<Award>();
   
}
 //獎勵陣列 123.9GB