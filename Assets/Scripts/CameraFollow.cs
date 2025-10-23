using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 玩家目标
    public Vector3 offset; // 跟随偏移（相对玩家的位置）
    public float smoothSpeed = 0.125f; // 平滑跟随速度

    // 边界限制参数
    [Header("边界限制")]
    public bool enableBoundaryLimit = true;
    public Vector2 boundaryMargin = new Vector2(5f, 5f); // 边距大小
    public Vector2 boundaryCenter = Vector2.zero; // 边界中心点
    public Vector2 boundarySize = new Vector2(20f, 20f); // 边界尺寸

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
        
        // 应用边界限制
        if (enableBoundaryLimit)
        {
            smoothedPosition = LimitCameraPosition(smoothedPosition);
        }
        
        transform.position = smoothedPosition;

        // 保持固定视角（如果需要）
        transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }
    
    /// <summary>
    /// 限制摄像机位置在边界范围内
    /// </summary>
    /// <param name="position">目标位置</param>
    /// <returns>限制后的位置</returns>
    private Vector3 LimitCameraPosition(Vector3 position)
    {
        // 计算边界范围
        float minX = boundaryCenter.x - boundarySize.x / 2 + boundaryMargin.x;
        float maxX = boundaryCenter.x + boundarySize.x / 2 - boundaryMargin.x;
        float minZ = boundaryCenter.y - boundarySize.y / 2 + boundaryMargin.y;
        float maxZ = boundaryCenter.y + boundarySize.y / 2 - boundaryMargin.y;
        
        // 限制X和Z轴位置（Y轴保持不变）
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        
        return position;
    }
    
    /// <summary>
    /// 在Scene视图中可视化边界
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableBoundaryLimit) return;
        
        // 绘制边界框
        Gizmos.color = Color.yellow;
        Vector3 boundaryCenter3D = new Vector3(boundaryCenter.x, transform.position.y, boundaryCenter.y);
        Gizmos.DrawWireCube(boundaryCenter3D, new Vector3(boundarySize.x, 0, boundarySize.y));
        
        // 绘制内边界（考虑边距）
        Gizmos.color = Color.red;
        Vector3 marginCenter3D = new Vector3(boundaryCenter.x, transform.position.y, boundaryCenter.y);
        Vector3 marginSize = new Vector3(
            boundarySize.x - boundaryMargin.x * 2,
            0,
            boundarySize.y - boundaryMargin.y * 2
        );
        Gizmos.DrawWireCube(marginCenter3D, marginSize);
    }
}