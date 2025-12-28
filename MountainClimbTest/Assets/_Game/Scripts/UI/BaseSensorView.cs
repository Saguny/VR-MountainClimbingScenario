using UnityEngine;
using MountainRescue.Systems;

namespace MountainRescue.UI.Views
{
    public abstract class BaseSensorView : MonoBehaviour
    {
        // In the inspector, drag the object that has the PlayerSensorSuite
        [SerializeField] protected GameObject sensorDataSource;

        protected PlayerSensorSuite Sensors { get; private set; }

        protected virtual void Start()
        {
            if (sensorDataSource != null)
            {
                Sensors = sensorDataSource.GetComponent<PlayerSensorSuite>();
            }

            if (Sensors == null)
            {
                Debug.LogError($"[UI] {name} is missing a link to PlayerSensorSuite!");
            }
        }
    }
}