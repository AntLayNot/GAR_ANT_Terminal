using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    public static bool IsPausedGlobal { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("UI Panels à cacher pendant la pause")]
    [SerializeField] private List<GameObject> panelsToHideWhilePaused = new();

    [Header("Scene Loading")]
    [SerializeField] private string menuSceneName = "Menu";

    [Tooltip("Si true, les panels seront remis dans leur état précédent à la fermeture du menu.")]
    [SerializeField] private bool restorePreviousPanelStates = true;

    [Header("Pause")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool startClosed = true;

    [Header("Input")]
    [SerializeField] private bool allowEscapeKey = true;

    [Header("Audio")]
    [SerializeField] private bool pauseAudio = false;

    private float defaultFixedDeltaTime;

    public bool IsOpen { get; private set; }

    private readonly Dictionary<GameObject, bool> previousPanelStates = new();

    private void Awake()
    {
        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (startClosed)
            CloseMenu();
        else
            OpenMenu();
    }

    private void Update()
    {
        if (!allowEscapeKey)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (IsOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        IsOpen = true;
        IsPausedGlobal = true;

        HideOtherPanels();

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (pauseTimeScale)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }

        if (pauseAudio)
            AudioListener.pause = true;

        Debug.Log("[PauseMenuController] Menu pause ouvert | TimeScale = " + Time.timeScale);
    }

    public void CloseMenu()
    {
        IsOpen = false;
        IsPausedGlobal = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        RestoreOtherPanels();

        if (pauseTimeScale)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }

        if (pauseAudio)
            AudioListener.pause = false;

        Debug.Log("[PauseMenuController] Menu pause fermé | TimeScale = " + Time.timeScale);
    }

    public void LoadMenuScene()
    {
        // On remet tout propre avant de changer de scène
        IsOpen = false;
        IsPausedGlobal = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        AudioListener.pause = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("[PauseMenuController] Chargement de la scène : " + menuSceneName);

        SceneManager.LoadScene(menuSceneName);
    }

    private void HideOtherPanels()
    {
        previousPanelStates.Clear();

        foreach (GameObject panel in panelsToHideWhilePaused)
        {
            if (panel == null)
                continue;

            if (panel == pausePanel)
                continue;

            previousPanelStates[panel] = panel.activeSelf;
            panel.SetActive(false);
        }
    }

    private void RestoreOtherPanels()
    {
        foreach (GameObject panel in panelsToHideWhilePaused)
        {
            if (panel == null)
                continue;

            if (panel == pausePanel)
                continue;

            if (restorePreviousPanelStates && previousPanelStates.TryGetValue(panel, out bool wasActive))
            {
                panel.SetActive(wasActive);
            }
            else
            {
                panel.SetActive(true);
            }
        }

        previousPanelStates.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        IsPausedGlobal = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        AudioListener.pause = false;
    }
}