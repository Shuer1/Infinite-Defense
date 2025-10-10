using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Tooltip("升级面板的缩放动画组件")]
    [SerializeField] private PanelScaleAnimation upgradePanelScaleAnim;

    [Tooltip("所有可用的升级选项")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>(); // 引用外部定义的UpgradeData

    [Tooltip("拖入玩家控制器实例")]
    public PlayerController playerController;

    [Tooltip("拖入子弹预制体")]
    public Bullet bulletPrefab;
    public ExplosiveBullet explosiveBulletPrefab;
    public FrostBullet frostBulletPrefab;

    // 升级特效、音效
    [SerializeField] private ParticleSystem upgradeEffect;
    private ParticleSystem.MainModule mainModule;
    [SerializeField] private AudioSource upgradeAudio;

    // 常量定义
    private const float FIRE_RATE_REDUCTION_MULTIPLIER = 0.01f;
    private const float SLOW_DURATION_MULTIPLIER = 0.5f;
    private const float EXPLOSION_RANGE_MULTIPLIER = 0.2f;
    private const float MIN_FIRE_RATE = 0.1f;

    public enum PoolType
    {
        BulletPool,
        ExplosiveBulletPool,
        FrostBulletPool
    }

    public enum BulletType
    {
        Bullet,
        ExplosiveBullet,
        FrostBullet
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"重复的UpgradeManager实例，已销毁：{gameObject.name}", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("未找到PlayerController，请手动赋值", this);
            }
        }

        if (upgradeEffect == null)
        {
            Debug.LogError("upgradeEffect未赋值", this);
            return;
        }
        mainModule = upgradeEffect.main;
        upgradeEffect.gameObject.SetActive(false);
    }

    void Update()
    {
        if (upgradeEffect.isPlaying && upgradeEffect.time >= mainModule.duration - 0.01f)
        {
            upgradeEffect.Stop();
            upgradeEffect.gameObject.SetActive(false);
        }
    }

    public void ShowUpgradeOptions()
    {
        if (allUpgrades.Count < 3)
        {
            Debug.LogError($"升级选项不足3个(当前：{allUpgrades.Count})", this);
            return;
        }

        var randomUpgrades = GetRandomUniqueUpgrades(3);

        if (UpgradePanel.Instance == null)
        {
            Debug.LogError("UpgradePanel实例不存在", this);
            return;
        }

        if (upgradePanelScaleAnim != null)
        {
            upgradePanelScaleAnim.OpenPanel();
        }
        else
        {
            Debug.LogError("upgradePanelScaleAnim未赋值", this);
        }

        UpgradePanel.Instance.Show(randomUpgrades);
    }

    private List<UpgradeData> GetRandomUniqueUpgrades(int count)
    {
        List<UpgradeData> shuffled = new List<UpgradeData>(allUpgrades);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled.Take(count).ToList();
    }

    public void ApplySelectedUpgrade(string upgradeId)
    {
        var upgrade = allUpgrades.FirstOrDefault(u => u.upgradeId == upgradeId);
        if (upgrade == null)
        {
            Debug.LogError($"未找到升级：{upgradeId}", this);
            return;
        }

        ApplyUpgradeByType(upgrade.type, upgrade.value);
        Debug.Log($"应用升级：{upgrade.displayName}（ID：{upgradeId}）");
    }

    private void ApplyUpgradeByType(UpgradeType type, int value) // 引用外部定义的UpgradeType
    {
        switch (type)
        {
            case UpgradeType.Attack:
                ApplyAttackUpgrade(value);
                break;
            case UpgradeType.FireRate:
                ApplyFireRateUpgrade(value);
                break;
            case UpgradeType.MaxHealth:
                ApplyMaxHealthUpgrade(value);
                break;
            case UpgradeType.AddChanceForExplosive:
                GetOrAddSpecialBullet(SpecialBulletType.Explosive, value);
                break;
            case UpgradeType.AddChanceForFrost:
                GetOrAddSpecialBullet(SpecialBulletType.Frost, value);
                break;
            case UpgradeType.ExploseRange:
                ApplyBulletRangeUpgrade(value);
                break;
            case UpgradeType.SlowTime:
                ApplySlowTimeLongerUpgrade(value);
                break;
            default:
                Debug.LogWarning($"未处理的升级类型：{type}", this);
                break;
        }

        if (upgradeEffect != null)
        {
            upgradeEffect.gameObject.SetActive(true);
            upgradeEffect.Play();
        }
        if (upgradeAudio != null && !upgradeAudio.isPlaying)
        {
            upgradeAudio.Play();
        }
    }

    private enum SpecialBulletType
    {
        Explosive,
        Frost
    }

    private void ApplyAttackUpgrade(int value) //提升普通子弹伤害 实现保存✅
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("bulletPrefab未赋值", this);
            return;
        }

        bulletPrefab.damage += value;
        UpdateAllPooledBulletsDamage(BulletType.Bullet, PoolType.BulletPool, bulletPrefab.damage);
        UIManager.Instance?.ShowAndUpdatePlayerAttack(bulletPrefab.damage);

        DataManager.SaveInt(DataManager.BaseBulletDamageKey,bulletPrefab.damage);
        Debug.Log($"普通子弹伤害提升至：{bulletPrefab.damage}");
    }

    private void ApplyFireRateUpgrade(int value) //提升射速 实现保存✅
    {
        if (playerController == null)
        {
            Debug.LogError("playerController为null", this);
            return;
        }

        float reduction = value * FIRE_RATE_REDUCTION_MULTIPLIER;
        playerController.fireRate = Mathf.Max(MIN_FIRE_RATE, playerController.fireRate - reduction);

        DataManager.SaveFloat(DataManager.PlayerShootSpeedKey,playerController.fireRate,isGreater:false);
        Debug.Log($"射速提升，当前间隔：{playerController.fireRate:F2}秒");
    }

    private void ApplyMaxHealthUpgrade(int value) //提升最大生命值 实现保存✅
    {
        if (playerController == null)
        {
            Debug.LogError("playerController为null", this);
            return;
        }

        playerController.health += value;
        playerController.currentHealth = Mathf.Min(playerController.currentHealth + value, playerController.health);
        UIManager.Instance?.UpdateAndShowPlayerHP(playerController.currentHealth, playerController.health);

        DataManager.SaveInt(DataManager.PlayerMaxHealthKey,playerController.health);
        Debug.Log($"最大生命值提升至：{playerController.health}");
    }

    private void GetOrAddSpecialBullet(SpecialBulletType type, int extraDamageValue) //获得特殊子弹/增加伤害 实现保存✅
    {
        switch (type)
        {
            case SpecialBulletType.Explosive:
                if (explosiveBulletPrefab == null)
                {
                    Debug.LogError("explosiveBulletPrefab未赋值", this);
                    return;
                }

                explosiveBulletPrefab.explosionDamage += extraDamageValue;
                playerController.explosiveBulletChance += 2;
                UpdateAllPooledBulletsDamage(BulletType.ExplosiveBullet, PoolType.ExplosiveBulletPool, explosiveBulletPrefab.explosionDamage);

                DataManager.SaveInt(DataManager.ExplosiveDamageKey, explosiveBulletPrefab.explosionDamage);
                DataManager.SaveInt(DataManager.ExplosiveBulletChanceKey,playerController.explosiveBulletChance);
                Debug.Log($"爆炸子弹伤害提升至：{explosiveBulletPrefab.explosionDamage}");
                break;
            case SpecialBulletType.Frost:
                if (frostBulletPrefab == null)
                {
                    Debug.LogError("frostBulletPrefab未赋值", this);
                    return;
                }

                frostBulletPrefab.frostDamage += extraDamageValue;
                playerController.normalBulletChance -= 1;
                UpdateAllPooledBulletsDamage(BulletType.FrostBullet, PoolType.FrostBulletPool, frostBulletPrefab.damage);
                
                DataManager.SaveInt(DataManager.FrostDamageKey, frostBulletPrefab.frostDamage);
                DataManager.SaveInt(DataManager.NormalBulletChanceKey, playerController.normalBulletChance, isGreater:false);
                Debug.Log($"冰冻子弹伤害提升至：{frostBulletPrefab.damage}");
                break;
        }
    }

    private void ApplySlowTimeLongerUpgrade(int value) //提升减速效果 实现保存✅
    {
        if (frostBulletPrefab == null)
        {
            Debug.LogError("frostBulletPrefab未赋值", this);
            return;
        }

        float addDuration = value * SLOW_DURATION_MULTIPLIER;
        frostBulletPrefab.slowDuration += addDuration;
        UpdateAllPooledFrostSlowDuration(frostBulletPrefab.slowDuration);

        DataManager.SaveFloat(DataManager.FrostFreezeDurationKey,frostBulletPrefab.slowDuration);
        Debug.Log($"冰冻减速时长提升至：{frostBulletPrefab.slowDuration:F1}秒");
    }

    private void ApplyBulletRangeUpgrade(int value) //提升爆炸范围 实现保存✅
    {
        if (explosiveBulletPrefab == null)
        {
            Debug.LogError("explosiveBulletPrefab未赋值", this);
            return;
        }

        float addRange = value * EXPLOSION_RANGE_MULTIPLIER;
        explosiveBulletPrefab.explosionRadius += addRange;
        UpdateAllPooledExplosionRange(explosiveBulletPrefab.explosionRadius);

        DataManager.SaveFloat(DataManager.ExplosionRangeKey,explosiveBulletPrefab.explosionRadius);
        Debug.Log($"爆炸范围提升至：{explosiveBulletPrefab.explosionRadius:F1}米");
    }

    private void UpdateAllPooledBulletsDamage(BulletType bulletType, PoolType poolType, int newDamage) //更新子弹伤害提升后的对象池
    {
        var bulletList = GetBulletListByPoolType(poolType);
        if (bulletList == null)
        {
            Debug.LogWarning($"{poolType} 实例不存在", this);
            return;
        }

        int updatedCount = 0;
        foreach (var bulletObj in bulletList)
        {
            if (bulletObj == null)
                continue;

            switch (bulletType)
            {
                case BulletType.Bullet:
                    var bullet = bulletObj.GetComponent<Bullet>();
                    if (bullet != null)
                    {
                        bullet.damage = newDamage;
                        updatedCount++;
                    }
                    break;
                case BulletType.ExplosiveBullet:
                    var explosive = bulletObj.GetComponent<ExplosiveBullet>();
                    if (explosive != null)
                    {
                        explosive.explosionDamage = newDamage;
                        updatedCount++;
                    }
                    break;
                case BulletType.FrostBullet:
                    var frost = bulletObj.GetComponent<FrostBullet>();
                    if (frost != null)
                    {
                        frost.frostDamage = newDamage;
                        updatedCount++;
                    }
                    break;
            }
        }

        Debug.Log($"同步 {poolType} 中的所有子弹伤害，新值：{newDamage}", this);
    }

    private void UpdateAllPooledFrostSlowDuration(float newDuration) //更新减速时长提升后的对象池
    {
        var bulletList = GetBulletListByPoolType(PoolType.FrostBulletPool);
        if (bulletList == null) return;

        int updatedCount = 0;
        foreach (var bulletObj in bulletList)
        {
            if (bulletObj == null) continue;
            var frost = bulletObj.GetComponent<FrostBullet>();
            if (frost != null)
            {
                frost.slowDuration = newDuration;
                updatedCount++;
            }
        }
        Debug.Log($"同步冰冻子弹池 {updatedCount} 个减速时长，新值：{newDuration:F1}秒", this);
    }

    private void UpdateAllPooledExplosionRange(float newRange)  //更新爆炸范围提升后的对象池
    {
        var bulletList = GetBulletListByPoolType(PoolType.ExplosiveBulletPool);
        if (bulletList == null) return;

        int updatedCount = 0;
        foreach (var bulletObj in bulletList)
        {
            if (bulletObj == null) continue;
            var explosive = bulletObj.GetComponent<ExplosiveBullet>();
            if (explosive != null)
            {
                explosive.explosionRadius = newRange;
                updatedCount++;
            }
        }
        Debug.Log($"同步爆炸子弹池 {updatedCount} 个范围，新值：{newRange:F1}米", this);
    }

    private IEnumerable<GameObject> GetBulletListByPoolType(PoolType poolType) //通过池类型判断获取到子弹列表
    {
        return poolType switch
        {
            PoolType.BulletPool => BulletPool.Instance?.GetAllBullets(),
            PoolType.ExplosiveBulletPool => ExplosiveBulletPool.Instance?.GetAllBullets(),
            PoolType.FrostBulletPool => FrostBulletPool.Instance?.GetAllBullets(),
            _ => null
        };
    }
}