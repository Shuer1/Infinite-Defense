using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using TMPro;

public class SceneLoadManager : MonoBehaviour
{
    // 单例实例
    public static SceneLoadManager Instance { get; private set; }

    // 加载进度（0~1）
    public float LoadProgress { get; private set; } = 0;

    // 加载完成事件（可注册回调）
    public event Action OnLoadComplete;

    [Header("加载界面配置")]
    public GameObject loadingUI; // 加载时显示的UI面板
    public Slider progressBar;   // 进度条
    public TextMeshProUGUI progressText;    // 进度文字（百分比）

    private void Awake()
    {
        // 单例逻辑（确保全局唯一）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 切换场景时不销毁

        // 初始化加载UI（默认隐藏）
        if (loadingUI != null)
            loadingUI.SetActive(false);
    }

    public void LoadGameScene()
    {
        LoadSceneAsync("MainScene",true);
    }
    public void LoadSceneAsync(string sceneName, bool showLoadingUI = true)
    {
        // 显示加载界面
        if (loadingUI != null && showLoadingUI)
        {
            loadingUI.SetActive(true);
            progressBar.value = 0;
            progressText.text = "0%";
        }

        // 开始异步加载
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, showLoadingUI));
    }

    // 异步加载协程
    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool showLoadingUI)
    {
        LoadProgress = 0;

        // 异步加载场景（允许当前场景继续运行直到加载完成）
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = true; // 加载到90%时自动激活场景

        // 循环更新进度
        while (!asyncOp.isDone)
        {
            // 注意：asyncOp.progress在允许激活场景时会卡在0.9，完成后跳至1.0
            LoadProgress = Mathf.Clamp01(asyncOp.progress / 0.9f); // 转换为0~1范围

            // 更新UI
            if (showLoadingUI && loadingUI != null)
            {
                progressBar.value = LoadProgress;
                progressText.text = $"{(int)(LoadProgress * 100)}%";
            }

            yield return null; // 等待下一帧
        }

        // 加载完成：隐藏加载UI，触发回调
        if (loadingUI != null && showLoadingUI)
            loadingUI.SetActive(false);

        OnLoadComplete?.Invoke(); // 执行注册的完成逻辑
        LoadProgress = 0; // 重置进度
    }
}