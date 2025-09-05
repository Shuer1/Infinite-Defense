using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // 玩家
    public Vector3 offset = new Vector3(0, 15, -10); // 相机相对位置
    public float smoothSpeed = 5f; // 平滑跟随速度

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = Quaternion.Euler(45f, 0f, 0f); // 固定俯视角
    }
}
