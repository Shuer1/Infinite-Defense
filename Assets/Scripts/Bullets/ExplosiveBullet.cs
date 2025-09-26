using UnityEngine;

/// <summary>
/// 爆炸子弹：命中后产生范围伤害
/// </summary>
public class ExplosiveBullet : Bullet
{
    [Header("爆炸特性")]
    public float explosionRadius = 2f; // 爆炸范围
    public int explosionDamage = 5; // 爆炸伤害（低于基础子弹）
    private int totalDamage;
    [Header("爆炸特效及音效")]
    public GameObject hitEffectPrefab; // 改为GameObject类型，适配对象池
    public AudioClip hitSound;

    protected override void OnTriggerEnter(Collider other)
    {
        // 从对象池获取并播放特效（替换Instantiate）
        if (hitEffectPrefab != null)
        {
            ParticleEffectPool.Instance.PlayEffect(hitEffectPrefab, transform.position, transform.rotation);
        }
        
        // 播放音效（保持不变）
        AudioSource.PlayClipAtPoint(hitSound, transform.position);
        base.OnTriggerEnter(other);
        
        // 检测范围内所有敌人（保持不变）
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                enemy?.TakeDamage(explosionDamage);
            }
        }

        // 回收子弹（保持不变
        gameObject.SetActive(false);
    }

    // 绘制Gizmos（保持不变）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}