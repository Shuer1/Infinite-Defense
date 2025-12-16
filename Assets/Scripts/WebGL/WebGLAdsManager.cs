using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLAdsManager : MonoBehaviour
{
    // 广告状态锁（防止并发展示）
    private bool openShowing = false;
    private bool rewardedShowing = false;
    private string currentRewardedType = "normal"; // 激励广告类型：normal/revive/ticket

    // JS桥接方法声明
    [DllImport("__Internal")]
    private static extern void WebGLAdsInit();
    [DllImport("__Internal")]
    private static extern void WebGLAdsShowOpen();
    [DllImport("__Internal")]
    private static extern void WebGLAdsShowBanner();
    [DllImport("__Internal")]
    private static extern void WebGLAdsShowRewarded();
    [DllImport("__Internal")]
    private static extern void SetUserAdId(string udid);
    [DllImport("__Internal")]
    private static extern void SetAdSlot(string adType, string slotId);
    [DllImport("__Internal")]
    private static extern string GetAdSlot(string adType);

    void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            WebGLAdsInit();
        }
    }

    // 开屏广告
    public void ShowAppOpenAd()
    {
        if (openShowing)
        {
            Debug.LogWarning("开屏广告正在展示中，跳过请求");
            return;
        }
        openShowing = true;
        WebGLAdsShowOpen();
    }

    // 通用激励广告
    public void ShowRewardedAd(string adType = "normal")
    {
        if (rewardedShowing)
        {
            Debug.LogWarning("激励广告正在展示中，跳过请求");
            ShowAdFailMessage("广告正在加载中，请稍后再试");
            return;
        }
        currentRewardedType = adType;
        rewardedShowing = true;
        WebGLAdsShowRewarded();
    }

    // 复活专用激励广告
    public void ShowReviveRewardedAd() => ShowRewardedAd("revive");
    // 升级券专用激励广告
    public void ShowTicketRewardedAd() => ShowRewardedAd("ticket");

    // 横幅广告
    public void ShowBanner() => WebGLAdsShowBanner();

    // 设置用户广告ID
    public void SetAdId(string udid)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            SetUserAdId(udid);
        }
    }

    // 动态设置广告位ID（运营侧调用）
    public void UpdateAdSlot(string adType, string slotId)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;
        SetAdSlot(adType, slotId);
        Debug.Log($"已设置{adType}广告位ID：{slotId}");
    }

    // 获取当前广告位配置（调试/核对）
    public string GetCurrentAdSlot(string adType)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return "";
        return GetAdSlot(adType);
    }

    // 广告展示前回调（暂停游戏）
    public void JS_BeforeAd()
    {
        Time.timeScale = 0;
    }

    // 广告展示后回调（恢复游戏）
    public void JS_AfterAd()
    {
        Time.timeScale = 1;
    }

    // 开屏广告回调
    public void JS_OpenAdDone(string result)
    {
        openShowing = false;
        // 区分错误类型提示
        string failMsg = result switch
        {
            "config_error" => "开屏广告配置异常（运营侧排查）",
            "timeout" => "开屏广告加载超时，请检查网络",
            "ok" => "", // 成功无提示
            _ => $"开屏广告失败：{result}"
        };
        if (!string.IsNullOrEmpty(failMsg))
        {
            Debug.LogWarning(failMsg);
            // 仅非用户主动取消的错误提示给用户
            if (result != "user_abort") ShowAdFailMessage(failMsg);
        }
    }

    // 激励广告回调
    public void JS_RewardedDone(string result)
    {
        rewardedShowing = false;
        Time.timeScale = 1; // 兜底恢复时间缩放

        string[] resArray = result.Split('|');
        if (resArray.Length < 2)
        {
            Debug.LogError("激励广告回调格式错误：" + result);
            ShowAdFailMessage("广告回调异常，请重试");
            return;
        }

        string status = resArray[0];
        string reason = resArray[1];
        // 区分错误类型生成提示语
        string failMsg = reason switch
        {
            "config_error" => "广告配置异常（运营侧排查）",
            "timeout" => "广告加载超时，请检查网络",
            "user_abort" => "你已取消广告观看，无法获得奖励",
            "unknown" => "广告展示失败，请稍后再试",
            _ => $"广告失败：{reason}"
        };

        if (status == "ok")
        {
            // 根据广告类型发放对应奖励
            switch (currentRewardedType)
            {
                case "revive":
                    Debug.Log("发放复活奖励");
                    // 复活奖励逻辑...
                    break;
                case "ticket":
                    Debug.Log("发放升级券奖励");
                    // 升级券奖励逻辑...
                    break;
                default:
                    Debug.Log("发放通用奖励");
                    // 通用奖励逻辑...
                    break;
            }
        }
        else
        {
            Debug.LogError($"激励广告({currentRewardedType})失败：{reason}");
            ShowAdFailMessage(failMsg);
        }
    }

    // 横幅广告回调
    public void JS_BannerDone(string result)
    {
        string failMsg = result switch
        {
            "config_error" => "横幅广告配置异常（运营侧排查）",
            "ok" => "",
            _ => $"横幅广告失败：{result}"
        };
        if (!string.IsNullOrEmpty(failMsg))
        {
            Debug.LogWarning(failMsg);
        }
    }

    // 广告失败提示（可替换为UI弹窗）
    private void ShowAdFailMessage(string msg)
    {
        Debug.Log($"[用户提示] {msg}");
        // 此处对接游戏UI弹窗逻辑，示例：
        // UIManager.Instance.ShowTip(msg);
    }
}