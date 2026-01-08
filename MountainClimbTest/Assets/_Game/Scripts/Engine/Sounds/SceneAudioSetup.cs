using UnityEngine;

namespace MountainRescue.Engine
{
    public class SceneAudioSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private AudioClip sceneMusic;
        [SerializeField] private AudioClip sceneAmbience;
        [SerializeField] private bool crossfade = true;

        private void Start()
        {
            if (AmbienceManager.Instance != null) return;

            if (sceneMusic != null)
            {
                AmbienceManager.Instance.PlayMusic(sceneMusic, crossfade);
            }

            if (sceneAmbience != null)
            {
                AmbienceManager.Instance.PlayAmbience(sceneAmbience, crossfade);
            }
        }

    }
}