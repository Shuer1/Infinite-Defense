using UnityEngine;

public class EnemyHeavy : EnemyBase
{
    void Awake()
    {
        maxHealth = 100;
        damage = 15;
        moveSpeed = 1.5f;
        expReward = 50;
    }
}
