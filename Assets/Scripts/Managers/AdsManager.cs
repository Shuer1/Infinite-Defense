using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class AdsManager : MonoBehaviour //单例
{
    public static AdsManager Instance;

    private AppOpenAd appOpenAd;
    private BannerView bannerView;
    private RewardedAd rewardedAd;
    private bool isInitialized = false;
    private bool hasShownFirstOpenAd = false;
    private bool isShowingAppOpenAd = false;
    private bool isShowingFullScreenAd = false;
    private DateTime adExpireTime;
    private readonly TimeSpan APP_OPEN_AD_TIMEOUT = TimeSpan.FromHours(1);

    private Coroutine bannerRefreshRoutine;
    private int bannerRefreshCount = 0;

    // 新增：激励广告回调
    public event Action<bool> OnRewardedAdCompleted;
    // 新增：用于升级券的广告回调
    public event Action<bool> OnTicketRewardedAdCompleted;
    // 新增：用于复活的激励广告回调
    public event Action<bool> OnReviveRewardedAdCompleted;

    [Header("调试模式")]
    public bool isDebugMode = false;

    [Header("UI 绑定")]
    [SerializeField] private Button closeBannerButton;
    public const string mainSceneName = "UIScene";

#if UNITY_ANDROID
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    //此分支为编辑器内调试：使用测试广告ID
    private const string APP_OPEN_ID = "ca-app-pub-3940256099942544/9257395921";
    private const string BANNER_ID   = "ca-app-pub-3940256099942544/6300978111";
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    #else
    //此分支为Android：发布前需替换为真实广告ID（已替换为正式AdUnitID）
    private const string APP_OPEN_ID = "ca-app-pub-7029478247518346/1934227914";
    private const string BANNER_ID   = "ca-app-pub-7029478247518346/6168986392";
    private const string REWARDED_ID = "ca-app-pub-7029478247518346/4869057603";
    #endif
#else
    private const string APP_OPEN_ID = "";
    private const string BANNER_ID   = "";
    private const string REWARDED_ID = "";
#endif

/*
#if UNITY_WEBGL && !UNITY_EDITOR  // WebGL 端获取 WALL 用户信息 新增✅
    [DllImport("__Internal")] private static extern string GetWallUser();
#endif
    private static string wallUserJson = "{}";
*/

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject);
        }
        else 
        { 
            Destroy(gameObject); 
        }

        /*
        #if UNITY_WEBGL && !UNITY_EDITOR // WebGL 端获取 WALL 用户信息 新增✅
            wallUserJson = GetWallUser();
        #endif
        */
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (isDebugMode)
        {
            Debug.LogWarning("[AdsManager] 🚧 调试模式已启用, 广告将被跳过/隐藏, 激励奖励直接发放！");
        }

        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("[AdsManager] ✅ MobileAds SDK 初始化完成。");

            if (!isDebugMode)
            {
                LoadAppOpenAd();
                LoadRewardedAd(); // 新增：初始化后加载激励广告
            }  
        });

        // 初始绑定按钮（首次加载场景时）
        BindCloseButton();
    }

    // 场景加载完成后触发
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AdsManager] 场景 {scene.name} 加载完成，准备重建横幅广告");

        if (!isDebugMode)
        {
            // 销毁旧横幅并重新创建
            DestroyBanner();
            // 重新绑定当前场景的关闭按钮
            BindCloseButton();
            // 延迟显示横幅
            StartCoroutine(DelayShowBanner(0.5f));
        }
        else
        {
            if (closeBannerButton != null)
                closeBannerButton.gameObject.SetActive(false);
        }
    }

    private void BindCloseButton()
    {
        GameObject buttonObj = GameObject.Find("AdsManager/BannerCanvas/CloseBannerBtn");
        if (buttonObj != null)
        {
            closeBannerButton = buttonObj.GetComponent<Button>();
            if (closeBannerButton != null)
            {
                closeBannerButton.onClick.RemoveAllListeners();
                closeBannerButton.onClick.AddListener(HideBanner);
                closeBannerButton.gameObject.SetActive(false);
                Debug.Log("[AdsManager] 成功绑定当前场景的关闭按钮");
            }
            else
            {
                Debug.LogWarning("[AdsManager] 找到按钮对象，但未获取到Button组件");
            }
        }
        else
        {
            Debug.LogWarning("[AdsManager] 未找到关闭按钮对象，请检查场景中是否存在名称为'CloseBannerButton'的按钮");
            closeBannerButton = null;
        }
    }

    private IEnumerator DelayShowBanner(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowBanner();
    }

    #region AppOpen (开屏广告)
    // 开屏广告代码保持不变
    public void LoadAppOpenAd()
    {
        if (isDebugMode)
        {
            Debug.Log("[AdsManager] 🚧 调试模式已启用，跳过加载 AppOpen");
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] SDK 未初始化，跳过加载 AppOpen");
            return;
        }

        AdRequest request = new AdRequest();

        AppOpenAd.Load(APP_OPEN_ID, request, (loadedAd, loadError) =>
        {
            if (loadError != null || loadedAd == null)
            {
                Debug.LogWarning($"[AdsManager] AppOpen 加载失败: {loadError?.ToString()}，30s后重试。");
                StartCoroutine(RetryLoadAppOpenAd(30f));
                return;
            }

            appOpenAd = loadedAd;
            adExpireTime = DateTime.Now + APP_OPEN_AD_TIMEOUT;
            RegisterAppOpenAdEvents();
            Debug.Log($"[AdsManager] ✅ AppOpen 加载成功，有效期至 {adExpireTime:HH:mm:ss}");

            if (!hasShownFirstOpenAd)
            {
                ShowAppOpenAd();
                hasShownFirstOpenAd = true;
            }
        });
    }

    public void ShowAppOpenAd()
    {
        if (isDebugMode) // 新增✅
        {
            Debug.Log("[AdsManager] 🚧 调试模式已启用，跳过展示 AppOpen");
            return;
        }

        if (appOpenAd == null)
        {
            Debug.LogWarning("[AdsManager] 无可用 AppOpenAd，尝试重新加载");
            LoadAppOpenAd();
            return;
        }

        if (isShowingAppOpenAd)
        {
            Debug.Log("[AdsManager] AppOpen 正在展示中，忽略重复请求");
            return;
        }

        if (!appOpenAd.CanShowAd() || DateTime.Now >= adExpireTime)
        {
            Debug.Log("[AdsManager] AppOpen 已失效或不可展示，重新加载");
            LoadAppOpenAd();
            return;
        }

        isShowingAppOpenAd = true;
        isShowingFullScreenAd = true;
        try
        {
            appOpenAd.Show();
            Debug.Log("[AdsManager] 🚀 AppOpen 开始展示");
        }
        catch (Exception ex)
        {
            Debug.LogError("[AdsManager] 展示 AppOpen 发生异常: " + ex);
            isShowingAppOpenAd = false;
            LoadAppOpenAd();
            ShowBanner();
        }
    }

    private IEnumerator RetryLoadAppOpenAd(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        LoadAppOpenAd();
    }
    #endregion

    #region Banner (横幅广告)
    // 横幅广告代码保持不变
    public void ShowBanner()
    {
        if (isDebugMode) // 新增✅
        {
            Debug.Log("[AdsManager] 🚧 调试模式已启用，跳过加载 Banner");
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] SDK 未初始化，无法加载横幅");
            return;
        }

        if (bannerView == null)
        {
            bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);
            bannerView.OnBannerAdLoaded += HandleBannerLoaded;
            bannerView.OnBannerAdLoadFailed += HandleBannerFailed;
        }

        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);

        bannerRefreshCount++;

        if (bannerRefreshRoutine == null)
        {
            bannerRefreshRoutine = StartCoroutine(BannerAutoRefreshRoutine());
        }
    }

    private IEnumerator BannerAutoRefreshRoutine()
    {
        const float REFRESH_INTERVAL = 45f;
        while (true)
        {
            yield return new WaitForSeconds(REFRESH_INTERVAL);
            if (bannerView != null && !isDebugMode) // 新增✅
            {
                Debug.Log($"[AdsManager] 🔁 自动刷新横幅广告");
                bannerView.LoadAd(new AdRequest());
                bannerRefreshCount++;
            }
        }
    }

    private void HandleBannerLoaded()
    {
        Debug.Log($"[AdsManager] ✅ 横幅加载成功（第 {bannerRefreshCount} 次），准备显示");
        if (bannerView != null)
        {
            bannerView.Show();
        }
        if (closeBannerButton != null)
        {
            closeBannerButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[AdsManager] 关闭按钮引用无效，无法显示按钮");
        }
    }

    private void HandleBannerFailed(LoadAdError error)
    {
        Debug.LogWarning($"[AdsManager] ❌ 横幅加载失败: {error?.ToString()}");
        if (closeBannerButton != null)
        {
            closeBannerButton.gameObject.SetActive(false);
        }
        StartCoroutine(RetryLoadBanner(60f));
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
        if (closeBannerButton != null)
        {
            closeBannerButton.gameObject.SetActive(false);
        }
        Debug.Log("[AdsManager] 🟨 横幅隐藏");
    }

    public void DestroyBanner()
    {
        if (bannerRefreshRoutine != null)
        {
            StopCoroutine(bannerRefreshRoutine);
            bannerRefreshRoutine = null;
        }

        if (bannerView != null)
        {
            bannerView.OnBannerAdLoaded -= HandleBannerLoaded;
            bannerView.OnBannerAdLoadFailed -= HandleBannerFailed;
            bannerView.Destroy();
            bannerView = null;
        }

        if (closeBannerButton != null)
        {
            closeBannerButton.gameObject.SetActive(false);
        }
        Debug.Log("[AdsManager] 🧹 横幅销毁完毕");
    }

    private IEnumerator RetryLoadBanner(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        ShowBanner();
    }
    #endregion

    #region Rewarded (激励奖励广告)
    // 新增：激励广告相关方法
    /// <summary>
    /// 加载激励广告
    /// </summary>
    public void LoadRewardedAd()
    {
        if (isDebugMode) // 新增✅
        {
            Debug.Log("[AdsManager] 🚧 调试模式已启用，跳过加载激励");
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] SDK 未初始化，无法加载激励广告");
            return;
        }

        // 清除现有广告引用
        if (rewardedAd != null)
        {
            UnregisterRewardedAdEvents();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();
        
        RewardedAd.Load(REWARDED_ID, request, (loadedAd, loadError) =>
        {
            if (loadError != null || loadedAd == null)
            {
                Debug.LogWarning($"[AdsManager] ❌ 激励广告加载失败: {loadError?.ToString()}，10s后重试");
                StartCoroutine(RetryLoadRewardedAd(10f));
                return;
            }

            rewardedAd = loadedAd;
            RegisterRewardedAdEvents();
            Debug.Log("[AdsManager] ✅ 激励广告加载成功");
        });
    }

    /// <summary>
    /// 显示激励广告（用于道具奖励）
    /// </summary>
    /// <returns>是否成功调用显示</returns>
    public bool ShowRewardedAd()
    {
        return ShowRewardedAdInternal(false);
    }

    /// <summary>
    /// 显示激励广告（用于升级券奖励）
    /// </summary>
    /// <returns>是否成功调用显示</returns>
    public bool ShowTicketRewardedAd()
    {
        return ShowRewardedAdInternal(false, true);
    }

    /// <summary>
    /// 显示激励广告（用于复活）
    /// </summary>
    /// <returns>是否成功调用显示</returns>
    public bool ShowReviveRewardedAd()
    {
        return ShowRewardedAdInternal(true);
    }

    /// <summary>
    /// 显示激励广告的内部实现
    /// </summary>
    /// <param name="isForRevive">是否用于复活</param>
    /// <returns>是否成功调用显示</returns>
    private bool ShowRewardedAdInternal(bool isForRevive, bool isForTicket = false)
    {
        if (isDebugMode) // 新增✅
        {
            Debug.Log("[AdsManager] 🚧 调试模式：直接发放奖励！");
            StartCoroutine(DelayTriggerRewardCallback(isForRevive, isForTicket));
            isShowingFullScreenAd = false;
            return true;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] SDK 未初始化，无法显示激励广告");
            return false;
        }

        if (rewardedAd == null)
        {
            Debug.LogWarning("[AdsManager] 激励广告未加载完成，尝试重新加载");
            LoadRewardedAd();
            return false;
        }

        try
        {
            isShowingFullScreenAd = true;

            rewardedAd.Show((reward) => 
            {
                if (isForRevive)
                {
                    OnReviveRewardedAdCompleted?.Invoke(true);
                }
                else if (isForTicket)
                {
                    OnTicketRewardedAdCompleted?.Invoke(true);
                }
                else
                {
                    OnRewardedAdCompleted?.Invoke(true);
                }
            });
            Debug.Log("[AdsManager] 🚀 激励广告开始展示");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AdsManager] 显示激励广告发生异常: " + ex);

            isShowingFullScreenAd = false;

            if (isForRevive)
            {
                OnReviveRewardedAdCompleted?.Invoke(false);
            }
            else if (isForTicket)
            {
                OnTicketRewardedAdCompleted?.Invoke(false);
            }
            else
            {
                OnRewardedAdCompleted?.Invoke(false);
            }
            LoadRewardedAd();
            return false;
        }
    }

    // 新增：延迟触发奖励回调（模拟真实广告流程）✅
    private IEnumerator DelayTriggerRewardCallback(bool isForRevive, bool isForTicket)
    {
        yield return new WaitForSeconds(0.1f);
        
        if (isForRevive)
        {
            OnReviveRewardedAdCompleted?.Invoke(true);
        }
        else if (isForTicket)
        {
            OnTicketRewardedAdCompleted?.Invoke(true);
        }
        else
        {
            OnRewardedAdCompleted?.Invoke(true);
        }
    }

    private IEnumerator RetryLoadRewardedAd(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        LoadRewardedAd();
    }

    private void RegisterRewardedAdEvents()
    {
        if (rewardedAd == null) return;
        
        rewardedAd.OnAdFullScreenContentClosed += HandleRewardedAdClosed;
        rewardedAd.OnAdFullScreenContentFailed += HandleRewardedAdFailed;
        rewardedAd.OnAdFullScreenContentOpened += HandleRewardedAdOpened;
        rewardedAd.OnAdImpressionRecorded += HandleRewardedAdImpression;
    }

    private void UnregisterRewardedAdEvents()
    {
        if (rewardedAd == null) return;
        
        rewardedAd.OnAdFullScreenContentClosed -= HandleRewardedAdClosed;
        rewardedAd.OnAdFullScreenContentFailed -= HandleRewardedAdFailed;
        rewardedAd.OnAdFullScreenContentOpened -= HandleRewardedAdOpened;
        rewardedAd.OnAdImpressionRecorded -= HandleRewardedAdImpression;
    }

    // 激励广告事件处理
    private void HandleRewardedAdOpened()
    {
        Debug.Log("[AdsManager] 激励广告已打开");
        isShowingFullScreenAd = true; //保险操作
    }
    private void HandleRewardedAdImpression() => Debug.Log("[AdsManager] 激励广告展示记录");
    private void HandleRewardedAdFailed(AdError error)
    {
        Debug.LogWarning($"[AdsManager] 激励广告展示失败: {error?.ToString()}");
        // 通知两个可能的监听者广告失败
        OnRewardedAdCompleted?.Invoke(false);
        OnReviveRewardedAdCompleted?.Invoke(false);
        OnTicketRewardedAdCompleted?.Invoke(false);

        isShowingFullScreenAd = false;

        UnregisterRewardedAdEvents();
        LoadRewardedAd(); // 失败后重新加载
    }
    private void HandleRewardedAdClosed()
    {
        Debug.Log("[AdsManager] 激励广告已关闭");

        isShowingFullScreenAd = false;

        UnregisterRewardedAdEvents();
        LoadRewardedAd(); // 关闭后立即加载新的
    }
    #endregion

    #region 生命周期 / 清理
    private void OnApplicationPause(bool isPaused)
    {
        if (isDebugMode) return;

        if (isPaused)
        {
            Debug.Log("[AdsManager] 应用进入后台，检查开屏");
            if (!isShowingFullScreenAd && hasShownFirstOpenAd)
            {
                ShowAppOpenAd();
            }
            else
            {
                Debug.Log("[AdsManager] 跳过开屏展示（当前正显示或刚显示完全屏广告）");
            }
        }
    }

    private void OnDestroy()
    {
        UnregisterAppOpenAdEvents();
        appOpenAd?.Destroy();
        
        // 新增：清理激励广告
        if (rewardedAd != null)
        {
            UnregisterRewardedAdEvents();
            rewardedAd.Destroy();
        }
        
        DestroyBanner();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[AdsManager] 🧩 资源释放完成");
    }

    private void RegisterAppOpenAdEvents()
    {
        if (appOpenAd == null) return;
        appOpenAd.OnAdFullScreenContentClosed += HandleAppOpenClosed;
        appOpenAd.OnAdFullScreenContentFailed += HandleAppOpenFailed;
        appOpenAd.OnAdFullScreenContentOpened += HandleAppOpenOpened;
    }

    private void UnregisterAppOpenAdEvents()
    {
        if (appOpenAd == null) return;
        appOpenAd.OnAdFullScreenContentClosed -= HandleAppOpenClosed;
        appOpenAd.OnAdFullScreenContentFailed -= HandleAppOpenFailed;
        appOpenAd.OnAdFullScreenContentOpened -= HandleAppOpenOpened;
    }

    private void HandleAppOpenOpened() => Debug.Log("[AdsManager] AppOpen 已打开");

    private void HandleAppOpenClosed()
    {
        Debug.Log("[AdsManager] AppOpen 已关闭 -> 显示横幅");
        isShowingAppOpenAd = false;
        isShowingFullScreenAd = false;//✅新增
        UnregisterAppOpenAdEvents();

        if (!isDebugMode)
        {
            ShowBanner();
            LoadAppOpenAd();
        }
    }

    private void HandleAppOpenFailed(AdError error)
    {
        Debug.LogWarning($"[AdsManager] AppOpen 展示失败: {error?.ToString()}");
        isShowingAppOpenAd = false;
        isShowingFullScreenAd = false;//✅新增
        UnregisterAppOpenAdEvents();

        if (!isDebugMode)
        {
            ShowBanner();
            LoadAppOpenAd();
        }
    }
    #endregion
    
    /*
    #region 统一“任务完成”处理
    public void ReportMission(string taskType,int stage=0) // 新增✅,并将访问修饰符设为public
    {
        var u = JsonUtility.FromJson<WallUser>(wallUserJson);
        var body = new {
            userId = u.userId,
            token  = u.token,
            taskId = u.taskId,
            taskType,
            stage
        };
        string json = JsonUtility.ToJson(body);
        #if UNITY_WEBGL && !UNITY_EDITOR
            NotifyTaskComplete(json);   // 调 JS
        #else
            Debug.Log("[ReportMission] "+json);
        #endif
    }
    [Serializable] private class WallUser{public string userId,token,taskId;}
    #endregion
    */
}