using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

namespace MountainRescue.Engine
{
    public class AmbienceManager : MonoBehaviour
    {
        public static AmbienceManager Instance;

        [Header("Sources")]
        [SerializeField] private AudioSource globalAmbienceSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Settings")]
        [SerializeField] private float defaultFadeDuration = 2.0f;

        [Header("Mixer Settings")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string musicExposedParam = "MusicVol";
        [SerializeField] private string ambienceExposedParam = "AmbienceVol";

        private Coroutine ambienceFadeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Ensure this persists across scenes
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayAmbience(AudioClip clip, bool fade = true)
        {
            // If the clip is already playing, don't restart it
            if (globalAmbienceSource.clip == clip && globalAmbienceSource.isPlaying) return;

            if (fade)
            {
                if (ambienceFadeCoroutine != null) StopCoroutine(ambienceFadeCoroutine);
                ambienceFadeCoroutine = StartCoroutine(FadeToNewAmbience(clip, defaultFadeDuration));
            }
            else
            {
                globalAmbienceSource.clip = clip;
                globalAmbienceSource.loop = true;
                globalAmbienceSource.volume = 1.0f; // Ensure volume is up if not fading
                globalAmbienceSource.Play();
            }
        }

        private IEnumerator FadeToNewAmbience(AudioClip newClip, float duration)
        {
            float startVolume = globalAmbienceSource.isPlaying ? globalAmbienceSource.volume : 1.0f;

            // Fade Out
            if (globalAmbienceSource.isPlaying)
            {
                for (float t = 0; t < duration / 2; t += Time.deltaTime)
                {
                    globalAmbienceSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2));
                    yield return null;
                }
            }

            globalAmbienceSource.Stop();
            globalAmbienceSource.clip = newClip;
            globalAmbienceSource.volume = 0;

            if (newClip != null)
            {
                globalAmbienceSource.Play();
                globalAmbienceSource.loop = true;

                // Fade In
                for (float t = 0; t < duration / 2; t += Time.deltaTime)
                {
                    globalAmbienceSource.volume = Mathf.Lerp(0, startVolume, t / (duration / 2));
                    yield return null;
                }
                globalAmbienceSource.volume = startVolume;
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic() => musicSource.Stop();

        public void SetMusicVolume(float volume) => SetMixerVolume(musicExposedParam, volume);
        public void SetAmbienceVolume(float volume) => SetMixerVolume(ambienceExposedParam, volume);

        private void SetMixerVolume(string param, float volume)
        {
            float db = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20;
            mixer.SetFloat(param, db);
        }
    }
}