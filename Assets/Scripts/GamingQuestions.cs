using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamingQuestions : MonoBehaviour
{
    public static GamingQuestions Instance {  get; private set; }
    [Header("問題和答案文字框")]
    public Text Question_Text;
    public Text AnswerLeft_Text;
    public Text AnswerRight_Text;

    [Header("題庫")]
    public string[] QuestionContexts;
    public string[] CorrectAnswerContexts;
    public string[] WrongAnswerContexts;


    private void Start()
    {
        Instance = this;
        //設定GameEvent 觸發答案
    }

    /// <summary>
    /// 亂數出題
    /// </summary>
    public void SetQuestionAndAnswer()
    {
        int randomindex = Random.Range(0, QuestionContexts.Length);
        int mix = randomindex % 2;
        string correctAnswer = CorrectAnswerContexts[randomindex];
        string worngAnswer = WrongAnswerContexts[randomindex];
        Question_Text.text = QuestionContexts[randomindex];
        if (mix == 0)
        {
            AnswerLeft_Text.text = correctAnswer;
            AnswerLeft_Text.gameObject.GetComponent<UITrigger>().triggerName = "Correct";
            AnswerRight_Text.text = worngAnswer;
            AnswerRight_Text.gameObject.GetComponent<UITrigger>().triggerName = "Wrong";
        }
        else
        {
            AnswerLeft_Text.text = worngAnswer;
            AnswerLeft_Text.gameObject.GetComponent<UITrigger>().triggerName = "Wrong";
            AnswerRight_Text.text = correctAnswer;
            AnswerRight_Text.gameObject.GetComponent<UITrigger>().triggerName = "Correct";
        }
    }

}
