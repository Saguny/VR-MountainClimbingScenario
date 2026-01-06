using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject controlsPanel;

    [SerializeField] private string gameSceneName = "Game";

    // ---------------- MAIN MENU ----------------

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // ---------------- OPTIONS ----------------

    public void BackToMainMenu()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenAudio()
    {
        CloseAllSubOptions();
        audioPanel.SetActive(true);
    }

    public void OpenGraphics()
    {
        CloseAllSubOptions();
        graphicsPanel.SetActive(true);
    }

    public void OpenControls()
    {
        CloseAllSubOptions();
        controlsPanel.SetActive(true);
    }

    private void CloseAllSubOptions()
    {
        audioPanel.SetActive(false);
        graphicsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }
}
