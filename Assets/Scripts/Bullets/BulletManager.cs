using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 🔫 子弹管理器 - 统一管理所有子弹相关逻辑
/// 职责：子弹配置、概率管理、属性模板、升级支持
/// </summary>
public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    [Header("子弹配置")]
    public List<BulletConfig> bulletConfigs = new List<BulletConfig>();

    [Header("默认概率配置")]
    [Range(0, 100)] public int defaultNormalChance = 70;
    [Range(0, 100)] public int defaultExplosiveChance = 10;
    [Range(0, 100)] public int defaultFrostChance = 10;
    [Range(0, 100)] public int defaultLightningChance = 10;

    // 运行时概率配置
    private Dictionary<BulletType, int> bulletChances = new Dictionary<BulletType, int>();
    
    // 子弹属性模板
    private Dictionary<BulletType, BulletTemplate> bulletTemplates = new Dictionary<BulletType, BulletTemplate>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeBulletSystem();
    }

    /// <summary>
    /// 初始化子弹系统
    /// </summary>
    private void InitializeBulletSystem()
    {
        InitializeBulletChances();
        InitializeBulletTemplates();
        ValidateBulletConfigs();
    }

    /// <summary>
    /// 初始化子弹概率
    /// </summary>
    private void InitializeBulletChances()
    {
        bulletChances.Clear();
        bulletChances[BulletType.Normal] = DataManager.GetInt(DataManager.NormalBulletChanceKey, defaultNormalChance);
        bulletChances[BulletType.Explosive] = DataManager.GetInt(DataManager.ExplosiveBulletChanceKey, defaultExplosiveChance);
        bulletChances[BulletType.Frost] = DataManager.GetInt(DataManager.FrostBulletChanceKey, defaultFrostChance);
        bulletChances[BulletType.Lightning] = 100 - bulletChances[BulletType.Normal] - 
                                            bulletChances[BulletType.Explosive] - 
                                            bulletChances[BulletType.Frost];
        
        ClampProbabilities();
    }

    /// <summary>
    /// 初始化子弹属性模板
    /// </summary>
    private void InitializeBulletTemplates()
    {
        bulletTemplates.Clear();
        
        foreach (var config in bulletConfigs)
        {
            var template = new BulletTemplate
            {
                bulletType = config.bulletType,
                prefab = config.prefab,
                baseDamage = config.baseDamage,
                currentDamage = config.baseDamage,
                baseSpeed = config.baseSpeed,
                specialValue1 = config.specialValue1,
                specialValue2 = config.specialValue2
            };
            
            bulletTemplates[config.bulletType] = template;
        }
    }

    /// <summary>
    /// 验证子弹配置
    /// </summary>
    private void ValidateBulletConfigs()
    {
        var requiredTypes = new[] { BulletType.Normal, BulletType.Explosive, BulletType.Frost, BulletType.Lightning };
        var missingTypes = requiredTypes.Where(type => !bulletConfigs.Any(c => c.bulletType == type)).ToList();
        
        if (missingTypes.Count > 0)
        {
            Debug.LogWarning($"[BulletManager] 缺少子弹类型配置: {string.Join(", ", missingTypes)}");
        }
    }

    /// <summary>
    /// 根据概率获取随机子弹类型
    /// </summary>
    public BulletType GetRandomBulletType()
    {
        int randomValue = Random.Range(0, 100);
        int cumulative = 0;

        foreach (var kvp in bulletChances.OrderBy(x => x.Key))
        {
            cumulative += kvp.Value;
            if (randomValue < cumulative)
            {
                return kvp.Key;
            }
        }

        return BulletType.Normal; // 默认返回普通子弹
    }

    /// <summary>
    /// 从对象池获取子弹实例
    /// </summary>
    public GameObject GetBullet(BulletType bulletType, Vector3 position, Quaternion rotation)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogError("[BulletManager] BulletPoolManager 未初始化！");
            return null;
        }

        string bulletTypeName = bulletType.ToString();
        return BulletPoolManager.Instance.GetBullet(bulletTypeName, position, rotation);
    }

    /// <summary>
    /// 更新子弹概率
    /// </summary>
    public void UpdateBulletChance(BulletType type, int newChance)
    {
        if (bulletChances.ContainsKey(type))
        {
            bulletChances[type] = newChance;
            ClampProbabilities();
            SaveBulletChances();
        }
    }

    /// <summary>
    /// 调整子弹概率（用于升级）
    /// </summary>
    public void AdjustBulletChances(int normalDelta, int explosiveDelta, int frostDelta)
    {
        bulletChances[BulletType.Normal] += normalDelta;
        bulletChances[BulletType.Explosive] += explosiveDelta;
        bulletChances[BulletType.Frost] += frostDelta;
        
        ClampProbabilities();
        SaveBulletChances();
    }

    /// <summary>
    /// 更新子弹伤害
    /// </summary>
    public void UpdateBulletDamage(BulletType type, int newDamage)
    {
        if (bulletTemplates.ContainsKey(type))
        {
            bulletTemplates[type].currentDamage = newDamage;
            
            // 同步到对象池中的所有子弹
            SyncBulletPropertiesToPool(type);
        }
    }

    /// <summary>
    /// 获取子弹当前伤害
    /// </summary>
    public int GetBulletDamage(BulletType type)
    {
        return bulletTemplates.ContainsKey(type) ? bulletTemplates[type].currentDamage : 0;
    }

    /// <summary>
    /// 获取子弹特殊属性
    /// </summary>
    public float GetBulletSpecialValue(BulletType type, int index)
    {
        if (!bulletTemplates.ContainsKey(type)) return 0f;
        
        var template = bulletTemplates[type];
        return index == 1 ? template.specialValue1 : template.specialValue2;
    }

    /// <summary>
    /// 更新子弹特殊属性
    /// </summary>
    public void UpdateBulletSpecialValue(BulletType type, int index, float value)
    {
        if (!bulletTemplates.ContainsKey(type)) return;
        
        var template = bulletTemplates[type];
        if (index == 1)
            template.specialValue1 = value;
        else
            template.specialValue2 = value;
            
        SyncBulletPropertiesToPool(type);
    }

    /// <summary>
    /// 确保概率总和为100%
    /// </summary>
    private void ClampProbabilities()
    {
        int total = bulletChances.Values.Sum();
        if (total != 100)
        {
            float scale = 100f / total;
            var types = bulletChances.Keys.ToList();
            
            for (int i = 0; i < types.Count - 1; i++)
            {
                bulletChances[types[i]] = Mathf.RoundToInt(bulletChances[types[i]] * scale);
            }
            
            // 最后一个类型确保总和为100
            bulletChances[types.Last()] = 100 - bulletChances.Take(types.Count - 1).Sum(x => x.Value);
        }
    }

    /// <summary>
    /// 保存子弹概率到数据管理器
    /// </summary>
    private void SaveBulletChances()
    {
        DataManager.SaveInt(DataManager.NormalBulletChanceKey, bulletChances[BulletType.Normal]);
        DataManager.SaveInt(DataManager.ExplosiveBulletChanceKey, bulletChances[BulletType.Explosive]);
        DataManager.SaveInt(DataManager.FrostBulletChanceKey, bulletChances[BulletType.Frost]);
    }

    /// <summary>
    /// 同步子弹属性到对象池
    /// </summary>
    private void SyncBulletPropertiesToPool(BulletType bulletType)
    {
        if (BulletPoolManager.Instance == null) return;

        string typeName = bulletType.ToString();
        var bullets = BulletPoolManager.Instance.GetAllBullets(typeName);
        
        if (!bulletTemplates.ContainsKey(bulletType)) return;
        
        var template = bulletTemplates[bulletType];
        
        foreach (var bulletObj in bullets)
        {
            if (bulletObj == null || !bulletObj.activeInHierarchy) continue;
            
            // 更新不同类型子弹的属性
            switch (bulletType)
            {
                case BulletType.Normal:
                    var bullet = bulletObj.GetComponent<Bullet>();
                    if (bullet != null) bullet.damage = template.currentDamage;
                    break;
                    
                case BulletType.Explosive:
                    var explosive = bulletObj.GetComponent<ExplosiveBullet>();
                    if (explosive != null)
                    {
                        explosive.explosionDamage = template.currentDamage;
                        explosive.explosionRadius = template.specialValue1;
                    }
                    break;
                    
                case BulletType.Frost:
                    var frost = bulletObj.GetComponent<FrostBullet>();
                    if (frost != null)
                    {
                        frost.extraFrostDamage = template.currentDamage;
                        frost.slowDuration = template.specialValue1;
                    }
                    break;
                    
                case BulletType.Lightning:
                    var lightning = bulletObj.GetComponent<LightningBullet>();
                    if (lightning != null)
                    {
                        lightning.lightningDamage = template.currentDamage;
                        lightning.lightningCount = Mathf.RoundToInt(template.specialValue1);
                        lightning.lightningRange = template.specialValue2;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 获取当前概率配置（只读）
    /// </summary>
    public IReadOnlyDictionary<BulletType, int> GetBulletChances()
    {
        return new Dictionary<BulletType, int>(bulletChances);
    }
}

/// <summary>
/// 子弹配置数据结构
/// </summary>
[System.Serializable]
public class BulletConfig
{
    public BulletType bulletType;
    public GameObject prefab;
    public int baseDamage = 10;
    public float baseSpeed = 15f;
    [Tooltip("特殊属性1 - 根据子弹类型不同含义不同")]
    public float specialValue1 = 0f;
    [Tooltip("特殊属性2 - 根据子弹类型不同含义不同")]
    public float specialValue2 = 0f;
}

/// <summary>
/// 子弹模板 - 运行时子弹属性
/// </summary>
public class BulletTemplate
{
    public BulletType bulletType;
    public GameObject prefab;
    public int baseDamage;
    public int currentDamage;
    public float baseSpeed;
    public float specialValue1;
    public float specialValue2;
}