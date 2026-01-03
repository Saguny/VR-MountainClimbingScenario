using UnityEngine;
using TMPro;
using MountainRescue.Systems;

namespace MountainRescue.UI
{
    public class StaminaTextDisplay : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BreathManager breathManager;
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Formatting")]
        [SerializeField] private string prefix = "STAMINA: ";

        private void Awake()
        {
            // Set the static starting value immediately so the user doesn't see "0%"
            if (staminaText != null)
            {
                staminaText.text = $"{prefix}100%";
            }
        }

        private void OnEnable()
        {
            if (breathManager != null)
            {
                // This will only update the text once stamina actually changes
                breathManager.onStaminaChanged.AddListener(UpdateStaminaText);
            }
        }

        private void OnDisable()
        {
            if (breathManager != null)
            {
                breathManager.onStaminaChanged.RemoveListener(UpdateStaminaText);
            }
        }

        private void UpdateStaminaText(float normalizedStamina)
        {
            if (staminaText == null) return;

            // Once BreathManager sends its first update, this takes over
            float percent = normalizedStamina * 100f;
            staminaText.text = $"{prefix}{percent:F0}%";
        }
    }
}