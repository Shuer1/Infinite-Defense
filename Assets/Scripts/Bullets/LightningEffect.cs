using UnityEngine;
using System.Collections;

public class LightningEffect : MonoBehaviour
{
    private LineRenderer lr;
    private Transform startTarget;
    private Transform endTarget;
    private float duration = 0.25f;
    private GameObject prefabKey;

    public void Init(Transform start, Transform end, GameObject prefab, float duration = 0.25f)
    {
        this.prefabKey = prefab;
        this.startTarget = start;
        this.endTarget = end;
        this.duration = duration;

        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start.position);
            lr.SetPosition(1, end.position);
        }

        StopAllCoroutines();
        StartCoroutine(FollowTargets());
    }

    private IEnumerator FollowTargets()
    {
        float t = 0f;
        while (t < duration)
        {
            if (lr != null && startTarget != null && endTarget != null)
            {
                lr.SetPosition(0, startTarget.position);
                lr.SetPosition(1, endTarget.position);
            }
            t += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        ParticleEffectPool.Instance.RecycleEffect(prefabKey, this.gameObject);
    }
}
