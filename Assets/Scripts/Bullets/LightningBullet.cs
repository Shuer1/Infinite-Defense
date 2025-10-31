using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class LightningBullet : Bullet
{
    [Header("闪电特性")]
    public int lightningCount = 3;
    public int lightningDamage = 40;
    public float lightningRange = 6f;
    public float chainDelay = 100f; // 单位：毫秒（UniTask 用 ms）
    [Range(0f, 1f)] public float damageDecayRate = 0.8f;

    [Header("闪电特效及音效")]
    public GameObject lightningEffectPrefab;
    public AudioClip hitSound;

    private int _enemyLayerMask;

    void Awake()
    {
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void Start()
    {
        SyncLightningBulletData();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        EnemyBase mainTarget = other.GetComponent<EnemyBase>();
        if (mainTarget == null) return;

        // ✅ 使用 UniTask 异步处理闪电链
        HandleChainLightningAsync(mainTarget).Forget();

        // 击中后隐藏自身
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 异步链式闪电逻辑（UniTask优化版）
    /// </summary>
    private async UniTaskVoid HandleChainLightningAsync(EnemyBase startTarget)
    {
        List<EnemyBase> hitList = new List<EnemyBase>();
        EnemyBase currentTarget = startTarget;
        int currentDamage = lightningDamage;

        for (int i = 0; i <= lightningCount && currentTarget != null; i++)
        {
            currentTarget.TakeDamage(currentDamage);
            hitList.Add(currentTarget);

            if (hitSound)
                AudioSource.PlayClipAtPoint(hitSound, currentTarget.transform.position);

            Vector3 startPos = (i == 0) ? transform.position : hitList[i - 1].transform.position;
            SpawnLightningEffect(startPos, currentTarget.transform.position);

            // ✅ 使用异步延迟而非 Coroutine 等待
            await UniTask.Delay((int)chainDelay);

            // ✅ 查找下一个目标
            EnemyBase nextTarget = FindNextTarget(currentTarget, hitList);
            currentTarget = nextTarget;
            currentDamage = Mathf.RoundToInt(currentDamage * damageDecayRate);
        }
    }

    private EnemyBase FindNextTarget(EnemyBase fromTarget, List<EnemyBase> excludeList)
    {
        Collider[] hits = Physics.OverlapSphere(fromTarget.transform.position, lightningRange, _enemyLayerMask);
        EnemyBase next = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && !excludeList.Contains(enemy))
            {
                float dist = Vector3.Distance(fromTarget.transform.position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    next = enemy;
                }
            }
        }

        return next;
    }

    private void SpawnLightningEffect(Vector3 start, Vector3 end)
    {
        if (lightningEffectPrefab == null) return;

        GameObject effect = Instantiate(lightningEffectPrefab);
        LineRenderer lr = effect.GetComponent<LineRenderer>();
        if (lr != null)
        {
            int segments = 6;
            lr.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point += Random.insideUnitSphere * 0.15f;
                lr.SetPosition(i, point);
            }
        }

        Destroy(effect, 0.25f);
    }

    void SyncLightningBulletData()
    {
        lightningCount = DataManager.GetInt(DataManager.LightningCountKey);
        lightningDamage = DataManager.GetInt(DataManager.LightningBulletDamageKey);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lightningRange);
    }
#endif
}
