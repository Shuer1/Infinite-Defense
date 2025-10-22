using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 初始化远程资源下载的组件
/// 在游戏启动时下载和缓存必要的远程资源文件
/// </summary>
public class InitialRemoteFetch : MonoBehaviour
{
    // 想一次性下载完的标签，可空
    public string[] initLabels = { "char", "scene" };
    
    /// <summary>
    /// 进度更新事件
    /// 参数1: 进度值 (0.0 - 1.0)，-1表示错误
    /// 参数2: 状态消息
    /// </summary>
    public delegate void ProgressUpdate(float progress, string message);  //委托方法
    public static event ProgressUpdate OnProgressUpdate;

    async void Start()
    {
        try 
        {
            // 1. 强制更新远程目录（必须）
            OnProgressUpdate?.Invoke(0.1f, "正在初始化资源系统...");
            AsyncOperationHandle<IResourceLocator> init = Addressables.InitializeAsync();
            await init.Task;

            // 2. 下载并缓存所有指定标签的 Bundle（无本地缓存或版本变化时会自动拉新）
            float progressPerLabel = 0.8f / initLabels.Length;
            float currentProgress = 0.1f;
            
            List<Task> downloadTasks = new List<Task>();
            
            foreach (string lbl in initLabels)
            {
                OnProgressUpdate?.Invoke(currentProgress, $"正在检查资源: {lbl}");
                
                AsyncOperationHandle<long> getSize = Addressables.GetDownloadSizeAsync(lbl);
                await getSize.Task;
                
                if (getSize.Result > 0)
                {
                    // 并行下载所有标签资源
                    var downloadTask = DownloadLabelAsync(lbl, currentProgress, progressPerLabel);
                    downloadTasks.Add(downloadTask);
                }
                
                currentProgress += progressPerLabel;
            }
            
            // 等待所有下载任务完成
            await Task.WhenAll(downloadTasks);
            
            // 3. 到这里资源已全部进入本地缓存，可正常加载
            OnProgressUpdate?.Invoke(0.95f, "资源准备完成，正在加载场景...");
            Debug.Log("远程美术资源就绪，进入首场景！");
            Addressables.LoadSceneAsync("Assets/Scenes/Main.unity");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"资源初始化过程中出现异常: {ex.Message}");
            OnProgressUpdate?.Invoke(-1, $"资源初始化失败: {ex.Message}");
        }
    }
    
    private async Task DownloadLabelAsync(string label, float baseProgress, float progressRange)
    {
        try
        {
            OnProgressUpdate?.Invoke(baseProgress, $"正在下载资源: {label}");
            
            AsyncOperationHandle download = Addressables.DownloadDependenciesAsync(label, false);
            
            // 监控下载进度
            while (!download.IsDone)
            {
                if (download.IsValid())
                {
                    OnProgressUpdate?.Invoke(
                        baseProgress + download.PercentComplete * progressRange, 
                        $"正在下载 {label}: {Mathf.RoundToInt(download.PercentComplete * 100)}%"
                    );
                }
                await Task.Yield();
            }
            
            await download.Task;
            
            if (download.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"下载 {label} 失败: {download.OperationException}");
                OnProgressUpdate?.Invoke(-1, $"下载 {label} 失败");
            }
            
            // 释放句柄
            Addressables.Release(download);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"下载 {label} 时发生异常: {ex.Message}");
            OnProgressUpdate?.Invoke(-1, $"下载 {label} 异常");
        }
    }
}