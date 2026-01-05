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

        // --- AMBIENCE LOGIC ---

        public void PlayAmbience(AudioClip clip, bool fade = true)
        {
            if (globalAmbienceSource.clip == clip) return;

            globalAmbienceSource.clip = clip;
            globalAmbienceSource.loop = true;
            globalAmbienceSource.Play();
        }

        // --- MUSIC LOGIC ---

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

        // --- VOLUME CONTROL (Using Mixer) ---

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