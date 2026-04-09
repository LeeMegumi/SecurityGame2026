using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents current;
    public event Action OnSaveAwardOBJ; 
    public event Action<string> OnAwardGet;

    private void Awake()
    {
        current = this;
    }
    public void SaveAwardOBJ() => OnSaveAwardOBJ?.Invoke();
    public void AwardGet(string s) => OnAwardGet?.Invoke(s);
}
