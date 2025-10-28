using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelScaleAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private Vector3 closedScale = Vector3.zero;
    [SerializeField] private Vector3 openedScale = Vector3.one;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool debugMode = true;

    private bool isAnimating = false;

    // 打开面板
    public void OpenPanel()
    {
        if (!EnsurePanelReady("展开")) return;

        Vector3 startScale = closedScale;
        targetPanel.localScale = startScale;
        canvasGroup.alpha = 0f;

        StopAllCoroutines();
        StartCoroutine(ScaleCoroutine(startScale, openedScale, true));
    }

    // 关闭面板
    public void ClosePanel()
    {
        if (!EnsurePanelReady("收起")) return;

        Vector3 startScale = openedScale;
        targetPanel.localScale = startScale;
        canvasGroup.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(ScaleCoroutine(startScale, closedScale, false));
    }

    // 无动画直接初始化关闭状态（供 UIManager 调用）
    public void InitializePanelState()
    {
        EnsureCanvasGroupExists();
        targetPanel.gameObject.SetActive(true);
        targetPanel.localScale = closedScale;
        canvasGroup.alpha = 0f;
        LogDebug("InitializePanelState: 初始化为关闭状态");
    }

    // 无动画立即关闭
    public void ClosePanelImmediate()
    {
        StopAllCoroutines();
        EnsureCanvasGroupExists();
        targetPanel.localScale = closedScale;
        canvasGroup.alpha = 0f;
        targetPanel.gameObject.SetActive(false);
        isAnimating = false;
        LogDebug("ClosePanelImmediate: 已立即隐藏面板");
    }

    private IEnumerator ScaleCoroutine(Vector3 startScale, Vector3 targetScale, bool isOpening)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        float elapsedTime = 0f;
        EnsureCanvasGroupExists();

        float startAlpha = isOpening ? 0f : 1f;
        float targetAlpha = isOpening ? 1f : 0f;
        targetPanel.localScale = startScale;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);
            targetPanel.localScale = Vector3.Lerp(startScale, targetScale, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        targetPanel.localScale = targetScale;
        canvasGroup.alpha = targetAlpha;
        isAnimating = false;
    }

    private bool EnsurePanelReady(string action)
    {
        if (targetPanel == null)
        {
            targetPanel = GetComponent<RectTransform>();
            if (targetPanel == null)
            {
                Debug.LogError($"PanelScaleAnimation: targetPanel 未赋值（尝试{action}）！");
                return false;
            }
        }

        EnsureCanvasGroupExists();

        if (!targetPanel.gameObject.activeSelf)
            targetPanel.gameObject.SetActive(true);

        return true;
    }

    private void EnsureCanvasGroupExists()
    {
        if (canvasGroup == null && targetPanel != null)
        {
            canvasGroup = targetPanel.GetComponent<CanvasGroup>() ?? targetPanel.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        InitializePanelState(); // ✅ 启动时直接初始化关闭状态（防止 inactive 报错）
    }

    private void LogDebug(string message)
    {
        if (debugMode) Debug.Log($"[PanelAnim] {message}");
    }
}
