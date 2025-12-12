using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    private AppOpenAd appOpenAd;
    private BannerView bannerView;
    private RewardedAd rewardedAd;
    private bool isInitialized = false;

    private bool hasShownFirstOpenAd = false;
    private bool isShowingFullScreenAd = false;

    private DateTime adExpireTime;
    private readonly TimeSpan APP_OPEN_AD_TIMEOUT = TimeSpan.FromHours(1);

    // 激励广告回调
    public event Action<bool> OnRewardedAdCompleted;
    public event Action<bool> OnTicketRewardedAdCompleted;
    public event Action<bool> OnReviveRewardedAdCompleted;

    [Header("调试模式")]
    public bool isDebugMode = false;

    [Header("UI 绑定")]
    [SerializeField] private Button closeBannerButton;

#if UNITY_ANDROID // 封闭式测试阶段-统一使用测试广告UnitID
    private const string APP_OPEN_ID = "ca-app-pub-3940256099942544/9257395921";
    private const string BANNER_ID   = "ca-app-pub-3940256099942544/9214589741";
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    /*
    private const string APP_OPEN_ID = "ca-app-pub-7029478247518346/1934227914";
    private const string BANNER_ID   = "ca-app-pub-7029478247518346/6168986392";
    private const string REWARDED_ID = "ca-app-pub-7029478247518346/4869057603";
    */
