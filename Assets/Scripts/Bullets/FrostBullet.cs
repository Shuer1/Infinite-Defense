using UnityEngine;

public class FrostBullet : Bullet
{
    [Header("冰冻特性")]
    public float frostRadius = 2.5f; // 减速范围
    public int frostDamage = 3; // 冰冻伤害（低于基础子弹）
    [Tooltip("减速百分比,eg:50表示50%,会被转换成0.5倍速度")]
    public float slowPercentage = 50f; // 减速百分比数值（50%）
    public float slowDuration = 2f; // 减速持续时间
    [Header("冰冻特效及音效")]
    public GameObject frostEffectPrefab; // 适配对象池
    public AudioClip hitSound;

    // 敌人专属层级索引（缓存避免重复计算）
    private int _enemyLayerIndex;


    void Awake()
    {
        // 初始化敌人层级索引（仅一次）
        _enemyLayerIndex = LayerMask.NameToLayer("Enemy");
    }


    protected override void OnTriggerEnter(Collider other)
    {
        // 仅处理敌人层级的碰撞（过滤所有非敌人对象）
        if (other.gameObject.layer != _enemyLayerIndex)
            return;

        // 计算总伤害（基础伤害+冰冻伤害，使用最新基础伤害值）
        int totalDamage = damage + frostDamage;

        // 播放冰冻特效（通过对象池，带空引用保护）
        if (frostEffectPrefab != null && ParticleEffectPool.Instance != null)
        {
            ParticleEffectPool.Instance.PlayEffect(frostEffectPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("Frost effect is null or ParticleEffectPool not initialized");
        }

        // 播放冰冻音效
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        // 处理第一个命中的敌人（直接碰撞的敌人）
        EnemyBase firstHitEnemy = other.GetComponent<EnemyBase>();
        if (firstHitEnemy != null && !firstHitEnemy.isDead)
        {
            firstHitEnemy.TakeDamage(totalDamage);
            firstHitEnemy.ApplySlow(slowPercentage, slowDuration);
        }

        // 检测范围内其他敌人（仅敌人层级，排除第一个命中的敌人）
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, frostRadius, 1 << _enemyLayerIndex);
        foreach (var collider in collidersInRange)
        {
            EnemyBase enemyInRange = collider.GetComponent<EnemyBase>();
            // 排除第一个命中的敌人，且只对存活敌人造成伤害和减速
            if (enemyInRange != null && enemyInRange != firstHitEnemy && !enemyInRange.isDead)
            {
                enemyInRange.TakeDamage(frostDamage);
                enemyInRange.ApplySlow(slowPercentage, slowDuration);
            }
        }

        // 回收子弹
        gameObject.SetActive(false);
    }

    // 绘制Gizmos（保持不变）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, frostRadius);
    }
}