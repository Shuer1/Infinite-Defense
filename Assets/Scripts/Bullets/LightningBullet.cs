using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class LightningBullet : Bullet
{
    [Header("闪电连锁属性（Inspector仅初始默认值）")]
    public int lightningCount = 3;          // 连锁次数
    public int lightningDamage = 40;        // 初始化伤害（首个目标）
    public float lightningRange = 2.5f;     // 连锁搜索范围
    public int chainDelayMs = 100;          // 每次连锁延迟(ms)
    [Range(0f, 1f)]
    public float damageDecayRate = 0.8f;    // 损耗系数

    [Header("特效")]
    public GameObject lightningEffectPrefab;
    public AudioClip hitSound;
    [Range(0,1.5f)] public float soundVolume = 1f;
    private AudioSource audioSource;

    private int enemyLayer;
    private int enemyLayerMask;

    private void Awake()
    {
        bulletType = BulletType.Lightning;
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyLayerMask = 1 << enemyLayer;

        audioSource = gameObject.AddComponent<AudioSource>();
          // 初始化AudioSource属性（你可以调大音量这里）
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;      // 3D 音效
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 30f;
        audioSource.volume = soundVolume;
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // 从DataManager读取 damage(基础伤害)

        // lightningDamage 和 lightningCount 从 DataManager 持久化读取
        lightningDamage = DataManager.GetInt(DataManager.LightningBulletDamageKey, lightningDamage);
        lightningCount = DataManager.GetInt(DataManager.LightningCountKey, lightningCount);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != enemyLayer) return;

        EnemyBase startEnemy = other.GetComponent<EnemyBase>();
        if (startEnemy != null && !startEnemy.isDead)
        {
            // 执行闪电连锁逻辑（异步，不阻塞主线程）
            HandleChainLightningAsync(startEnemy).Forget();
        }

        // 子弹打到第一个敌人后回收，不在场景中继续飞行
        ReturnToPool();
    }

    /// <summary>
    /// 链式闪电逻辑（异步执行）
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

            // 伤害
            current.TakeDamage(currentDamage);
            if (hitSound != null)
            {
                // AudioSource.PlayClipAtPoint(hitSound, current.transform.position);
                audioSource.transform.position = current.transform.position;
                audioSource.PlayOneShot(hitSound);
            }

            // 🔹 生成/复用电弧特效：使用通用池
            GameObject effect = ParticleEffectPool.Instance.GetEffect(lightningEffectPrefab);
            if (effect != null)
            {
                effect.SetActive(true);
                var le = effect.GetComponent<LightningEffect>();
                le.Init(
                    start: (i == 0 ? this.transform : hitEnemies[i - 1].transform),
                    end: current.transform,
                    prefab: lightningEffectPrefab,
                    duration: 0.25f
                );
            }

            // 递减伤害
            currentDamage = Mathf.Max(1, Mathf.RoundToInt(currentDamage * damageDecayRate));

            // 延迟链向下一个敌人
            await UniTask.Delay(chainDelayMs);

            // 查找下一目标
            current = FindNextEnemy(current, hitEnemies);
        }
    }

    /// <summary>
    /// 寻找下一个连锁目标
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

            float dist = (enemy.transform.position - from.transform.position).sqrMagnitude;
            if (dist < minDistSqr)
            {
                minDistSqr = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 生成闪电视觉效果
    /// </summary>
    private void SpawnLightningEffect(Vector3 start, Vector3 end)
    {
        if (!lightningEffectPrefab) return;

        GameObject effect = Instantiate(lightningEffectPrefab);
        LineRenderer lr = effect.GetComponent<LineRenderer>();
        if (lr != null)
        {
            int segments = 5;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 pos = Vector3.Lerp(start, end, t) + Random.insideUnitSphere * 0.15f;
                lr.SetPosition(i, pos);
            }
        }
        Destroy(effect, 0.25f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lightningRange);
    }
#endif
}
