using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightningEffect : MonoBehaviour
{
    private LineRenderer lr;
    private List<Transform> targetChain = new List<Transform>(); // 目标链
    private float duration = 0.3f;
    private GameObject prefabKey;
    private float trailFadeSpeed = 2f; // 拖尾消失速度
    private int segments = 8; // 线段分段数，越多拖尾越平滑
    private List<Vector3> segmentOffsets = new List<Vector3>(); // 每段的随机偏移

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            // 初始化线段渲染器属性
            lr.positionCount = segments;
            lr.widthCurve = new AnimationCurve(
                new Keyframe(0, 0.1f),
                new Keyframe(0.5f, 0.3f),
                new Keyframe(1, 0.1f)
            ); // 中间粗两端细
            lr.numCapVertices = 5;
            lr.numCornerVertices = 5;
        }
    }

    // 初始化方法，支持传入目标链
    public void Init(List<Transform> targets, GameObject prefab, float duration = 0.3f)
    {
        this.prefabKey = prefab;
        this.targetChain = new List<Transform>(targets);
        this.duration = duration;

        // 初始化随机偏移量，让闪电更自然
        segmentOffsets.Clear();
        for (int i = 0; i < segments; i++)
        {
            segmentOffsets.Add(Random.insideUnitSphere * 0.3f);
        }

        UpdateLightningPositions();
        StopAllCoroutines();
        StartCoroutine(AnimateLightning());
    }

    // 单个目标连接的重载（兼容旧用法）
    public void Init(Transform start, Transform end, GameObject prefab, float duration = 0.3f)
    {
        Init(new List<Transform> { start, end }, prefab, duration);
    }

    private IEnumerator AnimateLightning()
    {
        float lifeTime = 0;
        while (lifeTime < duration)
        {
            lifeTime += Time.deltaTime;
            UpdateLightningPositions();
            
            // 随时间增加随机偏移，模拟电流流动
            for (int i = 0; i < segmentOffsets.Count; i++)
            {
                segmentOffsets[i] = Vector3.Lerp(
                    segmentOffsets[i], 
                    Random.insideUnitSphere * 0.4f, 
                    Time.deltaTime * 15f
                );
            }

            // 逐渐降低透明度
            Color startColor = lr.startColor;
            Color endColor = lr.endColor;
            startColor.a = Mathf.Lerp(1f, 0f, lifeTime / duration);
            endColor.a = Mathf.Lerp(0.8f, 0f, lifeTime / duration);
            lr.startColor = startColor;
            lr.endColor = endColor;

            yield return null;
        }

        gameObject.SetActive(false);
        ParticleEffectPool.Instance.RecycleEffect(prefabKey, this.gameObject);
    }

    private void UpdateLightningPositions()
    {
        if (lr == null || targetChain.Count < 2) return;

        // 计算所有目标点之间的总距离
        float totalDistance = 0;
        for (int i = 0; i < targetChain.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(
                targetChain[i].position, 
                targetChain[i + 1].position
            );
        }

        // 生成闪电路径点
        float currentDistance = 0;
        int currentTargetIndex = 0;
        Vector3 currentTargetPos = targetChain[0].position;
        Vector3 nextTargetPos = targetChain[1].position;
        float segmentDistance = totalDistance / (segments - 1);

        for (int i = 0; i < segments; i++)
        {
            // 计算当前段应该在哪个目标区间
            while (currentDistance + Vector3.Distance(currentTargetPos, nextTargetPos) < segmentDistance * i)
            {
                currentDistance += Vector3.Distance(currentTargetPos, nextTargetPos);
                currentTargetIndex++;
                if (currentTargetIndex >= targetChain.Count - 1) break;
                currentTargetPos = targetChain[currentTargetIndex].position;
                nextTargetPos = targetChain[currentTargetIndex + 1].position;
            }

            // 计算当前段在目标区间内的比例
            float remaining = (segmentDistance * i) - currentDistance;
            float ratio = remaining / Vector3.Distance(currentTargetPos, nextTargetPos);
            ratio = Mathf.Clamp01(ratio);

            // 计算带随机偏移的位置
            Vector3 position = Vector3.Lerp(currentTargetPos, nextTargetPos, ratio);
            position += segmentOffsets[i] * (1 - i / (float)segments); // 拖尾逐渐变弱

            lr.SetPosition(i, position);
        }
    }
}