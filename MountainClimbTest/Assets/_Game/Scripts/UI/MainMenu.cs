using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    public GameObject audioPanel;

    [SerializeField] private string gameSceneName = "Game";

    // ---------------- MAIN MENU ----------------

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        CloseAllSubOptions();
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
        Debug.Log("OpenAudio gedrückt");
        Debug.Log(audioPanel.name);
        audioPanel.SetActive(true);
    }

    private void CloseAllSubOptions()
    {
        Debug.Log("Close all Suboptions");
        audioPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }
}
