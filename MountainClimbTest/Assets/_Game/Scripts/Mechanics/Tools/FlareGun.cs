using MountainRescue.Systems;
using MountainRescue.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x

namespace Game.Mechanics.Tools
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class FlareGun : MonoBehaviour
    {
        [Header("Requirements")]
        [Tooltip("The Rescue Target/Objective.")]
        [SerializeField] private Transform objectiveTarget;

        [Tooltip("Max distance (meters) to allow firing.")]
        [SerializeField] private float requiredRange = 1.5f;

        [Tooltip("Reference to your existing Sensor Suite.")]
        [SerializeField] private PlayerSensorSuite sensorSuite;

        [Tooltip("Max allowed inclination (e.g. 10 degrees). Set to match your sensor's output scale.")]
        [SerializeField] private float maxLevelDeviation = 10.0f;

        [Header("Feedback")]
        [SerializeField] private TMP_Text warningSubtitle; // Drag your World Space Text here
        [SerializeField] private float warningDuration = 3.0f;

        [Header("Projectile")]
        [SerializeField] private GameObject flareProjectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("End Sequence")]
        [SerializeField] private float rescueDelay = 60f;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip rescueHelicopterSound;
        [SerializeField] private HeadsetFader headsetFader;
        [SerializeField] private string nextSceneName = "EndScene";

        private XRGrabInteractable _interactable;
        private AudioSource _audioSource;
        private bool _hasFired = false;
        private Coroutine _warningCoroutine;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _audioSource = GetComponent<AudioSource>();
            if (warningSubtitle != null) warningSubtitle.gameObject.SetActive(false);
        }

        private void OnEnable() => _interactable.activated.AddListener(OnTriggerPulled);
        private void OnDisable() => _interactable.activated.RemoveListener(OnTriggerPulled);

        private void OnTriggerPulled(ActivateEventArgs args)
        {
            if (_hasFired) return;

            if (!CanFire(out string failReason))
            {
                ShowWarning(failReason);
                return;
            }

            FireFlare();
        }

        private bool CanFire(out string reason)
        {
            reason = "";

            // 1. Proximity Check
            if (objectiveTarget != null)
            {
                float dist = Vector3.Distance(transform.position, objectiveTarget.position);
                if (dist > requiredRange)
                {
                    reason = $"I need to save this for when I reach the victim";
                    return false;
                }
            }

            // 2. Level/Stability Check
            if (sensorSuite != null)
            {
                // NOTE: Replace 'VerticalInclination' with the exact property name from your PlayerSensorSuite
                // Example: float currentLevel = sensorSuite.VerticalInclination;
                // For now, we assume 0 is perfect level.

                float currentLevel = 0f; // placeholder

                // If you implemented IVerticalGuidanceProvider, you might cast it:
                // var guidance = sensorSuite as IVerticalGuidanceProvider;
                // if (guidance != null) currentLevel = guidance.GetVerticalDeviation();

                if (Mathf.Abs(currentLevel) > maxLevelDeviation)
                {
                    reason = "Unstable Position! Level yourself.";
                    return false;
                }
            }

            return true;
        }

        private void ShowWarning(string message)
        {
            if (warningSubtitle == null) return;

            if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
            _warningCoroutine = StartCoroutine(DisplayWarningRoutine(message));
        }

        private IEnumerator DisplayWarningRoutine(string message)
        {
            warningSubtitle.text = message;
            warningSubtitle.gameObject.SetActive(true);

            // Optional: Make text face player immediately
            if (Camera.main != null)
            {
                warningSubtitle.transform.rotation = Quaternion.LookRotation(warningSubtitle.transform.position - Camera.main.transform.position);
            }

            yield return new WaitForSeconds(warningDuration);
            warningSubtitle.gameObject.SetActive(false);
        }

        private void FireFlare()
        {
            _hasFired = true;
            if (warningSubtitle != null) warningSubtitle.gameObject.SetActive(false);
            if (fireSound) _audioSource.PlayOneShot(fireSound);

            // Spawn Projectile
            if (flareProjectilePrefab != null && firePoint != null)
            {
                // Spawn independent of gun rotation so we can force it up
                GameObject flare = Instantiate(flareProjectilePrefab, firePoint.position, Quaternion.identity);
                var flareScript = flare.GetComponent<FlareProjectile>();
                if (flareScript != null) flareScript.Launch();
            }

            StartCoroutine(RescueSequenceRoutine());
        }

        private IEnumerator RescueSequenceRoutine()
        {
            yield return new WaitForSeconds(rescueDelay);

            if (rescueHelicopterSound) _audioSource.PlayOneShot(rescueHelicopterSound);

            if (headsetFader != null)
            {
                // Assuming standard FadeIn/FadeOut naming. Adjust if your method is named differently.
                headsetFader.FadeIn(); // Fading to black usually means "Fade In" the black overlay
                yield return new WaitForSeconds(2.0f);
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}