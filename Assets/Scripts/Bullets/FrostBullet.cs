using UnityEngine;

public class FrostBullet : Bullet
{
    [Header("冰冻特性")]
    public float frostRadius = 2.5f; // 减速范围
    public int frostDamage = 3; // 冰冻伤害（低于基础子弹）
    private int totalDamage;
    [Tooltip("减速百分比,eg:50表示50%,会被转换成0.5倍速度")]
    public float slowPercentage = 50f; // 减速百分比数值（50%）
    public float slowDuration = 2f; // 减速持续时间
    [Header("冰冻特效及音效")]
    public GameObject frostEffectPrefab; // 修复类型为GameObject，适配对象池
    public AudioClip hitSound;

    protected override void OnTriggerEnter(Collider other)
    {
        // 从对象池获取并播放特效（替换Instantiate）
        if (frostEffectPrefab != null)
        {
            Debug.Log("Shoot Frost Bullet");
            ParticleEffectPool.Instance.PlayEffect(frostEffectPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("Frost effect is null");
        }
        // 播放音效（保持不变）
        AudioSource.PlayClipAtPoint(hitSound, transform.position);

        base.OnTriggerEnter(other);
        // 检测范围内所有敌人（保持不变）
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

        // 回收子弹（保持不变）
        gameObject.SetActive(false);
    }

    // 绘制Gizmos（保持不变）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, frostRadius);
    }
}