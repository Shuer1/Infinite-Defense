using UnityEngine;

/// <summary>
/// 爆炸子弹：命中后产生范围伤害
/// </summary>
public class ExplosiveBullet : Bullet
{
    [Header("爆炸特性")]
    public float explosionRadius = 2f; // 爆炸范围
    public int explosionDamage = 5; // 爆炸伤害（低于基础子弹）
    [Header("爆炸特效及音效")]
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
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                enemy?.TakeDamage(explosionDamage);
            }
        }

        // 回收子弹
        gameObject.SetActive(false);
    }

    // 绘制Gizmos方便调试爆炸范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}