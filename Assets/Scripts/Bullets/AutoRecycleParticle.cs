using UnityEngine;
using System.Collections;

public class AutoRecycleParticle : MonoBehaviour
{
    public GameObject prefabKey;
    public float lifeTime = 0.2f;

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(DisableAfterTime());
    }

    IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        ParticleEffectPool.Instance.RecycleEffect(prefabKey, gameObject);
    }
}
