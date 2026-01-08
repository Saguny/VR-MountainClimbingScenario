using UnityEngine;
using MountainRescue.Systems;
using MountainRescue.Engine;

public class SceneMasterConfig : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip sceneMusic;
    public AudioClip sceneAmbience;
    public bool useCrossfade = true;

    [Header("Atmosphere")]
    public float seaLevelPressure = 1013.25f;
    public float snowRate = 50f;

    [Header("Rescue Target")]
    public Transform sceneTargetOverride;

    [Header("Y-Following")]
    public GameObject objectToFollowPlayer;

    void Start()
    {
        // 1. Audio Crossfade
        if (AmbienceManager.Instance != null)
        {
            if (sceneMusic != null)
                AmbienceManager.Instance.PlayMusic(sceneMusic);

            // This will trigger the FadeToNewAmbience coroutine in the manager
            AmbienceManager.Instance.PlayAmbience(sceneAmbience, useCrossfade);
        }

        // 2. Rescue Target Setup
        if (RescueTargetManager.Instance != null)
        {
            if (sceneTargetOverride != null)
                RescueTargetManager.Instance.SetTarget(sceneTargetOverride);
            else
                RescueTargetManager.Instance.FindTargetInScene();
        }

        // 3. Player Systems
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ApplyPlayerSettings(player);
        }
    }

    private void ApplyPlayerSettings(GameObject player)
    {
        // Snow
        ParticleSystem ps = player.GetComponentInChildren<ParticleSystem>();
        if (ps != null) { var em = ps.emission; em.rateOverTime = snowRate; }

        // Pressure
        PlayerSensorSuite sensor = player.GetComponentInChildren<PlayerSensorSuite>();
        if (sensor != null) sensor.seaLevelPressureHPa = seaLevelPressure;

        // Y-Follower
        if (objectToFollowPlayer != null)
        {
            var f = objectToFollowPlayer.GetComponent<FollowPlayerY>() ?? objectToFollowPlayer.AddComponent<FollowPlayerY>();
            // If FollowPlayerY has a target field, set it here:
            // f.target = player.transform;
        }
    }
}