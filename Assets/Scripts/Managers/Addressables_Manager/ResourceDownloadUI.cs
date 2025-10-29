using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 【资源下载 UI（增强版）】
/// 增加了：网络状态提示、自动重试显示、缓存清理按钮。
/// </summary>
public class ResourceDownloadUI_Enhanced : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private GameObject downloadPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button clearCacheButton;

    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        downloadPanel?.SetActive(true);
        retryButton?.onClick.AddListener(OnRetry);
        cancelButton?.onClick.AddListener(OnCancel);
        clearCacheButton?.onClick.AddListener(OnClearCache);
        InitialRemoteFetch.OnProgress += OnProgressChanged;
    }

    private void OnDisable()
    {
        InitialRemoteFetch.OnProgress -= OnProgressChanged;
        retryButton?.onClick.RemoveListener(OnRetry);
        cancelButton?.onClick.RemoveListener(OnCancel);
        clearCacheButton?.onClick.RemoveListener(OnClearCache);
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void OnProgressChanged(float progress01, string message)
    {
        if (progress01 < 0)
        {
            HandleError(message);
            return;
        }

        SetProgress(progress01, message);

        if (progress01 >= 1f)
        {
            HidePanel();
        }
    }

    private void SetProgress(float p01, string msg)
    {
        if (progressBar)  progressBar.value = p01;
        if (progressText) progressText.text = $"{Mathf.RoundToInt(p01 * 100)}%";
        if (statusText)   statusText.text = msg;
    }

    private void HandleError(string err)
    {
        SetProgress(0f, $"下载失败：{err}");
        retryButton?.gameObject.SetActive(true);
        cancelButton?.gameObject.SetActive(true);
        Debug.LogError($"[ResourceDownloadUI] {err}");
    }

    private void HidePanel()
    {
        if (downloadPanel) downloadPanel.SetActive(false);
    }

    private void OnRetry()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        retryButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(true);

        SetProgress(0f, "重新尝试下载中…");
        InitialRemoteFetch.Instance.RestartAsync(_cts.Token).Forget();
    }

    private void OnCancel()
    {
        _cts?.Cancel();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private async void OnClearCache()
    {
        clearCacheButton.interactable = false;
        await InitialRemoteFetch.Instance.ClearCacheAsync();
        clearCacheButton.interactable = true;
    }
}
