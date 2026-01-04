using UnityEngine;
using UnityEngine.Audio;

namespace MountainRescue.Engine
{
    public class AmbienceManager : MonoBehaviour
    {
        public static AmbienceManager Instance;

        [SerializeField] private AudioSource globalWindSource;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetAmbienceVolume(float volume)
        {
            globalWindSource.volume = Mathf.Clamp01(volume);
        }

        public void PlayEnvironmentLoop(AudioClip clip)
        {
            if (globalWindSource.clip == clip) return;
            globalWindSource.clip = clip;
            globalWindSource.loop = true;
            globalWindSource.Play();
        }
    }
}