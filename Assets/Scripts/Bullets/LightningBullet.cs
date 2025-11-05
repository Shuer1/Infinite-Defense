using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class LightningBullet : Bullet
{
    [Header("闪电连锁属性")]
    public int lightningCount = 3;
    private const int maxLightningCount = 5;   // 最大连锁次数
    public int lightningDamage = 40;           // 初始伤害（第一个目标）
    public float lightningRange = 2.5f;        // 连锁范围
    public int chainDelayMs = 100;             // 每次连锁延迟(ms)
    [Range(0f, 1f)] public float damageDecayRate = 0.8f; // 每次衰减

    [Header("特效/音效")]
    public GameObject lightningEffectPrefab;   // 粒子闪电特效
    [Range(0,1.5f)] public float soundVolume = 1f;
    public AudioClip lightningSound;           // 闪电音效（全局播放）

    private int enemyLayer;
    private int enemyLayerMask;

    private void Awake()
    {
        bulletType = BulletType.Lightning;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyLayerMask = 1 << enemyLayer;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // 从 DataManager 获取动态参数
        lightningDamage = DataManager.GetInt(DataManager.LightningBulletDamageKey, lightningDamage);
        lightningCount = Mathf.Min(DataManager.GetInt(DataManager.LightningCountKey, lightningCount), maxLightningCount); // 限制最大5
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer) return;

        EnemyBase startEnemy = other.GetComponent<EnemyBase>();
        if (startEnemy != null && !startEnemy.isDead)
        {
            HandleChainLightningAsync(startEnemy).Forget();
        }

        // 命中第一个敌人后立即回收子弹
        ReturnToPool();
    }

    /// <summary>
    /// 异步链式闪电逻辑
    /// </summary>
    private async UniTaskVoid HandleChainLightningAsync(EnemyBase startEnemy)
    {
        List<EnemyBase> hitEnemies = new List<EnemyBase>();
        EnemyBase current = startEnemy;
        int currentDamage = lightningDamage;

        for (int i = 0; i < lightningCount; i++)
        {
            if (current == null || current.isDead) break;
            hitEnemies.Add(current);

            // 造成伤害
            current.TakeDamage(currentDamage);

            // 播放闪电粒子特效
            if (lightningEffectPrefab && ParticleEffectPool.Instance != null)
            {
                var effect = ParticleEffectPool.Instance.GetEffect(lightningEffectPrefab);
                if (effect)
                {
                    // 把实例放到正确位置
                    effect.transform.position = current.transform.position;
                    effect.transform.rotation = Quaternion.identity;

                    // 如果有 LightningEffect 脚本，用 Init 设置 prefabKey 与时长并启动播放
                    var le = effect.GetComponent<LightningEffect>();
                    if (le != null)
                    {
                        // 传入当前已命中的目标链（你也可以传 null）
                        le.Init(new List<Transform> { current.transform }, lightningEffectPrefab, 0.2f);
                    }
                    else
                    {
                        // 回退：没有脚本的话也激活对象并确保池能回收（不太推荐）
                        effect.SetActive(true);
                    }
                }
            }

            // 播放闪电音效（全局）
            if (lightningSound != null)
            {
                SoundManager.Instance?.PlaySFX(lightningSound, soundVolume);
            }

            // 等待连锁延迟
            await UniTask.Delay(chainDelayMs);

            // 查找下一个敌人
            EnemyBase next = FindNextEnemy(current, hitEnemies);
            if (next == null) break;

            // 递减伤害
            currentDamage = Mathf.Max(1, Mathf.RoundToInt(currentDamage * damageDecayRate));

            // 为下一次循环做准备
            current = next;
        }
    }

    /// <summary>
    /// 查找下一个最近敌人
    /// </summary>
    private EnemyBase FindNextEnemy(EnemyBase from, List<EnemyBase> excludeList)
    {
        Collider[] hits = Physics.OverlapSphere(from.transform.position, lightningRange, enemyLayerMask);
        EnemyBase nearest = null;
        float minDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null || excludeList.Contains(enemy) || enemy.isDead) continue;

            float distSqr = (enemy.transform.position - from.transform.position).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearest = enemy;
            }
        }
        return nearest;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightningRange);
    }
#endif
}
