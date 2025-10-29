using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// 【Addressables 远程资源增强版下载管理器】
/// 支持网络检测、断点续传、自动重试、清除缓存。
/// </summary>
public class InitialRemoteFetch : MonoBehaviour
{
    public static InitialRemoteFetch Instance { get; private set; }

    [Header("首包必须下载的标签")]
    public string[] initLabels = { "char", "scene", "img", "icon" };

    [Header("游戏主场景（Addressable 地址）")]
    public AssetReference mainScene;

    [Header("下载设置")]
    [Tooltip("网络断开后重试间隔（秒）")]
    public float retryInterval = 3f;
    [Tooltip("最大自动重试次数")]
    public int maxRetryCount = 3;

    public static event Action<float, string> OnProgress;

    private CancellationTokenSource _cts;
    private readonly Dictionary<string, long> _sizeCache = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await BeginDownloadSequence(_cts.Token);
    }

    public async UniTaskVoid RestartAsync(CancellationToken token)
    {
        await BeginDownloadSequence(token);
    }

    /// <summary>
    /// 总流程：初始化 → 检查网络 → 下载 → 加载主场景
    /// </summary>
    private async UniTask BeginDownloadSequence(CancellationToken token)
    {
        try
        {
            OnProgress?.Invoke(0.05f, "正在初始化资源系统…");
            await Addressables.InitializeAsync().ToUniTask(cancellationToken: token);

            // 检查网络
            if (!CheckNetwork())
            {
                OnProgress?.Invoke(0f, "网络不可用，请检查连接…");
                await WaitForNetworkAvailable(token);
            }

            var labelsToDownload = new List<string>();
            long totalBytes = 0;

            // 检查哪些资源未下载
            foreach (var lbl in initLabels)
            {
                if (PlayerPrefs.GetInt($"Label_{lbl}_Downloaded", 0) == 1)
                    continue;

                long size = await Addressables.GetDownloadSizeAsync(lbl)
                                              .ToUniTask(cancellationToken: token);
                if (size > 0)
                {
                    labelsToDownload.Add(lbl);
                    totalBytes += size;
                }
            }

            if (labelsToDownload.Count == 0)
            {
                OnProgress?.Invoke(1f, "资源已全部存在");
            }
            else
            {
                OnProgress?.Invoke(0.1f, $"发现 {labelsToDownload.Count} 个资源包待下载…");
                var progress = new Progress<float>(p =>
                    OnProgress?.Invoke(0.1f + p * 0.8f, $"下载中… {Mathf.RoundToInt(p * 100)}%"));

                await DownloadWithRetryAsync(labelsToDownload, totalBytes, progress, token);
            }

            OnProgress?.Invoke(0.95f, "正在加载主场景…");
            await Addressables.LoadSceneAsync(mainScene, LoadSceneMode.Single)
                              .ToUniTask(cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[InitialRemoteFetch] 下载取消。");
        }
        catch (Exception ex)
        {
            OnProgress?.Invoke(-1f, $"异常：{ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// 自动重试逻辑封装
    /// </summary>
    private async UniTask DownloadWithRetryAsync(
        IReadOnlyList<string> labels,
        long totalBytes,
        IProgress<float> progress,
        CancellationToken token)
    {
        int retryCount = 0;

        while (retryCount <= maxRetryCount)
        {
            try
            {
                await DownloadDependenciesSequentiallyAsync(labels, totalBytes, progress, token);
                return; // 成功则直接返回
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount > maxRetryCount)
                {
                    OnProgress?.Invoke(-1f, $"下载失败：{ex.Message}");
                    return;
                }

                Debug.LogWarning($"[InitialRemoteFetch] 下载失败，{retryInterval}s 后第 {retryCount}/{maxRetryCount} 次重试…");
                OnProgress?.Invoke(0f, $"下载失败，{retryInterval}s 后重试（{retryCount}/{maxRetryCount}）…");
                await UniTask.Delay(TimeSpan.FromSeconds(retryInterval), cancellationToken: token);
            }
        }
    }

    /// <summary>
    /// 串行下载标签，逐个持久化完成标记。
    /// </summary>
    private async UniTask DownloadDependenciesSequentiallyAsync(
        IReadOnlyList<string> labels,
        long totalBytes,
        IProgress<float> progress,
        CancellationToken token)
    {
        long downloaded = 0;

        foreach (var lbl in labels)
        {
            long size = await Addressables.GetDownloadSizeAsync(lbl).ToUniTask(cancellationToken: token);
            if (size <= 0)
            {
                PlayerPrefs.SetInt($"Label_{lbl}_Downloaded", 1);
                continue;
            }

            // 若网络断开则等待恢复
            while (!CheckNetwork())
            {
                OnProgress?.Invoke(0f, "网络断开，等待重新连接…");
                await WaitForNetworkAvailable(token);
            }

            OnProgress?.Invoke(0f, $"正在下载：{lbl} …");

            var handle = Addressables.DownloadDependenciesAsync(lbl, false);
            await handle.ToUniTask(progress, cancellationToken: token);
            Addressables.Release(handle);

            downloaded += size;
            progress.Report((float)downloaded / totalBytes);

            PlayerPrefs.SetInt($"Label_{lbl}_Downloaded", 1);
            PlayerPrefs.Save();

            Debug.Log($"[InitialRemoteFetch] 标签 {lbl} 下载完成。");
        }
    }

    /// <summary>
    /// 网络检测（移动端安全）
    /// </summary>
    private bool CheckNetwork()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    /// <summary>
    /// 等待直到网络恢复
    /// </summary>
    private async UniTask WaitForNetworkAvailable(CancellationToken token)
    {
        while (!CheckNetwork())
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);
        }
        OnProgress?.Invoke(0f, "网络已恢复，继续下载…");
    }

    /// <summary>
    /// 提供外部调用的缓存清除接口
    /// </summary>
    public async UniTask ClearCacheAsync()
    {
        OnProgress?.Invoke(0f, "清除缓存中…");
        foreach (var lbl in initLabels)
        {
            await Addressables.ClearDependencyCacheAsync(lbl, false).ToUniTask();
            PlayerPrefs.DeleteKey($"Label_{lbl}_Downloaded");
        }
        PlayerPrefs.Save();
        OnProgress?.Invoke(1f, "缓存已清空");
        Debug.Log("[InitialRemoteFetch] 缓存清除完成。");
    }

    private void OnDestroy() => _cts?.Cancel();
}
