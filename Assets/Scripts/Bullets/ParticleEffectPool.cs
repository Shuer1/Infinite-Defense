using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用特效对象池 - 支持管理多种类型的粒子/特效预制体
/// </summary>
public class ParticleEffectPool : MonoBehaviour  //单例
{
    public static ParticleEffectPool Instance { get; private set; }

    // 单个特效池的配置
    [Serializable]
    public class EffectPoolConfig
    {
        public GameObject effectPrefab; // 特效预制体（作为唯一标识）
        public int initialSize = 5;     // 初始实例数量
    }

    [Tooltip("需要被池化的所有特效配置")]
    public List<EffectPoolConfig> effectConfigs;

    // 核心：用字典存储「预制体→对象队列」的映射
    private Dictionary<GameObject, Queue<GameObject>> effectPools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePools();
    }

    /// <summary>初始化所有特效池</summary>
    private void InitializePools()
    {
        foreach (var config in effectConfigs)
        {
            if (config.effectPrefab == null)
            {
                Debug.LogError("特效预制体不能为空！");
                continue;
            }

            // 为每个预制体创建独立的队列
            Queue<GameObject> pool = new Queue<GameObject>();
            effectPools[config.effectPrefab] = pool;

            // 预创建初始实例
            for (int i = 0; i < config.initialSize; i++)
            {
                GameObject effect = Instantiate(config.effectPrefab, transform);
                effect.SetActive(false);
                pool.Enqueue(effect);
            }
        }
    }

    public GameObject GetEffect(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("GetEffect 失败：传入的 prefab 为空！");
            return null;
        }

        // 如果没有为这个 prefab 创建过对象池，自动创建新的队列
        if (!effectPools.ContainsKey(prefab))
        {
            effectPools[prefab] = new Queue<GameObject>();
        }

        GameObject effectObj;

        // ✅ 从池中取已有的对象
        if (effectPools[prefab].Count > 0)
        {
            effectObj = effectPools[prefab].Dequeue();
        }
        else
        {
            // ✅ 如果池中没有，则实例化新的
            effectObj = Instantiate(prefab, transform);
        }

        // ✅ 保证取出的对象关联正确的 prefabKey（用于回收）
        var lightningEffect = effectObj.GetComponent<LightningEffect>();
        if (lightningEffect != null)
        {
            lightningEffect.SetPrefabKey(prefab);
        }

        return effectObj;
    }

    
    /// <summary>
    /// 从池获取特效并播放
    /// </summary>
    /// <param name="prefab">特效预制体（用于匹配对应的池）</param>
    /// <param name="position">播放位置</param>
    /// <param name="rotation">旋转角度</param>
    public void PlayEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("特效预制体不能为空！");
            return;
        }

        // 检查是否有对应池，没有则自动创建（兜底逻辑）
        if (!effectPools.ContainsKey(prefab))
        {
            Debug.LogWarning($"未配置[{prefab.name}]的池，自动创建初始实例");
            effectPools[prefab] = new Queue<GameObject>();
        }

        Queue<GameObject> pool = effectPools[prefab];
        GameObject effect;

        // 池中有可用实例则复用，否则临时创建
        if (pool.Count > 0)
        {
            effect = pool.Dequeue();
        }
        else
        {
            effect = Instantiate(prefab, transform);
        }

        // 重置特效状态并播放
        position.y = 0f;
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        effect.SetActive(true);

        // 处理粒子系统自动回收
        HandleEffectRecycle(effect, prefab);
    }

    /// <summary>处理特效播放后的自动回收</summary>
    private void HandleEffectRecycle(GameObject effect, GameObject prefab)
    {
        ParticleSystem particle = effect.GetComponent<ParticleSystem>();
        if (particle != null)
        {
            // 等待粒子播放完毕（包括所有粒子生命周期结束）
            float totalDuration = particle.main.duration + particle.main.startLifetime.constantMax;
            StartCoroutine(RecycleAfterDelay(effect, prefab, totalDuration));
        }
        else
        {
            // 非粒子特效（如模型动画），默认2秒后回收（可根据需求调整）
            StartCoroutine(RecycleAfterDelay(effect, prefab, 2f));
        }
    }

    /// <summary>延迟回收特效到对应池</summary>
    private IEnumerator RecycleAfterDelay(GameObject effect, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        effect.SetActive(false);
        effectPools[prefab].Enqueue(effect); // 放回对应预制体的池
    }

    public void RecycleEffect(GameObject prefab, GameObject effectobj)
    {
        effectobj.SetActive(false);
        if (!effectPools.ContainsKey(prefab))
        {
            effectPools[prefab] = new Queue<GameObject>();
        }

        effectPools[prefab].Enqueue(effectobj);
    }
}