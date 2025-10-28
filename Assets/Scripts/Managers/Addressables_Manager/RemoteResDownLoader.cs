using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// 0-GC 并行下载远程 Addressable 资源并平滑汇报进度
/// </summary>
public class InitialRemoteFetch : MonoBehaviour
{
    public static InitialRemoteFetch Instance { get; private set; }

    [Header("首包必须下载的标签")]
    public string[] initLabels = { "char", "scene" , "img", "icon"};

    [Header("首场景（Addressable 地址）")]
    public AssetReference mainScene;   // 拖拽赋值，避免硬编码字符串

    // 对外只暴露一个事件：0~1 进度 + 状态
    public static event Action<float, string> OnProgress;

    private CancellationTokenSource _cts;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        _cts = new CancellationTokenSource();
        try
        {
            await FetchAllAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[InitialRemoteFetch] User cancelled.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            OnProgress?.Invoke(-1f, ex.Message);
        }
    }

    // 供 UI 重试
    public async UniTaskVoid RestartAsync(CancellationToken token)
    {
        await FetchAllAsync(token);
    }

    // 核心逻辑：初始化 → 并行下载 → 加载场景
    private async UniTask FetchAllAsync(CancellationToken token)
    {
        OnProgress?.Invoke(0.05f, "Initializing…");
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: token);

        long totalBytes = 0;
        foreach (var lbl in initLabels)
        {
            long size = await Addressables.GetDownloadSizeAsync(lbl)
                                          .ToUniTask(cancellationToken: token);
            totalBytes += size;
        }

        if (totalBytes == 0)
        {
            OnProgress?.Invoke(1f, "Nothing to download.");
        }
        else
        {
            var progress = new Progress<float>(p => OnProgress?.Invoke(
                                                 0.05f + p * 0.90f, "downloading…"));
            await DownloadDependenciesParallelAsync(initLabels, totalBytes,
                                                   progress, token);
        }

        OnProgress?.Invoke(0.95f, "Loading scene…");
        await Addressables.LoadSceneAsync(mainScene.RuntimeKey.ToString(),
                                          LoadSceneMode.Single)
                          .ToUniTask(cancellationToken: token);
    }

    // 并行下载多个标签，整体汇报一个 0~1 进度
    private async UniTask DownloadDependenciesParallelAsync(
        IReadOnlyList<string> labels,
        long totalBytes,
        IProgress<float> progress,
        CancellationToken token)
    {
        var tasks = new UniTask[labels.Count];
        long downloaded = 0;

        for (int i = 0; i < labels.Count; ++i)
        {
            string lbl = labels[i];
            long labelSize = GetLabelSize(lbl);          // 缓存大小
            tasks[i] = DownloadLabelAsync(lbl,
                new Progress<float>(p =>
                {
                    long prev = Interlocked.Read(ref downloaded);
                    long add = (long)(p * labelSize);
                    Interlocked.Add(ref downloaded, add - prev);
                    progress.Report((float)downloaded / totalBytes);
                }), token);
        }

        await UniTask.WhenAll(tasks);
    }

    // 单个标签下载
    private async UniTask DownloadLabelAsync(string label,
                                           IProgress<float> progress,
                                           CancellationToken token)
    {
        var handle = Addressables.DownloadDependenciesAsync(label, false);
        await handle.ToUniTask(progress, cancellationToken: token);
        Addressables.Release(handle);
    }

    // 缓存标签大小
    private readonly Dictionary<string, long> _sizeCache = new();
    private long GetLabelSize(string label)
    {
        if (!_sizeCache.TryGetValue(label, out var size))
        {
            size = Addressables.GetDownloadSizeAsync(label).WaitForCompletion();
            _sizeCache[label] = size;
        }
        return size;
    }

    private void OnDestroy() => _cts?.Cancel();
}