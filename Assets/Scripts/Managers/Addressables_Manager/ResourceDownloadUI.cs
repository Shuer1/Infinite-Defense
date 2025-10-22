using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ResourceDownloadUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private GameObject downloadPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;

    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        downloadPanel.SetActive(true);
        retryButton?.onClick.AddListener(OnRetry);
        cancelButton?.onClick.AddListener(OnCancel);
        InitialRemoteFetch.OnProgress += OnProgress;
    }

    private void OnDisable()
    {
        InitialRemoteFetch.OnProgress -= OnProgress;
        retryButton?.onClick.RemoveListener(OnRetry);
        cancelButton?.onClick.RemoveListener(OnCancel);
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void OnProgress(float progress01, string message)
    {
        if (progress01 < 0) { HandleError(message); return; }

        SetProgress(progress01, message);
        if (progress01 >= 1f) HidePanel();
    }

    private void SetProgress(float p01, string msg)
    {
        if (progressBar)  progressBar.value  = p01;
        if (progressText) progressText.text = $"{Mathf.RoundToInt(p01 * 100)}%";
        if (statusText)   statusText.text   = msg;
    }

    private void HandleError(string err)
    {
        SetProgress(0f, $"失败：{err}");
        retryButton?.gameObject.SetActive(true);
        cancelButton?.gameObject.SetActive(true);
        Debug.LogError($"[ResourceDownloadUI] {err}");
    }

    private void HidePanel() => downloadPanel.SetActive(false);

    private void OnRetry()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        retryButton.gameObject.SetActive(false);
        SetProgress(0f, "重试中…");
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
}