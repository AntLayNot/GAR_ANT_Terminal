using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Credits")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Options")]
    [SerializeField] private bool hideCreditsOnStart = true;

    private void Start()
    {
        if (creditsPanel != null && hideCreditsOnStart)
            creditsPanel.SetActive(false);
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("MainMenuManager : le nom de la scène de jeu est vide.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenCredits()
    {
        if (creditsPanel == null)
        {
            Debug.LogWarning("MainMenuManager : aucun Credits Panel assigné.");
            return;
        }

        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel == null)
            return;

        creditsPanel.SetActive(false);
    }

    public void ToggleCredits()
    {
        if (creditsPanel == null)
        {
            Debug.LogWarning("MainMenuManager : aucun Credits Panel assigné.");
            return;
        }

        creditsPanel.SetActive(!creditsPanel.activeSelf);
    }

    public void Quit()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}