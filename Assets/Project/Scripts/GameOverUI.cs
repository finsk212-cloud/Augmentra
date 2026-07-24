using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public GameObject panel;
    public TextMeshProUGUI statsText;
    public Button restartButton;
    public Button mainMenuButton;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartRun());
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
        }
    }

    public void Show(int kills, int gold, int level)
    {
        if (statsText != null)
        {
            statsText.text = "Kills: " + kills + "\nGold: " + gold + "\nLevel: " + level;
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }
}
