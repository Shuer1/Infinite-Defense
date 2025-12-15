using System;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLAdsManager : MonoBehaviour
{
    public static WebGLAdsManager Instance;

    [Header("调试模式")]
    public bool isDebugMode = false;

    // 与 Android 端同名事件
    public event Action<bool> OnRewardedAdCompleted;
    public event Action<bool> OnTicketRewardedAdCompleted;
    public event Action<bool> OnReviveRewardedAdCompleted;

    [DllImport("__Internal")] private static extern void WebGLAdsInit();
    [DllImport("__Internal")] private static extern void WebGLAdsShowOpen();
    [DllImport("__Internal")] private static extern void WebGLAdsShowBanner();
    [DllImport("__Internal")] private static extern void WebGLAdsShowRewarded();
    [DllImport("__Internal")] private static extern void SetUserAdId(string udid); // ← 新增

    private bool rewardedSuccess;
    private bool rewardedForRevive;
    private bool rewardedForTicket;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
        WebGLAdsInit();
        SendUserIdToJS();
        StartCoroutine(DelayOpenAd(0.5f));
    }

    private void SendUserIdToJS()
    {
        string udid = PlayerPrefs.GetString("udid", "");
        if (string.IsNullOrEmpty(udid))
        {
            udid = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("udid", udid);
        }
#if !UNITY_EDITOR && UNITY_WEBGL
        SetUserAdId(udid);
#endif
    }

    private IEnumerator DelayOpenAd(float t)
    {
        yield return new WaitForSeconds(t);
        ShowAppOpenAd();
    }

    #region 对外接口（与 RewardManager 联动）
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
        ShowBanner();
    }

    private void JS_BannerDone(string msg) =>
        Debug.Log($"[WebGL-Ads] 横幅结果：{msg}");

    private void JS_RewardedDone(string msg)
    {
        var arr = msg.Split('|');
        rewardedSuccess = arr[0] == "ok";
        AdTaskSystem.Instance?.OnRewardedFinished(rewardedSuccess);
        StartCoroutine(DelayReward(rewardedSuccess, rewardedForRevive, rewardedForTicket));
    }

    private IEnumerator DelayReward(bool success, bool forRevive, bool forTicket)
    {
        yield return null;
        if (forRevive)
            OnReviveRewardedAdCompleted?.Invoke(success);
        else if (forTicket)
            OnTicketRewardedAdCompleted?.Invoke(success);
        else
            OnRewardedAdCompleted?.Invoke(success);
    }
    #endregion
}