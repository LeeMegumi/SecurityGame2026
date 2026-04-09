using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AwardData : MonoBehaviour
{
    public Text[] TextData;
    public InputField[] TextInput;
    private void Awake()
    {
        TextData = gameObject.GetComponentsInChildren<Text>();
        TextInput= gameObject.GetComponentsInChildren<InputField>();
    }
    private void Start()
    {
        GameEvents.current.OnSaveAwardOBJ += SaveToObj;
    }
    //Text0 ID
    //Text1 Name
    //Text2 Nono
    //Text3 NameSET TextInput0

    //Text4 Probability
    //Text5 Nono
    //Text6 Probability  SET TextInput1

    //Text7 Amount
    //Text8 Nono
    //Text9 Amount  SET TextInput2
    public void SaveToObj()
    {
        
        TextData[1].text = !System.String.IsNullOrEmpty(TextInput[0].text) ? TextInput[0].text: TextData[1].text;
        TextData[4].text = !System.String.IsNullOrEmpty(TextInput[1].text) ? System.Text.RegularExpressions.Regex.Replace(TextInput[1].text, @"[^0-9]+", "").ToString()+"%": TextData[4].text;
        TextData[7].text = !System.String.IsNullOrEmpty(TextInput[2].text) ? System.Text.RegularExpressions.Regex.Replace(TextInput[2].text, @"[^0-9]+", "").ToString() + "Per" : TextData[7].text;
        //TextData[4].text = int.Parse(System.Text.RegularExpressions.Regex.Replace(TextData[7].text, @"[^0-9]+", "")) <= 0 ? "0%" : TextData[4].text;
        for (int i = 0; i < TextInput.Length; i++)
        {
            TextInput[i].text = "";
        }
    }
    //Set 數值轉存至畫面
    private void OnDestroy()
    {
        GameEvents.current.OnSaveAwardOBJ -= SaveToObj;
    }
}
