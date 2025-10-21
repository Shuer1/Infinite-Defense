using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局背景音乐管理器，负责统一管理游戏中所有背景音乐的播放状态
/// 采用单例模式确保全局唯一实例
/// </summary>
public class EntireBgmManager : MonoBehaviour
{
    [Header("背景音乐数组")]
    [SerializeField] private AudioSource[] entireBgms;  // 存储所有背景音乐的音频源

    [Header("初始播放设置")]
    [SerializeField] private int initialOrder = 0;      // 初始播放的背景音乐索引
    public float initialVolume = 1f;  // 初始音量(0-1)

    [SerializeField] private Slider soundVolume;

    private static EntireBgmManager instance;           // 单例实例
    private int currentPlayingIndex = -1;               // 当前播放的背景音乐索引(-1表示未播放)

    /// <summary>
    /// 单例访问点
    /// </summary>
    public static EntireBgmManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("EntireBgmManager instance not found! Please ensure there's one in the scene.");
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 单例模式实现：确保全局只有一个实例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);  // 跨场景保留
        InitializeBgms();
    }

    private void Start()
    {
        // 播放初始背景音乐
        PlayBgm(initialOrder);
    }

    /// <summary>
    /// 初始化背景音乐设置
    /// </summary>
    private void InitializeBgms()
    {
        if (entireBgms == null || entireBgms.Length == 0)
        {
            Debug.LogWarning("No BGMs assigned to EntireBgmManager!");
            return;
        }

        // 初始化所有音频源的基础设置
        foreach (var bgm in entireBgms)
        {
            if (bgm != null)
            {
                bgm.volume = initialVolume;
                bgm.playOnAwake = false;  // 禁止自动播放
            }
        }
    }

    /// <summary>
    /// 播放指定索引的背景音乐
    /// </summary>
    /// <param name="index">背景音乐索引</param>
    public void PlayBgm(int index)
    {
        // 检查音频源数组是否有效
        if (entireBgms == null || entireBgms.Length == 0)
        {
            Debug.LogError("BGM array is not initialized!");
            return;
        }

        // 检查索引是否有效
        if (index < 0 || index >= entireBgms.Length)
        {
            Debug.LogError($"Invalid BGM index: {index}. Valid range is 0-{entireBgms.Length - 1}");
            return;
        }

        // 停止当前播放的音乐(如果有)
        if (currentPlayingIndex != -1 && currentPlayingIndex < entireBgms.Length)
        {
            entireBgms[currentPlayingIndex].Stop();
        }

        // 播放新的音乐
        if (entireBgms[index] != null)
        {
            entireBgms[index].Play();
            currentPlayingIndex = index;
            Debug.Log($"Playing BGM: {index}");
        }
        else
        {
            Debug.LogError($"BGM at index {index} is null!");
            currentPlayingIndex = -1;
        }
    }

    /// <summary>
    /// 停止当前播放的背景音乐
    /// </summary>
    public void StopCurrentBgm()
    {
        if (IsValidIndex(currentPlayingIndex))
        {
            entireBgms[currentPlayingIndex].Stop();
            Debug.Log($"Stopped BGM: {currentPlayingIndex}");
            currentPlayingIndex = -1;
        }
        else
        {
            Debug.LogWarning("No BGM is currently playing!");
        }
    }

    /// <summary>
    /// 暂停当前播放的背景音乐
    /// </summary>
    public void PauseCurrentBgm()
    {
        if (IsValidIndex(currentPlayingIndex))
        {
            entireBgms[currentPlayingIndex].Pause();
            Debug.Log($"Paused BGM: {currentPlayingIndex}");
        }
        else
        {
            Debug.LogWarning("No BGM is currently playing!");
        }
    }

    /// <summary>
    /// 恢复播放当前暂停的背景音乐
    /// </summary>
    public void ResumeCurrentBgm()
    {
        if (IsValidIndex(currentPlayingIndex) && !entireBgms[currentPlayingIndex].isPlaying)
        {
            entireBgms[currentPlayingIndex].UnPause();
            Debug.Log($"Resumed BGM: {currentPlayingIndex}");
        }
        else
        {
            Debug.LogWarning("No paused BGM to resume!");
        }
    }

    /// <summary>
    /// 设置所有背景音乐的音量
    /// </summary>
    /// <param name="volume">音量值(0-1)</param>
    public void SetGlobalVolume()
    {
        if (entireBgms == null) return;

        // 确保音量在0-1范围内
        float clampedVolume = Mathf.Clamp01(soundVolume.value);
        initialVolume = clampedVolume;

        foreach (var bgm in entireBgms)
        {
            if (bgm != null)
            {
                bgm.volume = clampedVolume;
            }
        }
    }

    /// <summary>
    /// 获取当前播放的背景音乐索引
    /// </summary>
    /// <returns>当前索引(-1表示未播放)</returns>
    public int GetCurrentPlayingIndex()
    {
        return currentPlayingIndex;
    }

    /// <summary>
    /// 检查索引是否有效
    /// </summary>
    private bool IsValidIndex(int index)
    {
        return entireBgms != null && index >= 0 && index < entireBgms.Length && entireBgms[index] != null;
    }
}