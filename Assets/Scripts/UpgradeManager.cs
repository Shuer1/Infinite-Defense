using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    
    [Tooltip("所有可用的升级选项")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>();
    
    [Tooltip("拖入玩家控制器实例")]
    public PlayerController playerController;
    
    [Tooltip("拖入子弹预制体")]
    public Bullet bulletPrefab;

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
    }

    /// <summary>
    /// 显示升级选项（随机选择3个）
    /// </summary>
    public void ShowUpgradeOptions()
    {
        if (allUpgrades.Count < 3)
        {
            Debug.LogError("升级选项不足3个，请检查配置");
            return;
        }

        // 随机选择3个不重复的升级选项
        var randomUpgrades = GetRandomUniqueUpgrades(3);
        
        // 显示升级面板
        if (UpgradePanel.Instance != null)
        {
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
            case "Attack":
                ApplyAttackUpgrade(value);
                break;
            case "FireRate":
                ApplyFireRateUpgrade(value);
                break;
            case "MaxHealth":
                ApplyMaxHealthUpgrade(value);
                break;
            /*
            case "MoveSpeed":
                ApplyMoveSpeedUpgrade(value);
                break;
            case "BulletSpeed":
                ApplyBulletSpeedUpgrade(value);
                break;
            case "BulletRange":
                ApplyBulletRangeUpgrade(value);
                break;
            */
            default:
                Debug.LogWarning($"未知的升级类型: {upgradeId}");
                break;
        }
    }

    // 各种升级效果的具体实现
    private void ApplyAttackUpgrade(int value)
    {
        if (bulletPrefab == null) return;
        
        bulletPrefab.damage += value;
        UpdateAllPooledBulletsDamage(bulletPrefab.damage);
        Debug.Log($"子弹攻击力提升! 新攻击力: {bulletPrefab.damage}");
    }

    private void ApplyFireRateUpgrade(int value)
    {
        if (playerController == null) return;
        
        float fireRateReduction = value * 0.01f; // 转换为攻击速度减少值
        playerController.fireRate = Mathf.Max(0.1f, playerController.fireRate - fireRateReduction);
        Debug.Log($"攻击速度提升! 当前攻击速度: {playerController.fireRate:F2}");
    }

    private void ApplyMaxHealthUpgrade(int value)
    {
        if (playerController == null) return;
        
        playerController.health += value;
        playerController.currentHealth = playerController.health;
        Debug.Log($"最大生命值提升! 当前生命值: {playerController.health}");
    }

    private void ApplyMoveSpeedUpgrade(int value)
    {
        if (playerController == null) return;
        
        playerController.moveSpeed += value * 0.1f; // 移动速度小幅提升
        Debug.Log($"移动速度提升! 当前速度: {playerController.moveSpeed:F2}");
    }

    private void ApplyBulletSpeedUpgrade(int value)
    {
        // 实现子弹速度升级逻辑
        Debug.Log($"子弹速度提升! 提升值: {value}");
    }

    private void ApplyBulletRangeUpgrade(int value)
    {
        // 实现子弹射程升级逻辑
        Debug.Log($"子弹射程提升! 提升值: {value}");
    }

    /// <summary>
    /// 更新对象池中所有子弹的攻击力
    /// </summary>
    private void UpdateAllPooledBulletsDamage(int newDamage)
    {
        if (BulletPool.Instance == null) return;

        int updatedCount = 0;
        foreach (var bulletObj in BulletPool.Instance.GetAllBullets())
        {
            if (bulletObj != null)
            {
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.damage = newDamage;
                    updatedCount++;
                }
            }
        }

        Debug.Log($"已更新对象池中 {updatedCount} 个子弹的攻击力");
    }
}
    