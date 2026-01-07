using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider dialogueSlider;
    public Slider ambienceSlider;

    private void Start()
    {
        LoadVolumes();
        ApplyAllVolumes();
    }

    // ---------- SETTERS (für Slider OnValueChanged) ----------

    public void SetMasterVolume(float value)
    {
        SetVolume("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume("SFXVolume", value);
    }

    public void SetDialogueVolume(float value)
    {
        SetVolume("DialogueVolume", value);
    }

    public void SetAmbienceVolume(float value)
    {
        SetVolume("AmbienceVolume", value);
    }

    // ---------- CORE ----------

    private void SetVolume(string parameter, float sliderValue)
    {
        if (sliderValue <= 0.0001f)
            audioMixer.SetFloat(parameter, -80f);
        else
            audioMixer.SetFloat(parameter, Mathf.Log10(sliderValue) * 20f);

        PlayerPrefs.SetFloat(parameter, sliderValue);
    }


    private void ApplyAllVolumes()
    {
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);
        SetDialogueVolume(dialogueSlider.value);
        SetAmbienceVolume(ambienceSlider.value);
    }

    private void LoadVolumes()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        dialogueSlider.value = PlayerPrefs.GetFloat("DialogueVolume", 1f);
        ambienceSlider.value = PlayerPrefs.GetFloat("AmbienceVolume", 1f);
    }
}
