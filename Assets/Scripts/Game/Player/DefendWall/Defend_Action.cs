using UnityEngine;

public class Defend_Action : MonoBehaviour
{
    public enum DefendActionType
    {
        Normal,
        Weaken
    }
    public DefendActionType currentActionType = DefendActionType.Normal;

    public Animator Defend_Animator;
    public Animator Weak_Animator;

    private AudioSource Hurted_Audio;

    [SerializeField] public ParticleSystem brokenParticle; // 從 Inspector 拖入
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _init();
    }

    void _init()
    {
        Hurted_Audio = GetComponent<AudioSource>();
        brokenParticle.Stop();
        currentActionType = DefendActionType.Normal;

    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void Hurt()
    {
        switch(currentActionType)
        {
            case DefendActionType.Normal:
                Defend_Animator.Play("Shield_Hurt");
                Hurted_Audio.Play();
                break;
            case DefendActionType.Weaken:
                brokenParticle.Play();
                Defend_Animator.Play("Shield_WeakenHurt");
                Hurted_Audio.Play();
                break;
        }
        
    }

    public void ShieldTypeChange(DefendActionType newType)
    {
        currentActionType = newType;
        switch (currentActionType)
        {
            case DefendActionType.Normal:
                Defend_Animator.Play("Shield_Normal");
                Weak_Animator.Play("Empty");
                break;
            case DefendActionType.Weaken:
                Defend_Animator.Play("Shield_BeWeaken");
                Weak_Animator.Play("Weak");
                break;
        }
    }
}
