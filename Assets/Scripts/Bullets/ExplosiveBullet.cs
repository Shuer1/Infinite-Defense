using UnityEngine;

public class ExplosiveBullet : Bullet
{
    [Header("爆炸特性")]
    public float explosionRadius = 2f;     // 仅作初始默认值，实际运行从 DataManager 读取
    public int explosionDamage = 5;

    [Header("特效/音效")]
    public GameObject hitEffectPrefab;
    public AudioSource hitSound;
    [Range(0,1.5f)] public float soundVolume = 1f;

    private int enemyLayer;
    private int enemyLayerMask;

    protected void Awake()
    {
        bulletType = BulletType.Explosive;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyLayerMask = 1 << enemyLayer;
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // 继承基础逻辑，读取 damage
        // 再额外读取爆炸特化的数据（从 DataManager）
        explosionDamage = DataManager.GetInt(DataManager.ExplosiveDamageKey, explosionDamage);
        explosionRadius = DataManager.GetFloat(DataManager.ExplosionRangeKey, explosionRadius);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer) 
            return;

        int totalDamage = damage + explosionDamage; // 直击伤害 + 额外爆炸伤害

        // 播放特效
        if (hitEffectPrefab && ParticleEffectPool.Instance != null)
        {
            ParticleEffectPool.Instance.PlayEffect(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (hitSound != null && hitSound.clip != null)
        {
            AudioSource.PlayClipAtPoint(hitSound.clip, transform.position, soundVolume);
        }

        // 首个直接命中的敌人
        EnemyBase firstEnemy = other.GetComponent<EnemyBase>();
        if (firstEnemy != null && !firstEnemy.isDead)
        {
            firstEnemy.TakeDamage(totalDamage);
        }

        // 周围范围敌人伤害
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayerMask);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy && enemy != firstEnemy && !enemy.isDead)
            {
                enemy.TakeDamage(explosionDamage);
            }
        }

        ReturnToPool();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
