using UnityEngine;

/// <summary>
/// 冰霜弹类，具有小范围群体减速特性
/// </summary>
public class BulletFrozen : BulletBase
{
    [Header("冰霜属性")]
    [Tooltip("减速范围半径")]
    public float slowRadius = 2f;
    
    [Tooltip("减速百分比(0-1)")]
    public float slowPercentage = 0.5f;
    
    [Tooltip("减速持续时间(秒)")]
    public float slowDuration = 3f;
    
    [Tooltip("冰霜特效")]
    public GameObject frozenEffect;
    
    [Tooltip("命中特效")]
    public GameObject hitEffect;

    /// <summary>
    /// 命中目标处理 - 范围减速
    /// </summary>
    protected override void OnHitTarget(Collider target)
    {
        // 播放命中特效
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.LookRotation(direction));
        }

        // 对直接命中的目标造成伤害
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        // 对范围内敌人施加减速效果
        ApplyFrozenEffect();

        // 回收子弹到对象池
        DestroyBullet();
    }

    /// <summary>
    /// 应用冰霜减速效果
    /// </summary>
    protected virtual void ApplyFrozenEffect()
    {
        // 播放冰霜范围特效
        if (frozenEffect != null)
        {
            GameObject effect = Instantiate(frozenEffect, transform.position, Quaternion.identity);
            // 设置特效大小与范围匹配
            effect.transform.localScale = Vector3.one * slowRadius * 2;
            // 自动销毁特效
            Destroy(effect, 1f);
        }

        // 检测范围内的所有敌人
        Collider[] colliders = Physics.OverlapSphere(transform.position, slowRadius);
        foreach (Collider hit in colliders)
        {
            if (IsTarget(hit))
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    // 应用减速效果
                    enemy.ApplySlow(slowPercentage, slowDuration);
                }
            }
        }
    }

    // 调试用：在Scene视图中显示减速范围
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slowRadius);
    }
}
    