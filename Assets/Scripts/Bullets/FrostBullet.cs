using UnityEngine;

public class FrostBullet : Bullet
{
    [Header("冰冻子弹数据（Inspector仅初始默认值）")]
    public float frostRadius = 1.0f;         // 范围半径
    public int extraFrostDamage = 3;         // 额外伤害
    public float slowPercentage = 50f;       // 减速百分比
    public float slowDuration = 0.5f;        // 减速持续时间

    [Header("特效/音效")]
    public GameObject frostEffectPrefab;
    public AudioClip hitSound;
    [Range(0,1.5f)] public float soundVolume = 1f;

    private int enemyLayer;
    private int enemyLayerMask;

    // 设置类型 & LayerMask
    private void Awake()
    {
        bulletType = BulletType.Frost;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyLayerMask = 1 << enemyLayer;
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // 从DataManager读取 damage 基础伤害

        // 冰冻额外伤害、时间从 DataManager 读取（防止Inspector覆盖存档数据）
        extraFrostDamage = DataManager.GetInt(DataManager.FrostDamageKey, extraFrostDamage);
        slowDuration     = DataManager.GetFloat(DataManager.FrostFreezeDurationKey, slowDuration);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer) return;

        int totalDamage = damage + extraFrostDamage;

        // 播放特效
        if (frostEffectPrefab && ParticleEffectPool.Instance != null)
        {
            ParticleEffectPool.Instance.PlayEffect(frostEffectPrefab, transform.position, Quaternion.identity);
        }

        // 播放音效
        if (hitSound != null)
        {
            SoundManager.Instance.PlaySFX(hitSound, soundVolume);
        }

        // 对首个敌人造成伤害+减速
        EnemyBase firstEnemy = other.GetComponent<EnemyBase>();
        if (firstEnemy && !firstEnemy.isDead)
        {
            firstEnemy.TakeDamage(totalDamage);
            firstEnemy.ApplySlow(slowPercentage, slowDuration);
        }

        // 对范围内其它敌人伤害+减速
        Collider[] hits = Physics.OverlapSphere(transform.position, frostRadius, enemyLayerMask);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy && !enemy.isDead && enemy != firstEnemy)
            {
                enemy.TakeDamage(extraFrostDamage);
                enemy.ApplySlow(slowPercentage, slowDuration);
            }
        }

        ReturnToPool();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, frostRadius);
    }
#endif
}
