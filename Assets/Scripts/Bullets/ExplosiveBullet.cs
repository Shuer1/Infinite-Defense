using UnityEngine;

/// <summary>
/// 爆炸子弹：平行射出，命中敌人后触发范围伤害，第一个命中敌人受总伤害，其余受爆炸伤害
/// 依赖基类超时回收逻辑，无需额外处理超时
/// </summary>
public class ExplosiveBullet : Bullet
{
    [Header("爆炸特性")]
    [Tooltip("爆炸范围半径")]
    public float explosionRadius = 2f;
    [Tooltip("范围爆炸伤害（非直接命中时）")]
    public int explosionDamage = 5;

    [Header("爆炸特效及音效")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;

    // 敌人专属层级索引（缓存避免重复计算）
    private int _enemyLayerIndex;


    void Awake()
    {
        // 初始化敌人层级索引（仅一次）
        _enemyLayerIndex = LayerMask.NameToLayer("Enemy");
    }

    void Start()
    {
        SyncExplosiveBulletData();
    }


    /// <summary>
    /// 触发碰撞时处理（仅响应敌人）
    /// </summary>
    protected override void OnTriggerEnter(Collider other)
    {
        // 仅处理敌人层级的碰撞（过滤所有非敌人对象）
        if (other.gameObject.layer != _enemyLayerIndex)
            return;

        // 计算总伤害（基础伤害+爆炸伤害，使用最新基础伤害值）
        int totalDamage = damage + explosionDamage;

        // 播放爆炸特效（通过对象池，带空引用保护）
        if (hitEffectPrefab != null && ParticleEffectPool.Instance != null)
        {
            ParticleEffectPool.Instance.PlayEffect(hitEffectPrefab, transform.position, transform.rotation);
        }

        // 播放爆炸音效
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // 处理第一个命中的敌人（直接碰撞的敌人）
        EnemyBase firstHitEnemy = other.GetComponent<EnemyBase>();
        if (firstHitEnemy != null && !firstHitEnemy.isDead)
        {
            firstHitEnemy.TakeDamage(totalDamage);
        }

        // 检测范围内其他敌人（仅敌人层级，排除第一个命中的敌人）
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, explosionRadius, 1 << _enemyLayerIndex);
        foreach (var collider in collidersInRange)
        {
            EnemyBase enemyInRange = collider.GetComponent<EnemyBase>();
            // 排除第一个命中的敌人，且只对存活敌人造成伤害
            if (enemyInRange != null && enemyInRange != firstHitEnemy && !enemyInRange.isDead)
            {
                enemyInRange.TakeDamage(explosionDamage);
            }
        }

        // 触发回收（依赖基类超时回收，此处仅提前隐藏子弹）
        gameObject.SetActive(false);
    }


    /// <summary>
    /// 场景视图绘制爆炸范围 gizmo，方便调试
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 橙色半透明
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void SyncExplosiveBulletData()
    {
        DataManager.GetInt(DataManager.ExplosiveDamageKey);
        DataManager.GetFloat(DataManager.ExplosionRangeKey);
    }
}