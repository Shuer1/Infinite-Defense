using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Separation Settings")]
    public float separationRadius = 1.5f;   // 敌人最小间距
    public float separationForce = 3f;      // 推开力度

    private List<EnemyBase> enemies = new List<EnemyBase>();

    void Update()
    {
        enemies.RemoveAll(e => e == null);

        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 separation = Vector3.zero;

            for (int j = 0; j < enemies.Count; j++)
            {
                if (i == j) continue;

                Vector3 away = enemies[i].transform.position - enemies[j].transform.position;
                float dist = away.magnitude;

                if (dist > 0 && dist < separationRadius)
                {
                    separation += away.normalized * (separationForce / dist);
                }
            }

            enemies[i].transform.position += separation * Time.deltaTime;
        }
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }
}
