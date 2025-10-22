using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 启动管理器，用于初始化游戏和远程资源下载
/// </summary>
public class StartupManager : MonoBehaviour
{
    [Header("资源配置")]
    public string[] resourceLabels = { "char", "scene" };
    
    [Header("场景配置")]
    public string mainSceneAddress = "Assets/Scenes/Main.unity";
    
    [Header("UI引用")]
    public GameObject loadingUIPanel;
    
    private void Start()
    {
        // 确保Loading UI初始状态为隐藏
        if (loadingUIPanel != null)
            loadingUIPanel.SetActive(false);
        
        // 初始化远程资源获取
        InitializeRemoteResources();
    }
    
    /// <summary>
    /// 初始化远程资源获取
    /// </summary>
    private void InitializeRemoteResources()
    {
        // 创建InitialRemoteFetch组件实例
        GameObject remoteFetchObject = new GameObject("RemoteResourceFetcher");
        InitialRemoteFetch remoteFetch = remoteFetchObject.AddComponent<InitialRemoteFetch>();
        remoteFetch.initLabels = resourceLabels;
        
        // 如果有Loading UI面板，显示它并关联到ResourceDownloadUI
        if (loadingUIPanel != null)
        {
            loadingUIPanel.SetActive(true);
            
            ResourceDownloadUI downloadUI = loadingUIPanel.GetComponent<ResourceDownloadUI>();
            if (downloadUI == null)
            {
                downloadUI = loadingUIPanel.AddComponent<ResourceDownloadUI>();
            }
            
            // 关联UI元素
            if (downloadUI.downloadPanel == null)
                downloadUI.downloadPanel = loadingUIPanel;
        }
    }
}