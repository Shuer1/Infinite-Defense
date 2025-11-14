using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局背景音乐控制器（支持多场景、音量持久化、主场景滑块绑定）
/// </summary>
public class EntireBgmManager : MonoBehaviour
{
    public static EntireBgmManager Instance { get; private set; }

    [Header("背景音乐数组")]
    [SerializeField] private AudioSource[] entireBgms;

    [Header("初始播放设置")]
    [SerializeField] private int initialOrder = 0;
    [Range(0f, 0.8f)] public float initialVolume = 0.8f;

    [Header("主场景音量滑块设置")]
    [SerializeField] private string mainSceneName = "StartGameUI"; // 主场景名称
    [SerializeField] private string soundVolumeSliderName = "VolumeSlider"; // 滑块固定名
    private Slider soundVolume;
    private int currentPlayingIndex = -1;

    [Header("调试选项")]
    [SerializeField] private bool debugMode = true;

    public bool isPaused = false; // 是否处于暂停状态
    private float pausedTime = 0f; // 记录暂停时播放进度

    private void Awake()
    {
        // 单例模式，防止重复实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeBgms();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 如果当前场景已是主场景，尝试查找滑块
        if (SceneManager.GetActiveScene().name == mainSceneName)
        {
            FindSoundVolumeSlider();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindSliderEvent();
    }

    private void Start()
    {
        float lastestVolume = DataManager.GetFloat(DataManager.LastestVolumeKey, 1);
        if (soundVolume != null)
        {
            soundVolume.value = lastestVolume;
        }
        else
        {
            LogDebug("Start: 未找到滑块，稍后将在场景加载时绑定。");
        }

        PlayBgm(initialOrder);
    }

    /// <summary>
    /// 场景加载后处理：仅主场景查找滑块
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindSliderEvent(); // 先解绑之前的滑块（防内存泄漏）

        if (scene.name == mainSceneName)
        {
            FindSoundVolumeSlider(); // 主场景查找并绑定
        }
    }

    /// <summary>
    /// 查找主场景中的音量滑块（包括未激活物体）
    /// </summary>
    private void FindSoundVolumeSlider()
    {
        if (soundVolume != null)
        {
            LogDebug("滑块已绑定，无需重复查找。");
            return;
        }

        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            Slider targetSlider = FindSliderInChildren(root.transform, soundVolumeSliderName);
            if (targetSlider != null)
            {
                soundVolume = targetSlider;
                BindSliderEvent();
                return;
            }
        }

