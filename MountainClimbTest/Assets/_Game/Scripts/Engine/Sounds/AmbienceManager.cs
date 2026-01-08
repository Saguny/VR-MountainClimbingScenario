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
                
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayAmbience(AudioClip clip, bool fade = true)
        {
            if (globalAmbienceSource.clip == clip) return;

            if (fade)
            {
                if (ambienceFadeCoroutine != null) StopCoroutine(ambienceFadeCoroutine);
                ambienceFadeCoroutine = StartCoroutine(FadeToNewAmbience(clip));
            }
            else
            {
                globalAmbienceSource.clip = clip;
                globalAmbienceSource.loop = true;
                globalAmbienceSource.Play();
            }
        }

        private IEnumerator FadeToNewAmbience(AudioClip newClip, float duration = 1.5f)
        {
            float startVolume = globalAmbienceSource.volume;

            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                globalAmbienceSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2));
                yield return null;
            }

            globalAmbienceSource.volume = 0;
            globalAmbienceSource.Stop();
            globalAmbienceSource.clip = newClip;

            if (newClip != null)
            {
                globalAmbienceSource.Play();

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

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            float db = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20;
            mixer.SetFloat(musicExposedParam, db);
        }

        public void SetAmbienceVolume(float volume)
        {
            float db = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20;
            mixer.SetFloat(ambienceExposedParam, db);
        }
    }
}