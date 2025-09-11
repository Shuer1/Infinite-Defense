using UnityEngine;

/// <summary>
/// 基础子弹类，单体伤害
/// </summary>
public class BulletBasic : BulletBase
{
    [Header("基础子弹特效")]
    [Tooltip("命中特效")]
    public GameObject hitEffect;

    /// <summary>
    /// 命中目标处理 - 单体伤害
    /// </summary>
    protected override void OnHitTarget(Collider target)
    {
        // 播放命中特效
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.LookRotation(direction));
        }

        // 对目标造成伤害
        EnemyBase enemy = target.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        // 回收子弹到对象池
        DestroyBullet();
    }
}
    