        Debug.LogWarning($"[EntireBgmManager] 主场景未找到名为 {soundVolumeSliderName} 的滑块（请检查名称和层级）");
    }

    /// <summary>
    /// 遍历所有子物体查找目标滑块（包含未激活对象）
    /// </summary>
    private Slider FindSliderInChildren(Transform parent, string targetName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) // true 包含未激活对象
        {
            if (child.name == targetName && child.TryGetComponent(out Slider slider))
            {
                return slider;
            }
        }
        return null;
    }

    /// <summary>
    /// 绑定滑块事件（同步当前音量值）
    /// </summary>
    private void BindSliderEvent()
    {
        if (soundVolume == null) return;

        soundVolume.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        soundVolume.onValueChanged.AddListener(OnVolumeSliderChanged);

        soundVolume.value = initialVolume;
        LogDebug("已绑定音量滑块事件。");
    }

    /// <summary>
    /// 解绑滑块事件（避免内存泄漏）
    /// </summary>
    private void UnbindSliderEvent()
    {
        if (soundVolume != null)
        {
            soundVolume.onValueChanged.RemoveListener(OnVolumeSliderChanged);
            soundVolume = null;
        }
    }

    /// <summary>
    /// 滑块值变化时更新音量
    /// </summary>
    private void OnVolumeSliderChanged(float value)
    {
        SetGlobalVolume(value);
        DataManager.SaveFloatForce(DataManager.LastestVolumeKey, value);
        LogDebug($"滑块调节音量：{value}");
        // TODO: 可考虑未来增加延时保存（0.3秒后写入），避免频繁写入PlayerPrefs
    }

    /// <summary>
    /// 初始化所有BGM
    /// </summary>
    private void InitializeBgms()
    {
        if (entireBgms == null || entireBgms.Length == 0)
        {
            Debug.LogWarning("未分配背景音乐数组！");
            return;
        }

        foreach (var bgm in entireBgms)
        {
            if (bgm != null)
            {
                bgm.volume = initialVolume;
                bgm.playOnAwake = false;
                bgm.loop = true;
            }
        }
        LogDebug("所有BGM初始化完成。");
    }

    /// <summary>
    /// 播放指定索引的BGM
    /// </summary>
    public void PlayBgm(int index)
    {
        if (entireBgms == null || entireBgms.Length == 0)
        {
            Debug.LogError("BGM数组未初始化！");
            return;
        }

        if (index < 0 || index >= entireBgms.Length)
        {
            Debug.LogError($"无效BGM索引：{index}（有效范围 0-{entireBgms.Length - 1}）");
            return;
        }

        if (currentPlayingIndex != -1 && currentPlayingIndex < entireBgms.Length)
        {
            entireBgms[currentPlayingIndex].Stop();
        }

        if (entireBgms[index] != null)
        {
            entireBgms[index].Play();
            currentPlayingIndex = index;
            isPaused = false;
            pausedTime = 0f;
            LogDebug($"正在播放BGM索引 {index}");
        }
        else
        {
            Debug.LogError($"索引 {index} 的BGM为空！");
            currentPlayingIndex = -1;
        }
    }

    /// <summary>
    /// 设置全局音量（0~1）
    /// </summary>
    private void SetGlobalVolume(float volume)
    {
        if (entireBgms == null) return;

        float clampedVolume = Mathf.Clamp01(volume);
        initialVolume = clampedVolume;

        foreach (var bgm in entireBgms)
        {
            if (bgm != null)
            {
                bgm.volume = clampedVolume;
            }
        }

        LogDebug($"全局音量设置为 {clampedVolume}");
    }

    // -------------------- 控制方法完善区 --------------------

    /// <summary>
    /// 停止当前播放的BGM
    /// </summary>
    public void StopCurrentBgm()
    {
        if (currentPlayingIndex >= 0 && currentPlayingIndex < entireBgms.Length)
        {
            entireBgms[currentPlayingIndex].Stop();
            LogDebug("已停止当前BGM播放。");
        }

        currentPlayingIndex = -1;
        isPaused = false;
        pausedTime = 0f;
    }

    /// <summary>
    /// 暂停当前播放的BGM（记录暂停时间）
    /// </summary>
    public void PauseCurrentBgm()
    {
        if (isPaused)
        {
            LogDebug("BGM已处于暂停状态。");
            return;
        }

        if (currentPlayingIndex >= 0 && currentPlayingIndex < entireBgms.Length)
        {
            AudioSource current = entireBgms[currentPlayingIndex];
            if (current.isPlaying)
            {
                pausedTime = current.time;
                current.Pause();
                isPaused = true;
                LogDebug($"已暂停BGM（索引 {currentPlayingIndex}，时间 {pausedTime:F2}s）");
            }
        }
    }

    /// <summary>
    /// 恢复当前播放的BGM（从暂停处继续）
    /// </summary>
    public void ResumeCurrentBgm()
    {
        if (!isPaused)
        {
            LogDebug("当前未处于暂停状态，无法恢复。");
            return;
        }

        if (currentPlayingIndex >= 0 && currentPlayingIndex < entireBgms.Length)
        {
            AudioSource current = entireBgms[currentPlayingIndex];
            current.time = pausedTime;
            current.Play();
            isPaused = false;
            LogDebug($"已恢复BGM（索引 {currentPlayingIndex}，从 {pausedTime:F2}s）");
        }
    }

    /// <summary>
    /// 获取当前播放索引
    /// </summary>
    public int GetCurrentPlayingIndex() => currentPlayingIndex;

    // -------------------- 工具函数 --------------------
    private void LogDebug(string msg)
    {
        if (debugMode)
        {
            Debug.Log($"[EntireBgmManager] {msg}");
        }
    }
}
