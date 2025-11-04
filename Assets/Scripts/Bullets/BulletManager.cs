using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance;

    [Header("※仅用于无存档时初始化DataManager，不再直接控制子弹属性※")]
    public List<BulletConfig> bulletConfigs = new List<BulletConfig>();

    // --- 子弹概率 ---
    private Dictionary<BulletType, int> bulletChances = new Dictionary<BulletType, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeBulletChances();
        InitializeBulletDataIfNotExist();
    }

    // =========================================================
    // 1. 子弹概率只依赖DataManager，不依赖模板
    // =========================================================
    private void InitializeBulletChances()
    {
        bulletChances[BulletType.Normal]    = DataManager.GetInt(DataManager.NormalBulletChanceKey, 70);
        bulletChances[BulletType.Explosive] = DataManager.GetInt(DataManager.ExplosiveBulletChanceKey, 10);
        bulletChances[BulletType.Frost]     = DataManager.GetInt(DataManager.FrostBulletChanceKey, 10);
        bulletChances[BulletType.Lightning] = DataManager.GetInt(DataManager.LightningBulletChanceKey, 10);
        ClampAndNormalizeChances();
        SaveBulletChances();
    }

    private void SaveBulletChances()
    {
        DataManager.SaveInt(DataManager.NormalBulletChanceKey, bulletChances[BulletType.Normal]);
        DataManager.SaveInt(DataManager.ExplosiveBulletChanceKey, bulletChances[BulletType.Explosive]);
        DataManager.SaveInt(DataManager.FrostBulletChanceKey, bulletChances[BulletType.Frost]);
        DataManager.SaveInt(DataManager.LightningBulletChanceKey, bulletChances[BulletType.Lightning]);
    }

    private void ClampAndNormalizeChances()
    {
        foreach (var key in bulletChances.Keys.ToList())
            bulletChances[key] = Mathf.Clamp(bulletChances[key], 0, 100);

        int total = bulletChances.Values.Sum();
        if (total == 0)
        {
            bulletChances[BulletType.Normal] = 100;
            return;
        }

        if (total != 100)
        {
            float scale = 100f / total;
            foreach (var key in bulletChances.Keys.ToList())
                bulletChances[key] = Mathf.RoundToInt(bulletChances[key] * scale);
        }
    }

    // =========================================================
    // 2. 初始化子弹属性到 DataManager（仅第一次，无持久化数据时）
    // =========================================================
    private void InitializeBulletDataIfNotExist()
    {
        foreach (var config in bulletConfigs)
        {
            switch (config.bulletType)
            {
                case BulletType.Normal:
                    InitIfMissing(DataManager.BaseBulletDamageKey, config.baseDamage);
                    break;

                case BulletType.Explosive:
                    InitIfMissing(DataManager.ExplosiveDamageKey, config.extraDamage);
                    InitIfMissing(DataManager.ExplosionRangeKey, config.specialValue1);
                    break;

                case BulletType.Frost:
                    InitIfMissing(DataManager.FrostDamageKey, config.extraDamage);
                    InitIfMissing(DataManager.FrostFreezeDurationKey, config.specialValue1);
                    break;

                case BulletType.Lightning:
                    InitIfMissing(DataManager.LightningBulletDamageKey, config.extraDamage);
                    InitIfMissing(DataManager.LightningCountKey, (int)config.specialValue1);
                    break;
            }
        }
    }

    private void InitIfMissing(string key, int defaultValue)
    {
        if (!DataManager.HasKey(key))
            DataManager.SaveInt(key, defaultValue);
    }

    private void InitIfMissing(string key, float defaultValue)
    {
        if (!DataManager.HasKey(key))
            DataManager.SaveFloat(key, defaultValue);
    }

    // =========================================================
    // 3. 对外调用接口（保持原有功能）
    // =========================================================
    public BulletType GetRandomBulletType()
    {
        int rnd = Random.Range(0, 100);
        int cumulative = 0;
        foreach (var kv in bulletChances)
        {
            cumulative += kv.Value;
            if (rnd < cumulative)
                return kv.Key;
        }
        return BulletType.Normal;
    }

    public GameObject GetBullet(BulletType type, Vector3 pos, Quaternion rot)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogError("[BulletManager] BulletPoolManager 未初始化！");
            return null;
        }
        return BulletPoolManager.Instance.GetBullet(type, pos, rot);
    }

    public IReadOnlyDictionary<BulletType, int> GetBulletChances()
    {
        return bulletChances;
    }

    // =========================================================
    // 4. 数据修改接口（升级/设定面板调用）→ 写入 DataManager + 同步Pool
    // =========================================================
    public void UpdateBulletDamage(BulletType type, int newDamage)
    {
        switch (type)
        {
            case BulletType.Normal:
                DataManager.SaveInt(DataManager.BaseBulletDamageKey, newDamage);
                break;
            case BulletType.Explosive:
                DataManager.SaveInt(DataManager.ExplosiveDamageKey, newDamage);
                break;
            case BulletType.Frost:
                DataManager.SaveInt(DataManager.FrostDamageKey, newDamage);
                break;
            case BulletType.Lightning:
                DataManager.SaveInt(DataManager.LightningBulletDamageKey, newDamage);
                break;
        }
        SyncBulletPropertiesToPool(type);
    }

    public void UpdateBulletChance(BulletType type, int newValue)
    {
        if (!bulletChances.ContainsKey(type)) return;

        bulletChances[type] = Mathf.Clamp(newValue, 0, 100);
        ClampAndNormalizeChances();
        SaveBulletChances();
    }

    public void UpdateBulletSpecialValue(BulletType type, int index, float value)
    {
        switch (type)
        {
            case BulletType.Explosive:
                if (index == 1) DataManager.SaveFloat(DataManager.ExplosionRangeKey, value);
                break;
            case BulletType.Frost:
                if (index == 1) DataManager.SaveFloat(DataManager.FrostFreezeDurationKey, value);
                break;
            case BulletType.Lightning:
                if (index == 1) DataManager.SaveInt(DataManager.LightningCountKey, Mathf.RoundToInt(value));
                break;
        }
        SyncBulletPropertiesToPool(type);
    }

    // =========================================================
    // 5. 保留：同步对象池中的子弹实例（但数据从 DataManager 获取）
    // =========================================================
    public void SyncBulletPropertiesToPool(BulletType type)
    {
        if (BulletPoolManager.Instance == null) return;

        var bullets = BulletPoolManager.Instance.GetAllBullets(type);
        foreach (var obj in bullets)
        {
            if (obj == null) continue;

            switch (type)
            {
                case BulletType.Normal:
                    var b = obj.GetComponent<Bullet>();
                    if (b != null) b.damage = DataManager.GetInt(DataManager.BaseBulletDamageKey, b.damage);
                    break;

                case BulletType.Explosive:
                    var e = obj.GetComponent<ExplosiveBullet>();
                    if (e != null)
                    {
                        e.explosionDamage = DataManager.GetInt(DataManager.ExplosiveDamageKey, e.explosionDamage);
                        e.explosionRadius = DataManager.GetFloat(DataManager.ExplosionRangeKey, e.explosionRadius);
                    }
                    break;

                case BulletType.Frost:
                    var f = obj.GetComponent<FrostBullet>();
                    if (f != null)
                    {
                        f.extraFrostDamage = DataManager.GetInt(DataManager.FrostDamageKey, f.extraFrostDamage);
                        f.slowDuration = DataManager.GetFloat(DataManager.FrostFreezeDurationKey, f.slowDuration);
                    }
                    break;

                case BulletType.Lightning:
                    var l = obj.GetComponent<LightningBullet>();
                    if (l != null)
                    {
                        l.lightningDamage = DataManager.GetInt(DataManager.LightningBulletDamageKey, l.lightningDamage);
                        l.lightningCount = DataManager.GetInt(DataManager.LightningCountKey, l.lightningCount);
                    }
                    break;
            }
        }
    }
}

[System.Serializable]
public class BulletConfig
{
    public BulletType bulletType;
    public GameObject prefab;
    public int baseDamage = 10;
    public float baseSpeed = 15f;
    public int extraDamage = 0;
    public float specialValue1 = 0f;
    public float specialValue2 = 0f;
}
