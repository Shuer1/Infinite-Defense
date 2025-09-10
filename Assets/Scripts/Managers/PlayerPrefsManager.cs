using UnityEngine;
using System;

/// <summary>
/// PlayerPrefs存储管理器
/// 提供数据存储、读取、删除等功能
/// </summary>
public class PlayerPrefsManager : MonoBehaviour
{
    // 单例实例
    private static PlayerPrefsManager _instance;
    public static PlayerPrefsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 查找现有实例
                _instance = FindObjectOfType<PlayerPrefsManager>();
                
                // 如果没有实例，则创建一个新的
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("PlayerPrefsManager");
                    _instance = managerObject.AddComponent<PlayerPrefsManager>();
                    
                    // 确保在场景切换时不被销毁
                    DontDestroyOnLoad(managerObject);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // 确保单例唯一性
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // 初始化检查
        Initialize();
    }

    /// <summary>
    /// 初始化检查
    /// </summary>
    private void Initialize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL平台检查localStorage是否可用
        if (!IsWebGLStorageAvailable())
        {
            Debug.LogWarning("WebGL localStorage不可用，可能处于隐私模式或浏览器限制");
        }
#endif
    }

    #region 存储数据
    /// <summary>
    /// 存储整数
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public void SetInt(string key, int value)
    {
        try
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            Debug.Log($"成功存储整数: {key} = {value}");
        }
        catch (Exception e)
        {
            Debug.LogError($"存储整数失败 {key}: {e.Message}");
        }
    }

    /// <summary>
    /// 存储浮点数
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public void SetFloat(string key, float value)
    {
        try
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            Debug.Log($"成功存储浮点数: {key} = {value}");
        }
        catch (Exception e)
        {
            Debug.LogError($"存储浮点数失败 {key}: {e.Message}");
        }
    }

    /// <summary>
    /// 存储字符串
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public void SetString(string key, string value)
    {
        try
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            Debug.Log($"成功存储字符串: {key} = {value}");
        }
        catch (Exception e)
        {
            Debug.LogError($"存储字符串失败 {key}: {e.Message}");
        }
    }

    /// <summary>
    /// 存储布尔值
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="value">值</param>
    public void SetBool(string key, bool value)
    {
        // PlayerPrefs不直接支持bool，用int 0和1代替
        SetInt(key, value ? 1 : 0);
    }
    #endregion

    #region 读取数据
    /// <summary>
    /// 读取整数
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>读取到的值或默认值</returns>
    public int GetInt(string key, int defaultValue = 0)
    {
        try
        {
            int value = PlayerPrefs.GetInt(key, defaultValue);
            Debug.Log($"读取整数: {key} = {value}");
            return value;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取整数失败 {key}: {e.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 读取浮点数
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>读取到的值或默认值</returns>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        try
        {
            float value = PlayerPrefs.GetFloat(key, defaultValue);
            Debug.Log($"读取浮点数: {key} = {value}");
            return value;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取浮点数失败 {key}: {e.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 读取字符串
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>读取到的值或默认值</returns>
    public string GetString(string key, string defaultValue = "")
    {
        try
        {
            string value = PlayerPrefs.GetString(key, defaultValue);
            Debug.Log($"读取字符串: {key} = {value}");
            return value;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取字符串失败 {key}: {e.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 读取布尔值
    /// </summary>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>读取到的值或默认值</returns>
    public bool GetBool(string key, bool defaultValue = false)
    {
        int intValue = GetInt(key, defaultValue ? 1 : 0);
        return intValue == 1;
    }
    #endregion

    #region 数据管理
    /// <summary>
    /// 检查键是否存在
    /// </summary>
    /// <param name="key">键名</param>
    /// <returns>是否存在</returns>
    public bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// 删除指定键的数据
    /// </summary>
    /// <param name="key">键名</param>
    public void DeleteKey(string key)
    {
        try
        {
            if (HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"已删除键: {key}");
            }
            else
            {
                Debug.LogWarning($"删除失败，键不存在: {key}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"删除键失败 {key}: {e.Message}");
        }
    }

    /// <summary>
    /// 清除所有存储的数据
    /// </summary>
    public void DeleteAll()
    {
        try
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("已清除所有存储的数据");
        }
        catch (Exception e)
        {
            Debug.LogError($"清除数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 强制保存所有数据
    /// </summary>
    public void Save()
    {
        try
        {
            PlayerPrefs.Save();
            Debug.Log("数据已强制保存");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存数据失败: {e.Message}");
        }
    }
    #endregion

    #region 平台特定处理
#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// 检查WebGL平台localStorage是否可用
    /// </summary>
    /// <returns>是否可用</returns>
    private bool IsWebGLStorageAvailable()
    {
        try
        {
            string testKey = "storage_test";
            PlayerPrefs.SetString(testKey, "test_value");
            PlayerPrefs.Save();
            bool result = PlayerPrefs.GetString(testKey) == "test_value";
            PlayerPrefs.DeleteKey(testKey);
            PlayerPrefs.Save();
            return result;
        }
        catch
        {
            return false;
        }
    }
#endif
    #endregion
}
