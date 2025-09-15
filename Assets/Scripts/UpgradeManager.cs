using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    string[] choices = { "Attack", "FireRate", "Nothing", "MaxHealth" };
    private static System.Random _random = new();
    
    [Tooltip("拖入玩家控制器实例")]
    public PlayerController playerController;
    [Tooltip("拖入子弹预制体")]
    public Bullet bulletPrefab;  // 仅用于更新预制体属性

    void Awake()
    {
        // 完善单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // 自动获取玩家控制器
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }

    public void ShowUpgradeOptions()
    {
        Debug.Log("升级了! 随机选择一个升级选项:");
        Debug.Log("增加子弹攻击力 / 增加攻击速度 / 无加成 / 提升最大生命值");

        string choice = GetRandomChoice(choices);
        Debug.Log("你的随机升级选项是: " + choice);

        //ApplyUpgrade(choice, playerController);
        ApplyUpgrade("Attack",playerController);
    }
    
    public string GetRandomChoice(string[] arr)
    {
        if (arr == null || arr.Length == 0)
        {
            Debug.LogError("升级选项数组为空或未初始化");
            return null;
        }
        return arr[_random.Next(arr.Length)];
    }

    public void ApplyUpgrade(string option, PlayerController player)
    {
        if (player == null)
        {
            Debug.LogError("玩家控制器为空，无法应用升级");
            return;
        }

        switch (option)
        {
            case "Attack":
                if (bulletPrefab == null)
                {
                    Debug.LogError("未设置子弹预制体，无法升级攻击力");
                    return;
                }

                // 1. 更新预制体（确保新生成的子弹使用新属性）
                bulletPrefab.damage += 5;
                Debug.Log($"子弹攻击力提升! 新攻击力: {bulletPrefab.damage}");

                // 2. 同步更新对象池中所有子弹
                UpdateAllPooledBulletsDamage(bulletPrefab.damage);
                break;
                
            case "FireRate":
                player.fireRate -= 0.05f;
                player.fireRate = Mathf.Max(0.1f, player.fireRate);
                Debug.Log($"攻击速度提升! 当前攻击速度: {player.fireRate:F2}");
                break;
                
            case "MaxHealth":
                player.health += 20;
                player.currentHealth = player.health;
                Debug.Log($"最大生命值提升! 当前生命值: {player.health}");
                break;
                
            case "Nothing":
                Debug.Log("本次升级没有获得任何加成");
                break;
                
            default:
                Debug.LogWarning("未知的升级选项: " + option);
                break;
        }
    }

    /// <summary>
    /// 更新对象池中所有子弹的攻击力
    /// </summary>
    /// <param name="newDamage">新的攻击力值</param>
    private void UpdateAllPooledBulletsDamage(int newDamage)
    {
        // 通过单例获取子弹池
        if (BulletPool.Instance == null)
        {
            Debug.LogError("子弹池实例不存在，无法更新子弹");
            return;
        }

        // 获取对象池中所有子弹并更新
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
    