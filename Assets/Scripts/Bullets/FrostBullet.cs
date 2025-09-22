using UnityEngine;

/// <summary>
/// 冰冻子弹：命中后产生范围减速效果
/// </summary>
public class FrostBullet : Bullet
{
    [Header("冰冻特性")]
    public float frostRadius = 2.5f; // 减速范围
    public int frostDamage = 3; // 冰冻伤害（低于基础子弹）
    [Tooltip("减速百分比,eg:50表示50%,会被转换成0.5倍速度")]
    public float slowPercentage = 50f; // 减速百分比数值（50%）
    public float slowDuration = 2f; // 减速持续时间
    [Header("冰冻特效及音效")]
    public ParticleSystem hitEffect;
    public AudioClip hitSound;

    protected override void OnTriggerEnter(Collider other)
    {
        // 播放特效
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation);
        }
        // 播放音效（需AudioSource组件）
        AudioSource.PlayClipAtPoint(hitSound, transform.position);

        base.OnTriggerEnter(other);
        // 检测范围内所有敌人
        Collider[] colliders = Physics.OverlapSphere(transform.position, frostRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                enemy?.TakeDamage(frostDamage);
                enemy?.ApplySlow(slowPercentage, slowDuration);
            }
        }

        // 回收子弹
        gameObject.SetActive(false);
    }

    // 绘制Gizmos方便调试减速范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, frostRadius);
    }
}