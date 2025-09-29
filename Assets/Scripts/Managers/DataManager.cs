using UnityEngine;
using System.Collections.Generic;

public static class DataManager
{
    // --------------- 所有数据键定义（集中管理，避免硬编码） ---------------
    // 基础数据（Int）
    public const string HighScoreKey = "HighScore";
    public const string CurrentWaveKey = "CurrentWave";
    public const string PlayerMaxHealthKey = "PlayerMaxHealth";
    public const string PlayerLevelKey = "PlayerLevel";
    public const string NextLevelExpKey = "NextLevelExp";
    public const string PlayerShootSpeedKey = "PlayerShootSpeed";

    // 敌人数据（Int）
    public const string Enemy1MaxHealthKey = "BaseEnemyMaxHealth";
    public const string Enemy2MaxHealthKey = "HeavyEnemyMaxHealth";
    public const string Enemy1DamageKey = "BaseEnemyDamage";
    public const string Enemy2DamageKey = "HeavyEnemyDamage";

    // 子弹数据（Int）
    public const string BaseBulletDamageKey = "BaseBulletDamage";
    public const string FlameBulletDamageKey = "FlameBulletDamage";
    public const string FrostBulletDamageKey = "FrostBulletDamage";

    // 子弹范围数据（Float）
    public const string FlameExplosionRangeKey = "FlameExplosionRange";
    public const string FrostFreezeRangeKey = "FrostFreezeDuration";


    // --------------- 内存缓存（减少PlayerPrefs访问频率） ---------------
    private static readonly Dictionary<string, int> _intCache = new Dictionary<string, int>();
    private static readonly Dictionary<string, float> _floatCache = new Dictionary<string, float>();
    private static bool _isDirty = false; // 标记数据是否需要写入磁盘


    // --------------- 初始化：注册退出游戏时自动保存 ---------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 应用退出时自动保存未写入的数据
        Application.quitting += FlushSave;
    }


    // --------------- Int类型数据操作 ---------------
    /// <summary>
    /// 保存Int类型数据（仅新值更优时更新，默认"新值>旧值"为更优）
    /// </summary>
    public static void SaveInt(string key, int newValue, bool isGreater = true, int defaultIfNone = 0)
    {
        // 优先从缓存获取旧值，减少PlayerPrefs读取
        if (!_intCache.TryGetValue(key, out int currentValue))
        {
            currentValue = PlayerPrefs.GetInt(key, defaultIfNone);
            _intCache[key] = currentValue; // 同步到缓存
        }

        // 仅当新值更优时更新
        if ((isGreater && newValue > currentValue) || (!isGreater && newValue < currentValue))
        {
            PlayerPrefs.SetInt(key, newValue);
            _intCache[key] = newValue; // 更新缓存
            _isDirty = true; // 标记需要保存到磁盘
        }
    }

    /// <summary>
    /// 读取Int类型数据（优先从缓存读取）
    /// </summary>
    public static int GetInt(string key, int defaultValue = 0)
    {
        if (_intCache.TryGetValue(key, out int value))
        {
            return value; // 缓存命中，直接返回
        }

        // 缓存未命中，从PlayerPrefs读取并同步到缓存
        value = PlayerPrefs.GetInt(key, defaultValue);
        _intCache[key] = value;
        return value;
    }


    // --------------- Float类型数据操作 ---------------
    /// <summary>
    /// 保存Float类型数据（仅新值更优时更新，默认"新值>旧值"为更优）
    /// </summary>
    public static void SaveFloat(string key, float newValue, bool isGreater = true, float defaultIfNone = 0)
    {
        if (!_floatCache.TryGetValue(key, out float currentValue))
        {
            currentValue = PlayerPrefs.GetFloat(key, defaultIfNone);
            _floatCache[key] = currentValue;
        }

        if ((isGreater && newValue > currentValue) || (!isGreater && newValue < currentValue))
        {
            PlayerPrefs.SetFloat(key, newValue);
            _floatCache[key] = newValue;
            _isDirty = true;
        }
    }

    /// <summary>
    /// 读取Float类型数据（优先从缓存读取）
    /// </summary>
    public static float GetFloat(string key, float defaultValue = 0)
    {
        if (_floatCache.TryGetValue(key, out float value))
        {
            return value;
        }

        value = PlayerPrefs.GetFloat(key, defaultValue);
        _floatCache[key] = value;
        return value;
    }


    // --------------- 手动触发保存（关卡结束时调用） ---------------
    /// <summary>
    /// 将所有修改的数据写入本地磁盘（建议关卡结束、暂停时调用）
    /// </summary>
    public static void FlushSave()
    {
        if (_isDirty)
        {
            PlayerPrefs.Save();
            _isDirty = false;
            // Debug.Log("数据已保存到本地"); // 调试用，发布时可注释
        }
    }


    // --------------- 辅助功能：清空所有保存数据 ---------------
    /// <summary>
    /// 清空所有保存的本地数据（谨慎使用）
    /// </summary>
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        _intCache.Clear();
        _floatCache.Clear();
        _isDirty = false;
        PlayerPrefs.Save();
        // Debug.Log("所有本地数据已清空");
    }
}