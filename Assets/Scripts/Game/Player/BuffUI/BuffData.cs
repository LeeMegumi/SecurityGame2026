using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffData", menuName = "Buff System/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("基本設定")]
    public string buffId;
    public string displayName;
    public Sprite icon;

    [Header("時間設定")]
    public float duration = 8f;
    public float flickerThreshold = 3f;
    public float flickerInterval = 0.15f;
}
