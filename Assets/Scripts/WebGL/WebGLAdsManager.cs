#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLAdsManager : MonoBehaviour
{
    public static WebGLAdsManager Instance { get; private set; }

    [Header("调试模式")]
    public bool isDebugMode = false;

    public event Action<bool> OnRewardedAdCompleted;
    public event Action<bool> OnTicketRewardedAdCompleted;
    public event Action<bool> OnReviveRewardedAdCompleted;

    private bool rewardedSuccess, rewardedForRevive, rewardedForTicket;
    private bool openShowing, rewardedShowing;          // 防止并发

    [DllImport("__Internal")] private static extern void WebGLAdsInit();
    [DllImport("__Internal")] private static extern void WebGLAdsShowOpen();
    [DllImport("__Internal")] private static extern void WebGLAdsShowBanner();
    [DllImport("__Internal")] private static extern void WebGLAdsShowRewarded();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // 等待页面配置
        #if !UNITY_EDITOR
        Application.ExternalEval(@"
          if(!window.webglAdConfig){
            window.webglAdConfig={};
            fetch('./adconfig.json')
              .then(r=>r.json())
              .then(j=>window.webglAdConfig=j)
              .catch(()=>console.warn('adconfig.json not found'));
          }
        ");
        #endif
    }

    private void Start()
    {
        WebGLAdsInit();
        StartCoroutine(DelayOpenAd(0.5f));
    }

    private IEnumerator DelayOpenAd(float t)
    {
        yield return new WaitForSeconds(t);
        ShowAppOpenAd();
    }

    #region 对外 API
    public void ShowAppOpenAd()
    {
        if (isDebugMode) return;
        if (openShowing) return;
        openShowing = true;
        WebGLAdsShowOpen();
        StartCoroutine(Timeout("open", 8f, () => openShowing = false));
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
        if (rewardedShowing) return false;
        rewardedShowing = true;
        rewardedForRevive = forRevive;
        rewardedForTicket = forTicket;
        WebGLAdsShowRewarded();
        StartCoroutine(Timeout("rewarded", 15f, () => rewardedShowing = false));
        return true;
    }
    #endregion

    #region JS → Unity 回调
    private void JS_BeforeAd() => Time.timeScale = 0f;
    private void JS_AfterAd()  => Time.timeScale = 1f;

    private void JS_OpenAdDone(string msg)
    {
        openShowing = false;
        Debug.Log($"[WebGL-Ads] 开屏结果：{msg}");
        if (msg == "ok") ShowBanner();
    }

    private void JS_BannerDone(string msg) =>
        Debug.Log($"[WebGL-Ads] 横幅结果：{msg}");

    private void JS_RewardedDone(string msg)
    {
        rewardedShowing = false;
        var arr = msg.Split('|');
        rewardedSuccess = arr[0] == "ok";
        StartCoroutine(DelayReward(rewardedSuccess, rewardedForRevive, rewardedForTicket));
    }

    private IEnumerator DelayReward(bool success, bool forRevive, bool forTicket)
    {
        yield return null;
        if (forRevive) OnReviveRewardedAdCompleted?.Invoke(success);
        else if (forTicket) OnTicketRewardedAdCompleted?.Invoke(success);
        else OnRewardedAdCompleted?.Invoke(success);
    }

    // 超时兜底
    private IEnumerator Timeout(string type, float seconds, Action onTimeout)
    {
        yield return new WaitForSeconds(seconds);
        if (type == "open" && openShowing) { JS_OpenAdDone("timeout"); openShowing = false; }
        if (type == "rewarded" && rewardedShowing) { JS_RewardedDone("fail|timeout"); rewardedShowing = false; }
    }
    #endregion
}
#endif