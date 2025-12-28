using TMPro;
using UnityEngine;

namespace MountainRescue.UI.Views
{
    public class DistanceTextView : BaseSensorView
    {
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private string format = "{0:F1} m";

        private void Update()
        {
            if (Sensors == null) return;

            if (Sensors.HasValidTarget())
            {
                float dist = Sensors.GetDistanceToTarget();
                valueText.text = string.Format(format, dist);
            }
            else
            {
                valueText.text = "--.-";
            }
        }
    }
}