using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PlayerController player;
    public Transform playerTransform;

    public int kills;
    public int gold;
    public int xp;
    public int level = 1;
    public int xpToNextLevel = 5;

    public GameObject goldPopupPrefab;

    public enum GameState { Playing, ChoosingAugment, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public bool IsChoosingUpgrade => CurrentState == GameState.ChoosingAugment;
    public event Action<int> GoldChanged;
    public event Action<int> LevelChanged;
    public event Action<int, int> ExperienceChanged;
    public event Action<int> KillsChanged;

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        EnemyController.SpeedMultiplier = 1f;
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
    }

    public void EnemyKilled()
    {
        kills++;
        gold++;
        KillsChanged?.Invoke(kills);
        GoldChanged?.Invoke(gold);
        SpawnGoldPopup();

        if (player != null && player.bloodstoneActive)
        {
            player.bloodstoneDamageBonus += 0.5f;
            player.bloodstoneTimer = 3f;
        }

        xp++;

        if (xp >= xpToNextLevel)
        {
            xp = 0;
            level++;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.4f);
            LevelChanged?.Invoke(level);
        }

        ExperienceChanged?.Invoke(xp, xpToNextLevel);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || gold < amount)
        {
            return false;
        }

        gold -= amount;
        GoldChanged?.Invoke(gold);
        return true;
    }

    private void SpawnGoldPopup()
    {
        if (goldPopupPrefab == null) return;

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (playerTransform == null) return;

        Vector3 spawnPosition = playerTransform.position + Vector3.up * 2f;
        Instantiate(goldPopupPrefab, spawnPosition, Quaternion.identity);
    }

    public void PlayerDied()
    {
        if (CurrentState == GameState.GameOver) return;

        SetState(GameState.GameOver);
        Time.timeScale = 0f;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StopSpawning();
        }

        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.Show(kills, gold, level);
        }
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }
    }
}
