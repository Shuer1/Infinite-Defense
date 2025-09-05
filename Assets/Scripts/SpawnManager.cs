using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public float spawnRadius = 10f;

    void Start()
    {
        InvokeRepeating("SpawnEnemy", 1f, spawnInterval);
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = Random.onUnitSphere * spawnRadius;
        spawnPos.y = 0; // 平面生成
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
