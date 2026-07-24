using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private int lastCountdownSecond = -1;

    public State CurrentState => state;
    public int CurrentWave => currentWave;
    public int EnemiesAlive => enemiesAlive;
    public float CountdownRemaining => countdownRemaining;

    public event Action<int> WaveChanged;
    public event Action<int> EnemyCountChanged;
    public event Action<int> CountdownChanged;
    public event Action<State> StateChanged;
    public event Action<int> WaveStarted;
    public event Action<int> WaveCleared;

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
        WaveChanged?.Invoke(currentWave);
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

        WaveStarted?.Invoke(currentWave);
        yield return StartCoroutine(SpawnWave());
        if (stopped) yield break;

        SetState(State.WaitingForClear);

        while (enemiesAlive > 0)
        {
            if (stopped) yield break;
            yield return null;
        }

        SetState(State.WaveComplete);
        WaveCleared?.Invoke(currentWave);

        if (currentWave >= totalWaves)
        {
            SetState(State.Victory);
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
        SetState(State.Countdown);
        countdownRemaining = 3f;
        lastCountdownSecond = -1;

        while (countdownRemaining > 0f)
        {
            int shownSecond = Mathf.CeilToInt(countdownRemaining);

            if (shownSecond != lastCountdownSecond)
            {
                lastCountdownSecond = shownSecond;
                CountdownChanged?.Invoke(shownSecond);
            }

            countdownRemaining -= Time.deltaTime;
            yield return null;
        }

        countdownRemaining = 0f;
        CountdownChanged?.Invoke(0);
    }

    private IEnumerator SpawnWave()
    {
        SetState(State.Spawning);

        int count = GetEnemyCount(currentWave);
        enemiesAlive = count;
        EnemyCountChanged?.Invoke(enemiesAlive);

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
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

        EnemyCountChanged?.Invoke(enemiesAlive);
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    private void SetState(State newState)
    {
        state = newState;
        StateChanged?.Invoke(state);
    }
}
