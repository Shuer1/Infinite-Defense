using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoadManager : MonoBehaviour
{
    // 单例实例
    public static SceneLoadManager Instance { get; private set; }
    public static bool pauseAfterLoad = false; //加载后保持暂停

    // 加载进度（0~1）
    public float LoadProgress { get; private set; } = 0;

    // 加载完成事件（可注册回调）
    public event Action OnLoadComplete;

    [Header("加载界面配置")]
    public GameObject loadingUI; // 加载时显示的UI面板
    public Slider progressBar;   // 进度条
    public TextMeshProUGUI progressText;    // 进度文字（百分比）
    public Image blackMask; // 用于渐变到全黑的遮罩图片（需设置为黑色）
    public float fadeToBlackDuration = 1.5f; // 渐变到全黑的持续时间（秒）

    private bool _isFadeToBlackComplete; // 标记是否已完全变黑
    private AsyncOperation _currentAsyncOp; // 当前异步加载操作
    private float maxValue;

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

        maxValue = progressBar.maxValue;
        // 初始化加载UI（默认隐藏）
        if (loadingUI != null)
        {
            loadingUI.SetActive(false);

            loadingUI.transform.SetParent(null);
            DontDestroyOnLoad(loadingUI);
        }

        // 初始化黑色遮罩
        if (blackMask != null)
        {
            Color maskColor = blackMask.color;
            maskColor.a = 0; // 初始完全透明
            blackMask.color = maskColor;
        }
    }

    public void LoadGameScene()
    {
        LoadSceneAsync("MainScene", true, true);
    }

    public void LoadStartScene()
    {
        SceneManager.LoadScene(0); // 开始界面场景序号是0
    }

    public void ExitGame() //判断运行环境
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
            Application.Quit();
#endif
    }

    public void LoadSceneAsync(string sceneName, bool showLoadingUI = true, bool keepPause = false)
    {
        pauseAfterLoad = keepPause; //新增✅
        // 重置状态
        _isFadeToBlackComplete = false;
        //Time.timeScale = 1f;

        // 显示加载界面
        if (loadingUI != null && showLoadingUI)
        {
            loadingUI.SetActive(true);
            progressBar.value = 0;
            progressText.text = "Loading : 0%";

            // 重置遮罩透明度
            if (blackMask != null)
            {
                Color maskColor = blackMask.color;
                maskColor.a = 0;
                blackMask.color = maskColor;
            }
        }

        // 开始异步加载和渐黑效果（并行执行）
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, showLoadingUI));
        if (showLoadingUI && blackMask != null)
        {
            StartCoroutine(FadeToBlackCoroutine());
        }
    }

    // 异步加载协程
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool showLoadingUI)
    {
        LoadProgress = 0;

        // 异步加载场景（先不自动激活）
        _currentAsyncOp = SceneManager.LoadSceneAsync(sceneName);
        _currentAsyncOp.allowSceneActivation = false; // 加载到90%时暂停

        // 循环更新进度
        while (_currentAsyncOp.progress < 0.9f)
        {
            LoadProgress = Mathf.Clamp01(_currentAsyncOp.progress / 0.9f); // 转换为0~1范围

            // 更新UI
            if (showLoadingUI && loadingUI != null)
            {
                progressBar.value = LoadProgress;
                progressText.text = $"Loading : {(int)(LoadProgress * 100)}%";
            }

            yield return null; // 等待下一帧
        }

        // 加载到90%后，等待渐黑效果完成
        LoadProgress = maxValue;
        if (showLoadingUI && loadingUI != null)
        {
            progressBar.value = maxValue;
            progressText.text = "Loading : 100%";
        }

        if (blackMask == null)
        {
            _isFadeToBlackComplete = true;
        }

        // 等待遮罩完全变黑
        while (!_isFadeToBlackComplete)
        {
            yield return null;
        }

        // 激活场景
        _currentAsyncOp.allowSceneActivation = true;

        // 等待场景完全激活
        while (!_currentAsyncOp.isDone)
        {
            yield return null;
        }

        if (sceneName == "StartGameUI")
        {
            pauseAfterLoad = false;
        }

        if (pauseAfterLoad) //新增✅
        {
            // 暂停游戏
            Time.timeScale = 0f;
            Debug.Log("保持暂停状态");
        }

        // 加载完成：隐藏加载UI，触发回调
        if (loadingUI != null && showLoadingUI)
            loadingUI.SetActive(false);

        OnLoadComplete?.Invoke(); // 执行注册的完成逻辑
        LoadProgress = 0; // 重置进度
    }

    // 渐变为全黑的协程
    private IEnumerator FadeToBlackCoroutine()
    {
        if (blackMask == null) yield break;

        float elapsedTime = 0;
        Color originalColor = blackMask.color;
        originalColor.a = 0; // 开始时透明
        Color targetColor = originalColor;
        targetColor.a = 1; // 结束时全黑

        while (elapsedTime < fadeToBlackDuration)
        {
            //elapsedTime += Time.deltaTime;
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeToBlackDuration);
            blackMask.color = Color.Lerp(originalColor, targetColor, t);
            yield return null;
        }

        // 确保完全变黑
        blackMask.color = targetColor;
        _isFadeToBlackComplete = true;
    }
}