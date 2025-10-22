using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;

public class InitialRemoteFetch : MonoBehaviour
{
    // 想一次性下载完的标签，可空
    public string[] initLabels = { "char", "scene" };

    async void Start()
    {
        // 1. 强制更新远程目录（必须）
        AsyncOperationHandle<IResourceLocator> init = Addressables.InitializeAsync();
        await init.Task;

        // 2. 下载并缓存所有指定标签的 Bundle（无本地缓存或版本变化时会自动拉新）
        foreach (string lbl in initLabels)
        {
            AsyncOperationHandle<long> getSize = Addressables.GetDownloadSizeAsync(lbl);
            await getSize.Task;
            if (getSize.Result > 0)
            {
                AsyncOperationHandle download =
                    Addressables.DownloadDependenciesAsync(lbl, true);   // autoReleaseHandle=true
                await download.Task;
                if (download.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"下载 {lbl} 失败: {download.OperationException}");
            }
        }

        // 3. 到这里资源已全部进入本地缓存，可正常加载
        Debug.Log("远程美术资源就绪，进入首场景！");
        Addressables.LoadSceneAsync("Assets/Scenes/Main.unity");
    }
}