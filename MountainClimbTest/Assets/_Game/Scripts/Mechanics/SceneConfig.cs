using UnityEngine;
using MountainRescue.Systems; // Required to see PlayerSensorSuite

public class SceneWeatherConfig : MonoBehaviour
{
    [Header("Atmospheric Settings")]
    public float sceneSeaLevelPressure = 1013.25f;

    [Header("Snow Settings")]
    public float snowEmissionRate = 50f;

    void Start()
    {
        // 1. Find the Player (assuming the DDOL player has the "Player" tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 2. Update the Sensor (Sea Level Pressure)
            PlayerSensorSuite sensor = player.GetComponentInChildren<PlayerSensorSuite>();
            if (sensor != null)
            {
                // Note: seaLevelPressureHPa must be public in your PlayerSensorSuite script
                // or you can add a public method to change it.
                SetPrivateField(sensor, "seaLevelPressureHPa", sceneSeaLevelPressure);
                Debug.Log($"Scene Config: Set Pressure to {sceneSeaLevelPressure}");
            }

            // 3. Update the Snow Particle System
            ParticleSystem ps = player.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = snowEmissionRate;
                Debug.Log($"Scene Config: Set Snow to {snowEmissionRate}");
            }
        }
    }

    // Helper if the variable is private (Alternative: Change variable to public in Sensor script)
    private void SetPrivateField(PlayerSensorSuite target, string fieldName, float value)
    {
        var field = typeof(PlayerSensorSuite).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
    }
}