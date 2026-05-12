using UnityEngine;

/// <summary>
/// 單一等級的屬性設定，掛在 EnemyBase 的 Inspector 陣列中
/// </summary>
[System.Serializable]
public class EnemyLevelConfig
{
    [Header("等級標示")]
    public int level = 0;               // 唯讀標示用（0–4）

    [Header("屬性")]
    public float scale = 1f;       // 體積
    public float moveSpeed = 3f;       // 移動速度
    public int maxHealth = 100;     // 最大血量
    public int wallDamage = 10;      // 碰撞城牆造成的傷害
    public int scoreValue = 100;    // 被擊殺後給予玩家的分數
}