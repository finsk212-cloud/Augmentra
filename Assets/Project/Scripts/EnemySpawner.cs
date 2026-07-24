using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;

    public float spawnDistance = 12f;
    public float spawnRate = 2f;

    private float nextSpawnTime;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (enemyPrefab == null || player == null) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    private void SpawnEnemy()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized;

        Vector3 spawnPosition = player.position + new Vector3(
            randomCircle.x * spawnDistance,
            0f,
            randomCircle.y * spawnDistance
        );

        spawnPosition.y = 1f;

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}