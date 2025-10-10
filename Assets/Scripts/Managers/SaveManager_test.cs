using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

public static class SaveManager_test
{
    // --------------- 数据键枚举（类型安全，避免字符串拼写错误） ---------------
    public enum DataKey
    {
        // 基础数据（Int）
        HighScore,
        CurrentWave,
        PlayerMaxHealth,
        PlayerLevel,
        NextLevelExp,
        PlayerShootSpeed,

        // 敌人数据（Int）
        BaseEnemyMaxHealth,
        BaseEnemyExpEReward,
        HeavyEnemyMaxHealth,
        HeavyEnemyExpEReward,
        BaseEnemyDamage,
        HeavyEnemyDamage,

        // 子弹数据（Int）
        BaseBulletDamage,
        ExplosiveDamage,
        ExplosionRange,
        FrostDamage,
        FrostFreezeDuration,
        NormalBulletChance,
        ExplosiveBulletChance
    }

    // --------------- 加密配置（可根据需求修改密钥） ---------------
    private const string EncryptionKey = "GameData_ID"; // 加密密钥
    private const string ObjectSuffix = "_obj"; // 对象类型键后缀

    // --------------- 内存缓存（分层缓存减少IO操作） ---------------
    private static readonly Dictionary<DataKey, int> _intCache = new Dictionary<DataKey, int>();
    private static readonly Dictionary<DataKey, float> _floatCache = new Dictionary<DataKey, float>();
    private static readonly Dictionary<DataKey, object> _objectCache = new Dictionary<DataKey, object>();
    private static bool _isDirty = false; // 数据修改标记

