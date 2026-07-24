using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public GameObject enemyPrefab;
    public Transform player;
    public int totalWaves = 10;
    public float spawnRadius = 15f;
    public float timeBetweenSpawns = 0.8f;
    public float timeBetweenWaves = 3f;
    public float arenaBoundary = 19f;

    [Header("Enemy Count")]
    public int baseEnemyCount = 8;
    public int enemiesPerWave = 3;
    public int maxEnemyCount = 0;

    public enum State { Countdown, Spawning, WaitingForClear, WaveComplete, Victory }

    private State state;
    private int currentWave;
    private int enemiesAlive;
    private float countdownRemaining;
    private bool stopped;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        StartCountdown();
    }

    public void StartCountdown()
    {
        currentWave++;
        StartCoroutine(RunWave());
    }

    public void StopSpawning()
    {
        stopped = true;
        StopAllCoroutines();
    }

    private IEnumerator RunWave()
    {
        if (stopped) yield break;

        yield return StartCoroutine(Countdown());
        if (stopped) yield break;

        yield return StartCoroutine(SpawnWave());
        if (stopped) yield break;

        state = State.WaitingForClear;

        while (enemiesAlive > 0)
        {
            if (stopped) yield break;
            yield return null;
        }

        state = State.WaveComplete;

        if (currentWave >= totalWaves)
        {
            state = State.Victory;
            yield break;
        }

        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.ShowAugmentPicker(currentWave);
        }
        else if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenBreak();
        }
        else
        {
            StartCountdown();
        }
    }

    private IEnumerator Countdown()
    {
        state = State.Countdown;
        countdownRemaining = 3f;

        while (countdownRemaining > 0f)
        {
            countdownRemaining -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator SpawnWave()
    {
        state = State.Spawning;

        int count = GetEnemyCount(currentWave);

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            enemiesAlive++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    private int GetEnemyCount(int wave)
    {
        int count = baseEnemyCount + enemiesPerWave * (wave - 1);

        if (maxEnemyCount > 0)
        {
            count = Mathf.Min(count, maxEnemyCount);
        }

        return Mathf.Max(0, count);
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 center = player != null ? player.position : transform.position;

        float x = Mathf.Clamp(center.x + circle.x, -arenaBoundary, arenaBoundary);
        float z = Mathf.Clamp(center.z + circle.y, -arenaBoundary, arenaBoundary);
        Vector3 spawnPosition = new Vector3(x, 1f, z);

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }

    public void EnemyDied()
    {
        enemiesAlive--;

        if (enemiesAlive < 0)
        {
            enemiesAlive = 0;
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }
}
