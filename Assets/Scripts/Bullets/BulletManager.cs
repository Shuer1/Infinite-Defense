using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    [Header("子弹配置")]
    public List<BulletConfig> bulletConfigs = new List<BulletConfig>();

    [Header("默认概率配置")]
    [Range(0,100)] public int defaultNormalChance = 70;
    [Range(0,100)] public int defaultExplosiveChance = 10;
    [Range(0,100)] public int defaultFrostChance = 10;
    [Range(0,100)] public int defaultLightningChance = 10;

    private Dictionary<BulletType, int> bulletChances = new Dictionary<BulletType, int>();
    private Dictionary<BulletType, BulletTemplate> bulletTemplates = new Dictionary<BulletType, BulletTemplate>();

    void Awake()
    {
        if (Instance != null && Instance != this){ Destroy(gameObject); return; }
        Instance = this;
        InitializeBulletSystem();
    }

    private void InitializeBulletSystem()
    {
        InitializeBulletChances();
        InitializeBulletTemplates();
        ValidateBulletConfigs();
    }

    private void InitializeBulletChances()
    {
        bulletChances.Clear();
        bulletChances[BulletType.Normal]     = DataManager.GetInt(DataManager.NormalBulletChanceKey, defaultNormalChance);
        bulletChances[BulletType.Explosive] = DataManager.GetInt(DataManager.ExplosiveBulletChanceKey, defaultExplosiveChance);
        bulletChances[BulletType.Frost]     = DataManager.GetInt(DataManager.FrostBulletChanceKey, defaultFrostChance);
        bulletChances[BulletType.Lightning] = 100 - bulletChances[BulletType.Normal] - bulletChances[BulletType.Explosive] - bulletChances[BulletType.Frost];
        ClampProbabilities();
    }

    private void InitializeBulletTemplates()
    {
        bulletTemplates.Clear();
        foreach(var config in bulletConfigs)
        {
            bulletTemplates[config.bulletType] = new BulletTemplate
            {
                bulletType     = config.bulletType,
                prefab         = config.prefab,
                baseDamage     = config.baseDamage,
                currentDamage  = config.baseDamage,
                baseSpeed      = config.baseSpeed,
                specialValue1  = config.specialValue1,
                specialValue2  = config.specialValue2
            };
        }
    }

    private void ValidateBulletConfigs()
    {
        var required = new[] { BulletType.Normal, BulletType.Explosive, BulletType.Frost, BulletType.Lightning };
        var missing = required.Where(t => !bulletConfigs.Any(c => c.bulletType == t)).ToList();
        if (missing.Count > 0)
            Debug.LogWarning($"[BulletManager] 缺少子弹类型配置: {string.Join(",", missing)}");
    }

    public BulletType GetRandomBulletType()
    {
        int randomValue = Random.Range(0,100);
        int cumulative = 0;
        foreach (var kvp in bulletChances.OrderBy(x => x.Key))
        {
            cumulative += kvp.Value;
            if (randomValue < cumulative) return kvp.Key;
        }
        return BulletType.Normal;
    }

    public GameObject GetBullet(BulletType bulletType, Vector3 position, Quaternion rotation)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogError("[BulletManager] BulletPoolManager 未初始化！");
            return null;
        }
        return BulletPoolManager.Instance.GetBullet(bulletType, position, rotation);
    }

    public void UpdateBulletChance(BulletType type, int newChance)
    {
        if (bulletChances.ContainsKey(type))
        {
            bulletChances[type] = newChance;
            ClampProbabilities();
            SaveBulletChances();
        }
    }

    public void AdjustBulletChances(int normalDelta, int explosiveDelta, int frostDelta)
    {
        bulletChances[BulletType.Normal]     += normalDelta;
        bulletChances[BulletType.Explosive] += explosiveDelta;
        bulletChances[BulletType.Frost]     += frostDelta;
        ClampProbabilities();
        SaveBulletChances();
    }

    private void ClampProbabilities()
    {
        int total = bulletChances.Values.Sum();
        if (total != 100)
        {
            float scale = 100f / total;
            var types = bulletChances.Keys.ToList();
            for(int i = 0; i < types.Count-1; i++)
            {
                bulletChances[types[i]] = Mathf.RoundToInt(bulletChances[types[i]] * scale);
            }
            bulletChances[types.Last()] = 100 - bulletChances.Take(types.Count - 1).Sum(v => v.Value);
        }
    }

    private void SaveBulletChances()
    {
        DataManager.SaveInt(DataManager.NormalBulletChanceKey,     bulletChances[BulletType.Normal]);
        DataManager.SaveInt(DataManager.ExplosiveBulletChanceKey, bulletChances[BulletType.Explosive]);
        DataManager.SaveInt(DataManager.FrostBulletChanceKey,     bulletChances[BulletType.Frost]);
    }

    public void UpdateBulletDamage(BulletType type, int newDamage)
    {
        if (!bulletTemplates.ContainsKey(type)) return;
        bulletTemplates[type].currentDamage = newDamage;
        SyncBulletPropertiesToPool(type);
    }

    public void UpdateBulletSpecialValue(BulletType type, int index, float newValue)
    {
        if (!bulletTemplates.ContainsKey(type)) return;

        if (index == 1) bulletTemplates[type].specialValue1 = newValue;
        if (index == 2) bulletTemplates[type].specialValue2 = newValue;

        SyncBulletPropertiesToPool(type);
    }


    private void SyncBulletPropertiesToPool(BulletType bulletType)
    {
        Debug.Log($"[BulletManager] 同步子弹属性到池子: {bulletType}");
        if (BulletPoolManager.Instance == null) return;
        if (!bulletTemplates.ContainsKey(bulletType)) return;

        var template = bulletTemplates[bulletType];
        var bullets = BulletPoolManager.Instance.GetAllBullets(bulletType);

        foreach(var bulletObj in bullets)
        {
            if (bulletObj == null) continue;
            
            switch(bulletType)
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
                    }
                    break;
            }
        }
    }

    public IReadOnlyDictionary<BulletType,int> GetBulletChances() => 
        new Dictionary<BulletType,int>(bulletChances);
}

[System.Serializable]
public class BulletConfig
{
    public BulletType bulletType;
    public GameObject prefab;
    public int baseDamage = 10;
    public float baseSpeed = 15f;
    public float specialValue1 = 0f;
    public float specialValue2 = 0f;
}

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
