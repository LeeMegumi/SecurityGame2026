using UnityEngine;

public class UITrigger : MonoBehaviour
{
    [Header("Ä²µo¤è¶ô¦WºÙ")]
    public string triggerName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("TriggerBox"))
        {
            if (triggerName == "Correct" || triggerName == "Wrong")
            {
                GameEvents.current.AnswerQuestion(triggerName);
                return;
            }
            GameEvents.current.UITriggered(triggerName);
            Debug.Log(triggerName + "is Triggered");
        }
    }
    
}
