using UnityEngine;

/// <summary>
/// 烈焰弹类，具有爆炸和群体伤害特性
/// </summary>
public class BulletExplosive : BulletBase
{
    [Header("爆炸属性")]
    [Tooltip("爆炸范围半径")]
    public float explosionRadius = 3f;
    
    [Tooltip("爆炸冲击力")]
    public float explosionForce = 1000f;
    
    [Tooltip("爆炸特效")]
    public GameObject explosionEffect;
    
    [Tooltip("是否对范围内所有目标造成全额伤害")]
    public bool fullDamageInRadius = true;

    /// <summary>
    /// 命中目标处理 - 触发爆炸
    /// </summary>
    protected override void OnHitTarget(Collider target)
    {
        Explode();
        DestroyBullet();
    }

    /// <summary>
    /// 爆炸逻辑
    /// </summary>
    protected virtual void Explode()
    {
        // 播放爆炸特效
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // 检测范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            if (IsTarget(hit))
            {
                // 计算伤害（可以根据距离调整伤害）
                int damageToApply = fullDamageInRadius ? damage : CalculateDistanceDamage(hit.transform.position);
                
                // 应用伤害
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damageToApply);
                }

                // 应用爆炸力
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }
        }
    }

    /// <summary>
    /// 根据距离计算伤害（越远伤害越低）
    /// </summary>
    protected virtual int CalculateDistanceDamage(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        float damageRatio = 1 - (distance / explosionRadius);
        return Mathf.RoundToInt(damage * damageRatio);
    }

    // 调试用：在Scene视图中显示爆炸范围
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
    