#else
    private const string APP_OPEN_ID = "ca-app-pub-3940256099942544/9257395921";
    private const string BANNER_ID   = "ca-app-pub-3940256099942544/9214589741";
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
#endif

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
    }

    private void Start()
    {
        if (isDebugMode)
            Debug.Log("[AdsManager] 调试模式：广告将被模拟跳过");

        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("[AdsManager] MobileAds 初始化完成");

            if (!isDebugMode)
            {
                LoadAppOpenAd();
                LoadRewardedAd();
            }
        });

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnregisterAppOpenAdEvents();
        appOpenAd?.Destroy();

        if (rewardedAd != null)
        {
            UnregisterRewardedAdEvents();
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    // 场景加载后显示 Banner（符合需求：两个场景都显示）
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindCloseButton();

        if (scene.name == "StartGameUI" || scene.name == "MainScene")
        {
            if (!isDebugMode)
                StartCoroutine(DelayShowBanner(0.5f));
        }
        else
        {
            HideBanner();
        }
    }

    private void BindCloseButton()
    {
        GameObject buttonObj = GameObject.Find("CloseBannerBtn");
        if (buttonObj != null)
        {
            closeBannerButton = buttonObj.GetComponent<Button>();
            if (closeBannerButton != null)
            {
                closeBannerButton.onClick.RemoveAllListeners();
                closeBannerButton.onClick.AddListener(HideBanner);
                closeBannerButton.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator DelayShowBanner(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowBanner();
    }

    #region AppOpen 开屏广告
    public void LoadAppOpenAd()
    {
        if (isDebugMode) return;
        if (!isInitialized) return;

        AppOpenAd.Load(APP_OPEN_ID, new AdRequest(), (loadedAd, loadError) =>
        {
            if (loadError != null || loadedAd == null)
            {
                Debug.LogWarning($"[AdsManager] AppOpen 加载失败：{loadError}");
                return;
            }

            appOpenAd = loadedAd;
            adExpireTime = DateTime.Now + APP_OPEN_AD_TIMEOUT;
            RegisterAppOpenAdEvents();
            Debug.Log("[AdsManager] AppOpen 加载成功");

            if (!hasShownFirstOpenAd)
            {
                ShowAppOpenAd();
                hasShownFirstOpenAd = true;
            }
        });
    }

    public void ShowAppOpenAd()
    {
        if (isDebugMode) return;

        if (appOpenAd == null) return;
        if (!appOpenAd.CanShowAd()) return;

        try
        {
            isShowingFullScreenAd = true;
            appOpenAd.Show();
            Debug.Log("[AdsManager] AppOpen 展示");
        }
        catch (Exception e)
        {
            Debug.LogError("[AdsManager] 展示开屏失败: " + e);
        }
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

    private void HandleAppOpenOpened()
    {
        Debug.Log("[AdsManager] AppOpen 已打开");
    }

    private void HandleAppOpenClosed()
    {
        Debug.Log("[AdsManager] AppOpen 已关闭");
        isShowingFullScreenAd = false;

        UnregisterAppOpenAdEvents();
        LoadAppOpenAd();
    }

    private void HandleAppOpenFailed(AdError error)
    {
        Debug.LogWarning("[AdsManager] AppOpen 展示失败：" + error);
        isShowingFullScreenAd = false;

        UnregisterAppOpenAdEvents();
        LoadAppOpenAd();
    }
    #endregion

    #region Banner 横幅（官方标准刷新+失败自动重试）
    public void ShowBanner()
    {
        if (isDebugMode) return;
        if (!isInitialized) return;

        // 若已存在旧 Banner（可能被关闭），销毁后重新创建
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        bannerView = new BannerView(BANNER_ID, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += HandleBannerLoaded;
        bannerView.OnBannerAdLoadFailed += HandleBannerFailed;

        bannerView.LoadAd(new AdRequest());
    }

    private void HandleBannerLoaded()
    {
        if (bannerView == null) return;

        bannerView.Show();

        if (closeBannerButton != null)
            closeBannerButton.gameObject.SetActive(true);
    }

    private void HandleBannerFailed(LoadAdError error)
    {
        Debug.LogWarning("[AdsManager] Banner 加载失败：" + error);
        if (closeBannerButton != null)
            closeBannerButton.gameObject.SetActive(false);

        // 合规：失败时延迟重试（非刷新）
        StartCoroutine(RetryBannerAfterDelay());
    }

    private IEnumerator RetryBannerAfterDelay()
    {
        yield return new WaitForSeconds(10f);

        if (bannerView == null)
            ShowBanner();
        else
            bannerView.LoadAd(new AdRequest());
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        if (closeBannerButton != null)
            closeBannerButton.gameObject.SetActive(false);
    }
    #endregion

    #region Rewarded 激励广告
    public void LoadRewardedAd()
    {
        if (isDebugMode) return;
        if (!isInitialized) return;

        if (rewardedAd != null)
        {
            UnregisterRewardedAdEvents();
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(REWARDED_ID, new AdRequest(), (loadedAd, loadError) =>
        {
            if (loadError != null || loadedAd == null)
            {
                Debug.LogWarning("[AdsManager] 激励广告加载失败：" + loadError);
                return;
            }

            rewardedAd = loadedAd;
            RegisterRewardedAdEvents();
            Debug.Log("[AdsManager] 激励广告加载成功");
        });
    }

    public bool ShowRewardedAd() => ShowRewardedInternal(false, false);
    public bool ShowTicketRewardedAd() => ShowRewardedInternal(false, true);
    public bool ShowReviveRewardedAd() => ShowRewardedInternal(true, false);

    private bool ShowRewardedInternal(bool isRevive, bool isTicket = false)
    {
        if (isDebugMode)
        {
            StartCoroutine(SimulateRewardCallback(isRevive, isTicket));
            return true;
        }

        if (rewardedAd == null)
        {
            Debug.LogWarning("[AdsManager] 激励广告未加载");
            LoadRewardedAd();
            return false;
        }

        try
        {
            rewardedAd.Show((reward) =>
            {
                if (isRevive) OnReviveRewardedAdCompleted?.Invoke(true);
                else if (isTicket) OnTicketRewardedAdCompleted?.Invoke(true);
                else OnRewardedAdCompleted?.Invoke(true);
            });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }
    }

    private IEnumerator SimulateRewardCallback(bool isRevive, bool isTicket)
    {
        yield return new WaitForSeconds(0.1f);

        if (isRevive) OnReviveRewardedAdCompleted?.Invoke(true);
        else if (isTicket) OnTicketRewardedAdCompleted?.Invoke(true);
        else OnRewardedAdCompleted?.Invoke(true);
    }

    // 注册 / 注销 激励广告事件
    private void RegisterRewardedAdEvents()
    {
        if (rewardedAd == null) return;

        rewardedAd.OnAdFullScreenContentClosed += HandleRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed += HandleRewardedFailed;
        rewardedAd.OnAdFullScreenContentOpened += HandleRewardedOpened;
        rewardedAd.OnAdImpressionRecorded += HandleRewardedImpression;
    }

    private void UnregisterRewardedAdEvents()
    {
        if (rewardedAd == null) return;

        rewardedAd.OnAdFullScreenContentClosed -= HandleRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed -= HandleRewardedFailed;
        rewardedAd.OnAdFullScreenContentOpened -= HandleRewardedOpened;
        rewardedAd.OnAdImpressionRecorded -= HandleRewardedImpression;
    }

    private void HandleRewardedOpened()
    {
        Debug.Log("[AdsManager] 激励广告已打开");
        isShowingFullScreenAd = true;
    }

    private void HandleRewardedImpression()
    {
        Debug.Log("[AdsManager] 激励广告展示记录");
    }

    private void HandleRewardedFailed(AdError error)
    {
        Debug.LogWarning("[AdsManager] 激励广告展示失败：" + error);
        OnRewardedAdCompleted?.Invoke(false);
        OnReviveRewardedAdCompleted?.Invoke(false);
        OnTicketRewardedAdCompleted?.Invoke(false);

        UnregisterRewardedAdEvents();
        LoadRewardedAd();
    }

    private void HandleRewardedClosed()
    {
        Debug.Log("[AdsManager] 激励广告已关闭");
        isShowingFullScreenAd = false;

        UnregisterRewardedAdEvents();
        LoadRewardedAd();
    }
    #endregion
}
