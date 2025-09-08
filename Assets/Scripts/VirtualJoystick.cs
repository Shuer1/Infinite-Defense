using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    public RectTransform background;   // 摇杆底盘
    public RectTransform handle;       // 摇杆手柄
    [Range(50f, 150f)]
    public float handleRange = 100f;   // 手柄最大偏移范围（像素）

    private Vector2 inputVector;       // 摇杆输入值
    private Vector2 joystickCenter;    // 摇杆中心点

    void Start()
    {
        joystickCenter = background.position;
        handle.anchoredPosition = Vector2.zero; // 初始归位
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // 按下时立即更新一次
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 计算手指与摇杆中心的向量
        Vector2 direction = eventData.position - (Vector2)joystickCenter;

        // 限制最大半径
        direction = Vector2.ClampMagnitude(direction, handleRange);

        // 设置手柄位置
        handle.anchoredPosition = direction;

        // 转换为 -1 ~ 1 的输入值
        inputVector = direction / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 手指松开时归位
        handle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }

    // 提供给外部的输入值
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
}
