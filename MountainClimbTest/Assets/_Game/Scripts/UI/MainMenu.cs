using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject audioPanel;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource menuMusic;
    [SerializeField] private float fadeDuration = 1.5f;

    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        if (menuMusic != null)
        {
            menuMusic.volume = 0;
            menuMusic.Play();
            StartCoroutine(FadeAudio(menuMusic, 1f, fadeDuration));
        }
    }

    // ---------------- MAIN MENU ----------------

    public void PlayGame()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        if (menuMusic != null)
        {
            yield return StartCoroutine(FadeAudio(menuMusic, 0f, fadeDuration));
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
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