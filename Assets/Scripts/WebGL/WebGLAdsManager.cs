using System;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_WEBGL || UNITY_EDITOR
public class WebGLAdsManager : MonoBehaviour
{
    public static WebGLAdsManager Instance;

    [Header("调试模式")]
    public bool isDebugMode = false;

    // 事件签名保持与 Android 端一致
    public event Action<bool> OnRewardedAdCompleted;
    public event Action<bool> OnTicketRewardedAdCompleted;
    public event Action<bool> OnReviveRewardedAdCompleted;

    private bool rewardedSuccess;
    private bool rewardedForRevive;
    private bool rewardedForTicket;

    // JS 函数声明
    [DllImport("__Internal")] private static extern void WebGLAdsInit();
    [DllImport("__Internal")] private static extern void WebGLAdsShowOpen();
    [DllImport("__Internal")] private static extern void WebGLAdsShowBanner();
    [DllImport("__Internal")] private static extern void WebGLAdsShowRewarded();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
        WebGLAdsInit();
        // 延迟 0.5s 弹出开屏，等场景加载完毕
        StartCoroutine(DelayOpenAd(0.5f));
    }

    private IEnumerator DelayOpenAd(float t)
    {
        yield return new WaitForSeconds(t);
        ShowAppOpenAd();
    }

    #region 与 RewardManager 完全一致的对外接口
    public void ShowAppOpenAd()
    {
        if (isDebugMode) return;
        WebGLAdsShowOpen();
    }

    public void ShowBanner()
    {
        if (isDebugMode) return;
        WebGLAdsShowBanner();
    }

    public bool ShowRewardedAd()       => ShowRewardedAdInternal(false, false);
    public bool ShowTicketRewardedAd() => ShowRewardedAdInternal(false, true);
    public bool ShowReviveRewardedAd() => ShowRewardedAdInternal(true, false);

    private bool ShowRewardedAdInternal(bool forRevive, bool forTicket)
    {
        if (isDebugMode)
        {
            StartCoroutine(DelayReward(true, forRevive, forTicket));
            return true;
        }
        rewardedForRevive = forRevive;
        rewardedForTicket = forTicket;
        WebGLAdsShowRewarded();
        return true;
    }
    #endregion

    #region JS → Unity 回调
    private void JS_BeforeAd() => Time.timeScale = 0f;
    private void JS_AfterAd()  => Time.timeScale = 1f;

    private void JS_OpenAdDone(string msg)
    {
        Debug.Log($"[WebGL-Ads] 开屏结果：{msg}");
        // 开屏结束再加载横幅
        ShowBanner();
    }

    private void JS_BannerDone(string msg) =>
        Debug.Log($"[WebGL-Ads] 横幅结果：{msg}");

    private void JS_RewardedDone(string msg)
    {
        var arr = msg.Split('|');
        rewardedSuccess = arr[0] == "ok";
        StartCoroutine(DelayReward(rewardedSuccess, rewardedForRevive, rewardedForTicket));
    }

    private IEnumerator DelayReward(bool success, bool forRevive, bool forTicket)
    {
        yield return null; // 等下一帧再发事件，避免广告层遮挡 UI
        if (forRevive)
            OnReviveRewardedAdCompleted?.Invoke(success);
        else if (forTicket)
            OnTicketRewardedAdCompleted?.Invoke(success);
        else
            OnRewardedAdCompleted?.Invoke(success);
    }
    #endregion
}
#endif