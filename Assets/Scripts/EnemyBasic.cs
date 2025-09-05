using UnityEngine;

public class EnemyBasic : EnemyBase
{
    void Awake()
    {
        maxHealth = 30;
        damage = 5;
        moveSpeed = 3f;
        expReward = 20;
    }
}
