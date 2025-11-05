using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightningEffect : MonoBehaviour
{
    [Header("粒子特效组件（必须绑定）")]
    public ParticleSystem lightningParticle;  // 你的雷电粒子系统

    private GameObject prefabKey;             // 对象池 Key
    private float duration = 0.3f;            // 闪电持续时间（粒子播放时间）

    private void Awake()
    {
        if (lightningParticle == null)
            lightningParticle = GetComponentInChildren<ParticleSystem>();

        if (lightningParticle == null)
            Debug.LogError("[LightningEffect] 未绑定粒子特效组件！");
    }

    /// <summary>
    /// 传入目标链（可选），但仅用于定位粒子位置
    /// </summary>
    public void Init(List<Transform> targets, GameObject prefab, float duration = 0.3f)
    {
        this.prefabKey = prefab;
        this.duration = duration;

        if (targets != null && targets.Count > 0)
            transform.position = targets[0].position; // 粒子起点放第一目标位置

        PlayEffect();
    }

    /// <summary> 简化重载：起点→终点 </summary>
    public void Init(Transform start, Transform end, GameObject prefab, float duration = 0.3f)
    {
        transform.position = start.position;
        this.prefabKey = prefab;
        this.duration = duration;

        // 如果粒子系统需要朝向目标，可加上朝向逻辑：
        // transform.forward = (end.position - start.position).normalized;

        PlayEffect();
    }

    private void PlayEffect()
    {
        gameObject.SetActive(true);

        if (lightningParticle != null)
            lightningParticle.Play(true);

        StopAllCoroutines();
        StartCoroutine(DelayRecycle());
    }

    // 在 LightningEffect 类中增加：
    public void SetPrefabKey(GameObject prefab)
    {
        this.prefabKey = prefab;
    }


    /// <summary>
    /// 粒子播放完毕后回收或销毁
    /// </summary>
    private IEnumerator DelayRecycle()
    {
        yield return new WaitForSeconds(duration);

        // 如果特效 Prefab 是通过对象池生成的：
        if (prefabKey != null && ParticleEffectPool.Instance != null)
            ParticleEffectPool.Instance.RecycleEffect(prefabKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
