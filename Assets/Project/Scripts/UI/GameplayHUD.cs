using TMPro;
using UnityEngine;

namespace Augmentra.UI
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WaveManager waveManager;

        [Header("Bars")]
        [SerializeField] private UIProgressBar healthBar;
        [SerializeField] private UIProgressBar manaBar;
        [SerializeField] private UIProgressBar experienceBar;

        [Header("Wave")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private WaveAnnouncementUI announcement;

        [Header("Resources")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI killsText;

        private bool hasStarted;

        public void Configure(
            PlayerController playerController,
            Health health,
            GameManager game,
            WaveManager waves,
            UIProgressBar hp,
            UIProgressBar mana,
            UIProgressBar experience,
            TextMeshProUGUI wave,
            TextMeshProUGUI enemies,
            TextMeshProUGUI countdown,
            TextMeshProUGUI gold,
            TextMeshProUGUI level,
            TextMeshProUGUI kills,
            WaveAnnouncementUI waveAnnouncement)
        {
            player = playerController;
            playerHealth = health;
            gameManager = game;
            waveManager = waves;
            healthBar = hp;
            manaBar = mana;
            experienceBar = experience;
            waveText = wave;
            enemyCountText = enemies;
            countdownText = countdown;
            goldText = gold;
            levelText = level;
            killsText = kills;
            announcement = waveAnnouncement;
        }

        private void OnEnable()
        {
            PlayerController.PlayerReady += OnPlayerReady;
            Subscribe();

            if (hasStarted)
            {
                ResolveSourcesOnce();
                Rebind();
                RefreshAll(true);
            }
        }

        private void Start()
        {
            // All scene Awake calls have completed here, so Health and mana contain real values.
            hasStarted = true;
            ResolveSourcesOnce();
            Rebind();
            RefreshAll(true);
        }

        private void OnDisable()
        {
            PlayerController.PlayerReady -= OnPlayerReady;
            Unsubscribe();
        }

        private void ResolveSourcesOnce()
        {
            if (player == null)
            {
                player = PlayerController.Instance != null
                    ? PlayerController.Instance
                    : FindFirstObjectByType<PlayerController>();
            }

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null
                    ? GameManager.Instance
                    : FindFirstObjectByType<GameManager>();
            }

            if (waveManager == null)
            {
                waveManager = WaveManager.Instance != null
                    ? WaveManager.Instance
                    : FindFirstObjectByType<WaveManager>();
            }
        }

        private void OnPlayerReady(PlayerController readyPlayer)
        {
            if (readyPlayer == null)
            {
                return;
            }

            Unsubscribe();
            player = readyPlayer;
            playerHealth = readyPlayer.GetComponent<Health>();
            Subscribe();
            RefreshAll(true);
        }

        private void Rebind()
        {
            Unsubscribe();
            Subscribe();
        }

        private void Subscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged += OnHealthChanged;
                playerHealth.Damaged += OnPlayerDamaged;
                playerHealth.Died += OnPlayerDied;
            }

            if (player != null)
            {
                player.ManaChanged += OnManaChanged;
            }

            if (gameManager != null)
            {
                gameManager.GoldChanged += OnGoldChanged;
                gameManager.LevelChanged += OnLevelChanged;
                gameManager.ExperienceChanged += OnExperienceChanged;
                gameManager.KillsChanged += OnKillsChanged;
            }

            if (waveManager != null)
            {
                waveManager.WaveChanged += OnWaveChanged;
                waveManager.EnemyCountChanged += OnEnemyCountChanged;
                waveManager.CountdownChanged += OnCountdownChanged;
                waveManager.StateChanged += OnWaveStateChanged;
                waveManager.WaveStarted += OnWaveStarted;
                waveManager.WaveCleared += OnWaveCleared;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnHealthChanged;
                playerHealth.Damaged -= OnPlayerDamaged;
                playerHealth.Died -= OnPlayerDied;
            }

            if (player != null)
            {
                player.ManaChanged -= OnManaChanged;
            }

            if (gameManager != null)
            {
                gameManager.GoldChanged -= OnGoldChanged;
                gameManager.LevelChanged -= OnLevelChanged;
                gameManager.ExperienceChanged -= OnExperienceChanged;
                gameManager.KillsChanged -= OnKillsChanged;
            }

            if (waveManager != null)
            {
                waveManager.WaveChanged -= OnWaveChanged;
                waveManager.EnemyCountChanged -= OnEnemyCountChanged;
                waveManager.CountdownChanged -= OnCountdownChanged;
                waveManager.StateChanged -= OnWaveStateChanged;
                waveManager.WaveStarted -= OnWaveStarted;
                waveManager.WaveCleared -= OnWaveCleared;
            }
        }

        private void RefreshAll(bool immediate)
        {
            if (playerHealth != null)
            {
                UpdateHealth(playerHealth.CurrentHealth, playerHealth.maxHealth, immediate);
            }

            if (player != null)
            {
                UpdateMana(player.Mana, player.MaxMana, immediate);
            }

            if (gameManager != null)
            {
                OnGoldChanged(gameManager.gold);
                OnLevelChanged(gameManager.level);
                UpdateExperience(gameManager.xp, gameManager.xpToNextLevel, immediate);
                OnKillsChanged(gameManager.kills);
            }

            if (waveManager != null)
            {
                OnWaveChanged(waveManager.CurrentWave);
                OnEnemyCountChanged(waveManager.EnemiesAlive);
                OnWaveStateChanged(waveManager.CurrentState);
                OnCountdownChanged(Mathf.CeilToInt(waveManager.CountdownRemaining));
            }
        }

        private void OnHealthChanged(float current, float maximum)
        {
            UpdateHealth(current, maximum, false);
        }

        private void UpdateHealth(float current, float maximum, bool immediate)
        {
            if (immediate)
            {
                healthBar?.SetValueImmediate(current, maximum);
            }
            else
            {
                healthBar?.SetValue(current, maximum);
            }

            healthBar?.SetLowHealth(
                maximum > 0f &&
                current / maximum < 0.3f &&
                current > 0f);
        }

        private void OnPlayerDamaged(float amount, DamageType type)
        {
            healthBar?.PulseDamage();
        }

        private void OnPlayerDied()
        {
            healthBar?.SetLowHealth(false);
        }

        private void OnManaChanged(float current, float maximum)
        {
            UpdateMana(current, maximum, false);
        }

        private void UpdateMana(float current, float maximum, bool immediate)
        {
            if (immediate)
            {
                manaBar?.SetValueImmediate(current, maximum);
            }
            else
            {
                manaBar?.SetValue(current, maximum);
            }

            manaBar?.SetUnavailable(false);
        }

        private void OnGoldChanged(int amount)
        {
            if (goldText != null)
            {
                goldText.text = amount.ToString();
            }
        }

        private void OnLevelChanged(int level)
        {
            if (levelText != null)
            {
                levelText.text = level.ToString();
            }
        }

        private void OnExperienceChanged(int current, int required)
        {
            UpdateExperience(current, required, false);
        }

        private void UpdateExperience(int current, int required, bool immediate)
        {
            if (immediate)
            {
                experienceBar?.SetValueImmediate(current, required);
            }
            else
            {
                experienceBar?.SetValue(current, required);
            }
        }

        private void OnKillsChanged(int kills)
        {
            if (killsText != null)
            {
                killsText.text = kills.ToString();
            }
        }

        private void OnWaveChanged(int wave)
        {
            if (waveText != null)
            {
                waveText.text = "WAVE " + Mathf.Max(1, wave);
            }
        }

        private void OnEnemyCountChanged(int remaining)
        {
            if (enemyCountText != null)
            {
                enemyCountText.text =
                    remaining == 1 ? "1 ENEMY REMAINING" : remaining + " ENEMIES REMAINING";
            }
        }

        private void OnCountdownChanged(int seconds)
        {
            if (countdownText != null && waveManager != null &&
                waveManager.CurrentState == WaveManager.State.Countdown)
            {
                countdownText.text = seconds > 0 ? "NEXT WAVE IN " + seconds : string.Empty;
            }
        }

        private void OnWaveStateChanged(WaveManager.State state)
        {
            if (countdownText != null && state != WaveManager.State.Countdown)
            {
                countdownText.text = string.Empty;
            }
        }

        private void OnWaveStarted(int wave)
        {
            announcement?.ShowWave(wave);
        }

        private void OnWaveCleared(int wave)
        {
            announcement?.ShowCleared();
        }
    }
}
