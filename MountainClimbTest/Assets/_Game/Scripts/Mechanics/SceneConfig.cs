using UnityEngine;
using MountainRescue.Systems;
using MountainRescue.Engine;

public class SceneMasterConfig : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip sceneMusic;
    public AudioClip sceneAmbience;

    [Header("Atmosphere")]
    public float seaLevelPressure = 1013.25f;
    public float snowRate = 50f;

    [Header("Y-Following")]
    public GameObject objectToFollowPlayer;

    void Start()
    {
        // 1. Audio
        if (AmbienceManager.Instance != null)
        {
            if (sceneMusic != null) AmbienceManager.Instance.PlayMusic(sceneMusic);
            if (sceneAmbience != null) AmbienceManager.Instance.PlayAmbience(sceneAmbience);
        }

        // 2. Player-Related (Snow & Pressure)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Set Snow
            ParticleSystem ps = player.GetComponentInChildren<ParticleSystem>();
            if (ps != null) { var em = ps.emission; em.rateOverTime = snowRate; }

            // Set Pressure
            PlayerSensorSuite sensor = player.GetComponentInChildren<PlayerSensorSuite>();
            if (sensor != null) SetPrivateField(sensor, "seaLevelPressureHPa", seaLevelPressure);

            // 3. Setup Follower
            if (objectToFollowPlayer != null)
            {
                // Add the component if it's missing, or just update the reference
                var f = objectToFollowPlayer.GetComponent<FollowPlayerY>() ?? objectToFollowPlayer.AddComponent<FollowPlayerY>();
                // In your FollowPlayerY, you can make playerTransform public so we set it here
            }
        }
    }

    private void SetPrivateField(PlayerSensorSuite target, string fieldName, float value)
    {
        var field = typeof(PlayerSensorSuite).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
    }
}