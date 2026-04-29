using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lottery_Done : MonoBehaviour
{
    [SerializeField] public List<Award> m_Award;
    public bool TotalAmount()
    {
        int total = 0;
        for (int i = 0; i < m_Award.Count; i++)
        {
            total += m_Award[i]._amount;
        }
        return total <= 0 ? false : true;
    }
    public bool LittleAmount()
    {
        int total = 0;
        for (int i = 0; i < 10; i++)
        {
            total += m_Award[i]._amount;
        }
        return total <= 0 ? false : true;
    }
    public int Choose()
    {
        float total = 0;
        for (int i = 0; i < m_Award.Count; i++)
        {
            total += m_Award[i]._probability;
        }
        //Debug.Log("total:" + total);
        float randomPoint = Random.value * total;
        //Debug.Log("randomPoint" + randomPoint);

        for (int i = 0; i < m_Award.Count; i++)
        {
            if (randomPoint < m_Award[i]._probability)
            {
                //Debug.Log("i" + i);
                return i;
            }
            else
            {
                randomPoint -= m_Award[i]._probability;
                //Debug.Log("randomPoint" + randomPoint);
            }
        }
        //Debug.Log("m_Loots.Length - 1" + (m_Award.Count - 1));
        return m_Award.Count - 1;
    }
    //抽!! return 獎品編號 m_award[i];
    public int LittleChoose()
    {
        float total = 0;
        for (int i = 0; i < 10; i++)
        {
            total += m_Award[i]._probability;
        }
        //Debug.Log("total:" + total);
        float randomPoint = Random.value * total;
        //Debug.Log("randomPoint" + randomPoint);

        for (int i = 0; i < 10; i++)
        {
            if (randomPoint < m_Award[i]._probability)
            {
                //Debug.Log("i" + i);
                return i;
            }
            else
            {
                randomPoint -= m_Award[i]._probability;
                //Debug.Log("randomPoint" + randomPoint);
            }
        }
        //Debug.Log("m_Loots.Length - 1" + (m_Award.Count - 1));
        return m_Award.Count - 1;
    }
    public void ADDnewAwaeds()
    {
        m_Award.Add(new Award());//Awards Length
    }
    public void ReduceAwaeds()
    {
        m_Award.Remove(m_Award[m_Award.Count-1]);//Awards Length
    }
}
