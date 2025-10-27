using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdapter : MonoBehaviour
{
    [Tooltip("是否强制适配安全区域（勾选则生效，否则忽略）")]
    public bool enableAdaptation = true; // 用于控制是否启用适配（方便单个Canvas跳过适配）

    private RectTransform _uiRoot;
    private Rect _lastSafeArea;
    private Canvas _parentCanvas;

    private void Awake()
    {
        _uiRoot = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();

        if (_parentCanvas == null)
        {
            Debug.LogError($"{gameObject.name} 未挂载在任何Canvas下，适配失效！");
            enabled = false; // 禁用脚本，避免报错
        }
    }

    private void Start()
    {
        if (enableAdaptation)
            AdaptToSafeArea();
    }

    private void Update()
    {
        if (enableAdaptation && Screen.safeArea != _lastSafeArea)
        {
            AdaptToSafeArea();
        }
    }

    private void AdaptToSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;

        // 根据Canvas渲染模式计算安全区域的UI坐标
        Vector2 anchorMin, anchorMax;

        switch (_parentCanvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                // 屏幕空间覆盖模式：直接用屏幕像素比例计算锚点
                anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
                anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
                break;

            case RenderMode.ScreenSpaceCamera:
                // 屏幕空间相机模式：需转换为相机视口坐标
                Camera canvasCam = _parentCanvas.worldCamera ?? Camera.main;
                if (canvasCam == null)
                {
                    Debug.LogError($"{_parentCanvas.name} 未指定相机，适配失效！");
                    return;
                }
                // 将安全区域像素坐标转换为视口坐标（0-1范围）
                anchorMin = canvasCam.ScreenToViewportPoint(safeArea.position);
                anchorMax = canvasCam.ScreenToViewportPoint(safeArea.position + safeArea.size);
                break;

            default:
                // 世界空间模式（World Space）：通常无需适配安全区域，直接跳过
                Debug.LogWarning($"{_parentCanvas.name} 是世界空间Canvas，不支持安全区域适配");
                return;
        }

        // 应用适配到当前UI根容器
        _uiRoot.anchorMin = anchorMin;
        _uiRoot.anchorMax = anchorMax;
        _uiRoot.offsetMin = Vector2.zero;
        _uiRoot.offsetMax = Vector2.zero;

        Debug.Log($"{gameObject.name} 适配安全区域完成：{safeArea}");
    }
}