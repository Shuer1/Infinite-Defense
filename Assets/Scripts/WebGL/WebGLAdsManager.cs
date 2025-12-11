using UnityEngine;
using System.Runtime.InteropServices;   // 必需

public class WebGLAdsManager : MonoBehaviour
{
    public bool isDebugMode = false;
    [DllImport("__Internal")] private static extern void AdsInit();
    [DllImport("__Internal")] private static extern void AdsShowOpen();
    [DllImport("__Internal")] private static extern void AdsShowBanner();
    [DllImport("__Internal")] private static extern void AdsShowRewarded();

    private void Start()
    {
        // 初始化
        AdsInit();
    }

    public void ShowOpenAd()
    {
        if (isDebugMode) return;
        AdsShowOpen();
    }

    public void ShowBanner()
    {
        if (isDebugMode) return;
        AdsShowBanner();
    }

    private bool ShowRewardedAdInternal(bool forRevive, bool forTicket)
    {
        if (isDebugMode) { /* 直接发奖励 */ return true; }
        AdsShowRewarded();
        return true;
    }
}