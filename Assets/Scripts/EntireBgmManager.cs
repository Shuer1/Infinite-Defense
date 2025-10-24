using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EntireBgmManager : MonoBehaviour
{
    public static EntireBgmManager Instance { get; private set; }

    [Header("背景音乐数组")]
    [SerializeField] private AudioSource[] entireBgms;

    [Header("初始播放设置")]
    [SerializeField] private int initialOrder = 0;
    public float initialVolume = 1f;
    
    [Header("主场景音量滑块设置")]
    [SerializeField] private string mainSceneName = "StartGameUI"; // 主场景名称
    [SerializeField] private string soundVolumeSliderName = "VolumeSlider"; // 滑块固定名为VolumeSlider
    private Slider soundVolume;
    private int currentPlayingIndex = -1;

    private void Awake()
    {
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
        // 初始场景如果是主场景，尝试查找滑块
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
        PlayBgm(initialOrder);
    }

    /// <summary>
    /// 场景加载后处理：仅主场景查找滑块
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindSliderEvent(); // 先解绑之前的滑块

        if (scene.name == mainSceneName)
        {
            FindSoundVolumeSlider(); // 主场景查找并绑定
        }
    }

    /// <summary>
    /// 递归查找所有物体（包括子层级和未激活物体）中的目标滑块
    /// </summary>
    private void FindSoundVolumeSlider()
    {
        // 获取场景中所有根物体（包括未激活的）
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            // 递归查找子物体中的Slider
            Slider targetSlider = FindSliderInChildren(root.transform, soundVolumeSliderName);
            if (targetSlider != null)
            {
                soundVolume = targetSlider;
                BindSliderEvent();
                Debug.Log($"主场景找到滑块（子层级/未激活）：{soundVolume.name}，路径：{GetTransformPath(soundVolume.transform)}");
                return;
            }
        }
        Debug.LogWarning($"主场景未找到名为 {soundVolumeSliderName} 的滑块（检查名称和层级）");
    }

    /// <summary>
    /// 递归遍历子物体查找Slider（支持未激活物体）
    /// </summary>
    private Slider FindSliderInChildren(Transform parent, string targetName)
    {
        // 检查当前物体（即使未激活）
        if (parent.gameObject.TryGetComponent<Slider>(out Slider slider) && parent.name == targetName)
        {
            return slider;
        }

        // 递归检查所有子物体
        for (int i = 0; i < parent.childCount; i++)
        {
            Slider childSlider = FindSliderInChildren(parent.GetChild(i), targetName);
            if (childSlider != null)
            {
                return childSlider;
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

        // 先移除旧事件，避免重复绑定
        soundVolume.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        soundVolume.onValueChanged.AddListener(OnVolumeSliderChanged);

        // 同步滑块值与当前音量（即使滑块未激活，值也会被正确设置）
        soundVolume.value = initialVolume;
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
    }

    // 以下为原有核心逻辑（保持不变）
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
    }

    public void PlayBgm(int index)
    {
        if (entireBgms == null || entireBgms.Length == 0)
        {
            Debug.LogError("BGM数组未初始化！");
            return;
        }

        if (index < 0 || index >= entireBgms.Length)
        {
            Debug.LogError($"无效BGM索引：{index}（有效范围0-{entireBgms.Length - 1}）");
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
        }
        else
        {
            Debug.LogError($"索引{index}的BGM为空！");
            currentPlayingIndex = -1;
        }
    }

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
    }

    // 工具方法：获取Transform的完整路径（用于调试）
    private string GetTransformPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }
        return path;
    }

    // 原有控制方法（保持不变）
    public void StopCurrentBgm() { /* 不变 */ }
    public void PauseCurrentBgm() { /* 不变 */ }
    public void ResumeCurrentBgm() { /* 不变 */ }
    public int GetCurrentPlayingIndex() { return currentPlayingIndex; }
    
}