using TMPro;
using UnityEngine;

namespace MountainRescue.UI.Views
{
    public class DirectionTextView : BaseSensorView
    {
        [SerializeField] private TextMeshProUGUI indicatorText;

        [Header("Visual Config")]
        [SerializeField] private string textUp = "▲ UP";
        [SerializeField] private string textDown = "▼ DOWN";
        [SerializeField] private string textNeutral = "● LEVEL";

        [SerializeField] private Color colorWarning = Color.yellow;
        [SerializeField] private Color colorGood = Color.green;

        protected override void Start()
        {
            base.Start();
            if (Sensors != null)
            {
                Sensors.OnStateChanged += UpdateVisuals; // Subscribe to event
                UpdateVisuals(Sensors.GetCurrentState()); // Initial set
            }
        }

        private void OnDestroy()
        {
            if (Sensors != null) Sensors.OnStateChanged -= UpdateVisuals;
        }

        private void UpdateVisuals(VerticalGuidanceState state)
        {
            switch (state)
            {
                case VerticalGuidanceState.TargetIsAbove:
                    indicatorText.text = textUp;
                    indicatorText.color = colorWarning;
                    break;
                case VerticalGuidanceState.TargetIsBelow:
                    indicatorText.text = textDown;
                    indicatorText.color = colorWarning;
                    break;
                case VerticalGuidanceState.Neutral:
                    indicatorText.text = textNeutral;
                    indicatorText.color = colorGood;
                    break;
            }
        }
    }
}