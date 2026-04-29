using UnityEngine;

public class ShieldWall : MonoBehaviour
{
    public Defend_Action[] DefendShields;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameEvents.current.OnEnemyWeakShield += ShieldBeWeak;
        GameEvents.current.OnHealShield += ShieldBeHeal;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ShieldBeWeak()
    {
        int randomIndex = Random.Range(0, DefendShields.Length);
        if (DefendShields[randomIndex].currentActionType != Defend_Action.DefendActionType.Weaken)
        {
            DefendShields[randomIndex].ShieldTypeChange(Defend_Action.DefendActionType.Weaken);
            DefendShields[randomIndex].brokenParticle.Play();
        }
            
    }
    void ShieldBeHeal()
    {
        for (int i = 0; i < DefendShields.Length; i++)
        {
            if (DefendShields[i].currentActionType == Defend_Action.DefendActionType.Weaken)
            {
                DefendShields[i].ShieldTypeChange(Defend_Action.DefendActionType.Normal);
            }
        }
        
    }
}
