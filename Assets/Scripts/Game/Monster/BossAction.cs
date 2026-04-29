using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{

    [SerializeField]private ParticleSystem brokenParticle; // 從 Inspector 拖入
    [SerializeField]private GameObject[] bossModels;

    [SerializeField] public WaveManager waveManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameEvents.current.OnChangeStage+= ChangeStageAction;
        GameEvents.current.OnGameWin += () =>bossModels[2].GetComponent<Animator>().Play("Death");
        brokenParticle.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ChangeStageAction()=> StartCoroutine(BossApearDelay());
    IEnumerator BossApearDelay()
    {
        brokenParticle.Play();
        yield return new WaitForSeconds(0.25f);
        switch (waveManager.CurrentWave)
        {
            case 1:
                bossModels[0].SetActive(true);
                bossModels[1].SetActive(false);
                bossModels[2].SetActive(false);
                break;
            case 2:
                bossModels[0].SetActive(false);
                bossModels[1].SetActive(true);
                bossModels[2].SetActive(false);
                break;
            case 3:
                ;
                bossModels[0].SetActive(false);
                bossModels[1].SetActive(false);
                bossModels[2].SetActive(true);
                break;
        }
    }
}
