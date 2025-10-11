using System.Runtime.Serialization;
using UnityEngine;

public class EnemyBasic : EnemyBase
{
    new void Start()
    {
        base.Start(); // 必须调用基类初始化
        maxHealth = DataManager.GetInt(DataManager.Enemy1MaxHealthKey);
        currentHealth = maxHealth;
        damage = DataManager.GetInt(DataManager.Enemy1DamageKey);
    }
}
