using TMPro;
using UnityEngine;

namespace MountainRescue.UI.Views
{
    public class PressureTextView : BaseSensorView
    {
        [SerializeField] private TextMeshProUGUI pressureText;
        [SerializeField] private TextMeshProUGUI altText;

        private void Update()
        {
            if (Sensors == null) return;

            // Updates every frame - low overhead for simple text
            pressureText.text = $"{Sensors.GetPressureHPa():0} hPa";

            // Optional: Altitude reading
            if (altText != null)
                altText.text = $"ALT: {Sensors.GetAltitudeMeters():0}m";
        }
    }
}