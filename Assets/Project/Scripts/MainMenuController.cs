using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";

    public void OnPlayClicked()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}
