using UnityEngine;

public class UI_Particle : MonoBehaviour
{
    public GameObject[] _particles;

    public void RandomApear()
    {
        int randomIndex = Random.Range(0, _particles.Length);
        _particles[randomIndex].SetActive(true);
    }

    public void HideAll()
    {
        foreach (GameObject particle in _particles)
        {
            particle.SetActive(false);
        }
    }
}
