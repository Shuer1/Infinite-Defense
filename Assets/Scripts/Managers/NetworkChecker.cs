using UnityEngine;
using System.Collections;

public static class NetworkChecker
{
    // 检查是否有网络连接
    public static bool IsNetworkAvailable()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    // 检查是否为WiFi（可选，优化广告加载策略）
    public static bool IsWifi()
    {
        return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
    }
}