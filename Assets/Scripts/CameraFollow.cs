using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 玩家目标
    public Vector3 offset; // 跟随偏移（相对玩家的位置）
    public float smoothSpeed = 0.125f; // 平滑跟随速度

    // 新增：接收抖动偏移量（由CameraShakeController设置）
    [HideInInspector] public Vector3 shakeOffset;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 计算跟随目标位置（玩家位置 + 基础偏移）
        Vector3 desiredPosition = target.position + offset;
        // 2. 叠加抖动偏移量（这是关键！）
        desiredPosition += shakeOffset;
        // 3. 平滑过渡到目标位置
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 保持固定视角（如果需要）
        transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }
}