    // --------------- 初始化：注册自动保存机制 ---------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Application.quitting += FlushSave;
        // 编辑器模式下播放结束时保存
        #if UNITY_EDITOR
                EditorApplication.playModeStateChanged += state =>
                {
                    if (state == PlayModeStateChange.ExitingPlayMode)
                        FlushSave();
                };
        #endif
    }

    // --------------- 核心加密解密方法 ---------------
    private static string Encrypt(string data)
    {
        if (string.IsNullOrEmpty(data)) return data;
        
        char[] dataChars = data.ToCharArray();
        char[] keyChars = EncryptionKey.ToCharArray();
        
        for (int i = 0; i < dataChars.Length; i++)
        {
            dataChars[i] ^= keyChars[i % keyChars.Length];
        }
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dataChars));
    }

    private static string Decrypt(string data)
    {
        if (string.IsNullOrEmpty(data)) return data;
        
        try
        {
            byte[] bytes = Convert.FromBase64String(data);
            char[] dataChars = System.Text.Encoding.UTF8.GetChars(bytes);
            char[] keyChars = EncryptionKey.ToCharArray();
            
            for (int i = 0; i < dataChars.Length; i++)
            {
                dataChars[i] ^= keyChars[i % keyChars.Length];
            }
            return new string(dataChars);
        }
        catch
        {
            Debug.LogWarning("数据解密失败，可能是数据损坏或密钥不匹配");
            return string.Empty;
        }
    }

    // --------------- Int类型数据操作 ---------------
    public static void SaveInt(DataKey key, int newValue, bool isGreater = true, int defaultIfNone = 0)
    {
        try
        {
            if (!_intCache.TryGetValue(key, out int currentValue))
            {
                string keyStr = key.ToString();
                currentValue = PlayerPrefs.GetInt(keyStr, defaultIfNone);
                _intCache[key] = currentValue;
            }

            if ((isGreater && newValue > currentValue) || (!isGreater && newValue < currentValue))
            {
                string keyStr = key.ToString();
                PlayerPrefs.SetInt(keyStr, newValue);
                _intCache[key] = newValue;
                _isDirty = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"保存Int数据失败 [{key}]: {e.Message}");
        }
    }

    public static int GetInt(DataKey key, int defaultValue = 0)
    {
        try
        {
            if (_intCache.TryGetValue(key, out int value))
                return value;

            string keyStr = key.ToString();
            value = PlayerPrefs.GetInt(keyStr, defaultValue);
            _intCache[key] = value;
            return value;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取Int数据失败 [{key}]: {e.Message}");
            return defaultValue;
        }
    }

    // --------------- Float类型数据操作 ---------------
    public static void SaveFloat(DataKey key, float newValue, bool isGreater = true, float defaultIfNone = 0)
    {
        try
        {
            if (!_floatCache.TryGetValue(key, out float currentValue))
            {
                string keyStr = key.ToString();
                currentValue = PlayerPrefs.GetFloat(keyStr, defaultIfNone);
                _floatCache[key] = currentValue;
            }

            if ((isGreater && newValue > currentValue) || (!isGreater && newValue < currentValue))
            {
                string keyStr = key.ToString();
                PlayerPrefs.SetFloat(keyStr, newValue);
                _floatCache[key] = newValue;
                _isDirty = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"保存Float数据失败 [{key}]: {e.Message}");
        }
    }

    public static float GetFloat(DataKey key, float defaultValue = 0)
    {
        try
        {
            if (_floatCache.TryGetValue(key, out float value))
                return value;

            string keyStr = key.ToString();
            value = PlayerPrefs.GetFloat(keyStr, defaultValue);
            _floatCache[key] = value;
            return value;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取Float数据失败 [{key}]: {e.Message}");
            return defaultValue;
        }
    }

    // --------------- 复杂对象类型操作（新增） ---------------
    public static void SaveObject<T>(DataKey key, T obj) where T : class, new()
    {
        try
        {
            if (obj == null)
            {
                Debug.LogWarning($"尝试保存空对象 [{key}]");
                return;
            }

            string json = JsonUtility.ToJson(obj);
            string encryptedJson = Encrypt(json);
            string keyStr = $"{key}{ObjectSuffix}";

            PlayerPrefs.SetString(keyStr, encryptedJson);
            _objectCache[key] = obj;
            _isDirty = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"保存对象数据失败 [{key}]: {e.Message}");
        }
    }

    public static T GetObject<T>(DataKey key, T defaultValue = null) where T : class, new()
    {
        try
        {
            if (_objectCache.TryGetValue(key, out object cachedObj) && cachedObj is T tObj)
                return tObj;

            string keyStr = $"{key}{ObjectSuffix}";
            if (!PlayerPrefs.HasKey(keyStr))
                return defaultValue;

            string encryptedJson = PlayerPrefs.GetString(keyStr);
            string json = Decrypt(encryptedJson);
            T obj = JsonUtility.FromJson<T>(json) ?? defaultValue;
            
            _objectCache[key] = obj;
            return obj;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取对象数据失败 [{key}]: {e.Message}");
            return defaultValue;
        }
    }

    // --------------- 初始化数据方法 ---------------
    public static int InitializeIntData(DataKey key, int initialValue)
    {
        if (HasKey(key))
            return GetInt(key);
        
        SaveIntForce(key, initialValue);
        return initialValue;
    }

    public static float InitializeFloatData(DataKey key, float initialValue)
    {
        if (HasKey(key))
            return GetFloat(key);
        
        SaveFloatForce(key, initialValue);
        return initialValue;
    }

    public static T InitializeObjectData<T>(DataKey key, T initialValue) where T : class, new()
    {
        if (HasKey(key, isObject: true))
            return GetObject<T>(key);
        
        SaveObject(key, initialValue);
        return initialValue;
    }

    // --------------- 强制保存方法 ---------------
    public static void SaveIntForce(DataKey key, int newValue)
    {
        try
        {
            string keyStr = key.ToString();
            PlayerPrefs.SetInt(keyStr, newValue);
            _intCache[key] = newValue;
            _isDirty = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"强制保存Int失败 [{key}]: {e.Message}");
        }
    }

    public static void SaveFloatForce(DataKey key, float newValue)
    {
        try
        {
            string keyStr = key.ToString();
            PlayerPrefs.SetFloat(keyStr, newValue);
            _floatCache[key] = newValue;
            _isDirty = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"强制保存Float失败 [{key}]: {e.Message}");
        }
    }

    // --------------- 数据检查与持久化 ---------------
    public static bool HasKey(DataKey key, bool isObject = false)
    {
        string keyStr = isObject ? $"{key}{ObjectSuffix}" : key.ToString();
        return PlayerPrefs.HasKey(keyStr);
    }

    public static void FlushSave()
    {
        if (_isDirty)
        {
            try
            {
                PlayerPrefs.Save();
                _isDirty = false;
                Debug.Log("数据已保存到本地");
            }
            catch (Exception e)
            {
                Debug.LogError($"数据持久化失败: {e.Message}");
            }
        }
        else
        {
            Debug.Log("无数据需要更新");
        }
    }

    // --------------- 数据清理 ---------------
    public static void ClearAllData()
    {
        try
        {
            PlayerPrefs.DeleteAll();
            _intCache.Clear();
            _floatCache.Clear();
            _objectCache.Clear();
            _isDirty = false;
            PlayerPrefs.Save();
            Debug.Log("所有本地数据已清空");
        }
        catch (Exception e)
        {
            Debug.LogError($"清空数据失败: {e.Message}");
        }
    }

    public static void DeleteKey(DataKey key, bool isObject = false)
    {
        try
        {
            string keyStr = isObject ? $"{key}{ObjectSuffix}" : key.ToString();
            PlayerPrefs.DeleteKey(keyStr);
            
            // 清理缓存
            if (!isObject)
            {
                _intCache.Remove(key);
                _floatCache.Remove(key);
            }
            else
            {
                _objectCache.Remove(key);
            }
            
            _isDirty = true;
            Debug.Log($"已删除数据键: {key}");
        }
        catch (Exception e)
        {
            Debug.LogError($"删除数据键失败 [{key}]: {e.Message}");
        }
    }
}