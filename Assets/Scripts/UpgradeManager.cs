using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Tooltip("升级面板的缩放动画组件")]
    [SerializeField] private PanelScaleAnimation upgradePanelScaleAnim;

    [Tooltip("所有可用的升级选项")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>();

    [Tooltip("拖入玩家控制器实例")]
    public PlayerController playerController;

    [Tooltip("拖入子弹预制体")]
    public Bullet bulletPrefab;
    public ExplosiveBullet explosiveBulletPrefab;
    public FrostBullet frostBulletPrefab;
    //升级特效、音效
    [SerializeField] private ParticleSystem upgradeEffect;
    private ParticleSystem.MainModule mainModule;
    [SerializeField] private AudioSource upgradeAudio;

    public enum PoolType
    {
        BulletPool,          // 普通子弹池
        ExplosiveBulletPool, // 爆炸子弹池
        FrostBulletPool      // 冰冻子弹池
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
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (upgradeEffect == null)
        {
            Debug.LogError("upgradeEffct is null");
            return;
        }

        mainModule = upgradeEffect.main;

    }

    void Update()
    {
        //处理升级特效
        if (!upgradeEffect.isPlaying && upgradeEffect.time >= mainModule.duration - 0.01f)
        {
            upgradeEffect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示升级选项（随机选择3个）
    /// </summary>
    public void ShowUpgradeOptions()
    {
        if (allUpgrades.Count < 3)
        {
            Debug.LogError("升级选项不足3个,请检查配置");
            return;
        }

        // 随机选择3个不重复的升级选项
        var randomUpgrades = GetRandomUniqueUpgrades(3);

        // 显示升级面板和缩放动画
        if (UpgradePanel.Instance != null)
        {
            if (upgradePanelScaleAnim != null)
            {
                upgradePanelScaleAnim.OpenPanel();
            }
            else
            {
                Debug.LogError("为赋值upgradePanelScaleAnim");
            }

            //显示升级选项内容
            UpgradePanel.Instance.Show(randomUpgrades);
        }
        else
        {
            Debug.LogError("升级面板实例不存在");
        }
    }

    /// <summary>
    /// 随机获取指定数量的不重复升级选项
    /// </summary>
    private List<UpgradeData> GetRandomUniqueUpgrades(int count)
    {
        // 打乱顺序并取前count个
        return allUpgrades
            .OrderBy(x => Random.Range(0, allUpgrades.Count))
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 应用玩家选择的升级
    /// </summary>
    public void ApplySelectedUpgrade(string upgradeId)
    {
        var upgrade = allUpgrades.FirstOrDefault(u => u.upgradeId == upgradeId);
        if (upgrade == null)
        {
            Debug.LogError($"找不到升级选项: {upgradeId}");
            return;
        }

        ApplyUpgradeEffect(upgradeId, upgrade.value);
        Debug.Log($"已选择升级: {upgrade.displayName}");
    }

    /// <summary>
    /// 应用升级效果
    /// </summary>
    private void ApplyUpgradeEffect(string upgradeId, int value)
    {
        switch (upgradeId)
        {
            case "Attack":                  //普通子弹攻击力
                ApplyAttackUpgrade(value);
                break;
            case "FireRate":                //玩家射速
                ApplyFireRateUpgrade(value);
                break;
            case "MaxHealth":               //玩家最大血量
                ApplyMaxHealthUpgrade(value);
                break;
            case "AddChanceForExplosive":   //增加烈焰弹发射几率
                GetOrAddSpecialBullet(value);
                break;
            case "AddChanceForFrost":       //增加冰霜弹发射几率
                GetOrAddSpecialBullet(value);
                break;
            case "ExploseRange":            //提升烈焰弹伤害范围
                ApplyBulletRangeUpgrade(value);
                break;
            case "SlowTime":                //提升冰霜弹减速时长
                ApplySlowTimeLongerUpgrade(value);
                break;
            default:
                Debug.LogWarning($"未知的升级类型: {upgradeId}");
                break;
        }
        upgradeEffect.gameObject.SetActive(true); //显示升级特效
        upgradeAudio.Play();
    }

    // 各种升级效果的具体实现
    private void ApplyAttackUpgrade(int value) //1、子弹伤害增加
    {
        if (bulletPrefab == null) return;

        bulletPrefab.damage += value;
        UpdateAllPooledBulletsDamage(BulletType.Bullet,PoolType.BulletPool,bulletPrefab.damage);
        UIManager.Instance.ShowAndUpdatePlayerAttack(bulletPrefab.damage); //更新显示UI
        Debug.Log($"子弹攻击力提升! 新攻击力: {bulletPrefab.damage}");
    }

    private void ApplyFireRateUpgrade(int value) //2、射速增加
    {
        if (playerController == null) return;

        float fireRateReduction = value * 0.01f; //转换为攻击速度减少值
        playerController.fireRate = Mathf.Max(0.1f, playerController.fireRate - fireRateReduction);
        Debug.Log($"攻击速度提升! 当前攻击速度: {playerController.fireRate:F2}");
    }

    private void ApplyMaxHealthUpgrade(int value) //3、提升最大生命值
    {
        if (playerController == null) return;

        playerController.health += value;
        playerController.currentHealth = playerController.health;
        UIManager.Instance.UpdateAndShowPlayerHP(playerController.currentHealth, playerController.health);
        Debug.Log($"最大生命值提升! 当前生命值: {playerController.health}");
    }

    private void GetOrAddSpecialBullet(int value) //4and5、获得特殊子弹OR增加特殊子弹发射几率
    {
        if (explosiveBulletPrefab == null || frostBulletPrefab == null) return;

        switch (value)
        {
            case 1:
                Debug.Log("Develop Explosive Bullet!");
                explosiveBulletPrefab.explosionDamage += 5;
                // 同步更新对象池中的爆炸子弹伤害
                UpdateAllPooledBulletsDamage(BulletType.ExplosiveBullet, PoolType.ExplosiveBulletPool, explosiveBulletPrefab.explosionDamage);
                break;
            case 2:
                Debug.Log("Develop Frost Bullet!");
                frostBulletPrefab.damage += 5;
                // 同步更新对象池中的冰冻子弹伤害
                UpdateAllPooledBulletsDamage(BulletType.FrostBullet, PoolType.FrostBulletPool, frostBulletPrefab.damage);
                break;
        }
    }

    private void ApplySlowTimeLongerUpgrade(int value)
    {
        // 实现冰冻时间延长
        Debug.Log("The freezing time of frost bullet increases");
    }

    private void ApplyBulletRangeUpgrade(int value)
    {
        // 实现烈焰弹范围升级逻辑
        Debug.Log($"爆炸范围提升: {value}");
    }

    /// 更新对象池中所有子弹的攻击力/特殊伤害
    private void UpdateAllPooledBulletsDamage(BulletType bulletType, PoolType poolType, int newDamage)
    {
        // 根据池类型获取对应的对象池实例及子弹列表
        IEnumerable<GameObject> bulletList = GetBulletListByPoolType(poolType);
        if (bulletList == null)
        {
            Debug.LogWarning($"更新失败：{poolType} 实例不存在或池中无子弹");
            return;
        }

        int updatedCount = 0;
        foreach (var bulletObj in bulletList)
        {
            // 跳过空对象或已销毁的子弹
            if (bulletObj == null || !bulletObj.activeInHierarchy)
                continue;

            // 根据子弹类型更新对应伤害属性
            switch (bulletType)
            {
                case BulletType.Bullet:
                    Bullet bullet = bulletObj.GetComponent<Bullet>();
                    if (bullet != null)
                    {
                        bullet.damage = newDamage;
                        updatedCount++;
                    }
                    break;
                case BulletType.ExplosiveBullet:
                    ExplosiveBullet explosive = bulletObj.GetComponent<ExplosiveBullet>();
                    if (explosive != null)
                    {
                        explosive.explosionDamage = newDamage;
                        updatedCount++;
                    }
                    break;
                case BulletType.FrostBullet:
                    FrostBullet frost = bulletObj.GetComponent<FrostBullet>();
                    if (frost != null)
                    {
                        frost.damage = newDamage;
                        updatedCount++;
                    }
                    break;
            }
        }

        Debug.Log($"已更新 {poolType} 中 {updatedCount} 个子弹的伤害(新值：{newDamage})");
    }

    // 根据池类型获取对应的子弹列表
    private IEnumerable<GameObject> GetBulletListByPoolType(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.BulletPool:
                return BulletPool.Instance != null ? BulletPool.Instance.GetAllBullets() : null;
            case PoolType.ExplosiveBulletPool:
                return ExplosiveBulletPool.Instance != null ? ExplosiveBulletPool.Instance.GetAllBullets() : null;
            case PoolType.FrostBulletPool:
                return FrostBulletPool.Instance != null ? FrostBulletPool.Instance.GetAllBullets() : null;
            default:
                Debug.LogError($"未定义的池类型：{poolType}");
                return null;
        }
    }
}