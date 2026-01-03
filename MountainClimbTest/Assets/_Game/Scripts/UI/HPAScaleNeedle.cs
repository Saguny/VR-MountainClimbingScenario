using MountainRescue.Systems;
using UnityEngine;

public class HPAScaleNeedle : MonoBehaviour
{
    [SerializeField] private PlayerSensorSuite sensorSuite;
    [SerializeField] private GameObject gaugeNeedle;

    [Header("Fixed Orientation")]
    [Tooltip("Set these to the Y and Z rotation your needle should always have")]
    [SerializeField] private float fixedY = -88.396f;
    [SerializeField] private float fixedZ = -90f;

    private const float MAX_PRESSURE = 1013f;
    private const float MIN_ROTATION = 0f;
    private const float MAX_ROTATION = -178f;

    private void Update()
    {
        if (sensorSuite == null || gaugeNeedle == null) return;

        // 1. Get the pressure and calculate the target X
        float currentPressure = sensorSuite.GetPressureHPa();
        float pressureFactor = Mathf.Clamp01(currentPressure / MAX_PRESSURE);
        float targetX = Mathf.Lerp(MIN_ROTATION, MAX_ROTATION, pressureFactor);

        // 2. APPLY DIRECTLY without reading current rotation.
        // This prevents the "Euler Flip" jitter entirely.
        gaugeNeedle.transform.localRotation = Quaternion.Euler(targetX, fixedY, fixedZ);
    }
}
