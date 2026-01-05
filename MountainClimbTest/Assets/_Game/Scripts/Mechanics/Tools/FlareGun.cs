using MountainRescue.Systems;
using MountainRescue.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Game.Mechanics.Tools
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class FlareGun : MonoBehaviour
    {
        [Header("Requirements")]
        [SerializeField] private Transform objectiveTarget;
        [SerializeField] private float requiredRange = 1.5f;
        [SerializeField] private PlayerSensorSuite sensorSuite;
        [SerializeField] private float maxLevelDeviation = 10.0f;

        [Header("Feedback")]
        [SerializeField] private TMP_Text warningSubtitle;
        [SerializeField] private float warningDuration = 3.0f;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioSource warningAudioSource; // Dedicated source for warnings

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
        private AudioSource _mainAudioSource; // Renamed for clarity
        private bool _hasFired = false;
        private Coroutine _warningCoroutine;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _mainAudioSource = GetComponent<AudioSource>();

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

            if (objectiveTarget != null)
            {
                float dist = Vector3.Distance(transform.position, objectiveTarget.position);
                if (dist > requiredRange)
                {
                    reason = $"I need to save this until I reach the victim";
                    return false;
                }
            }

            if (sensorSuite != null)
            {
                float currentLevel = 0f;

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
            if (warningSound != null && warningAudioSource != null)
            {
                warningAudioSource.PlayOneShot(warningSound);
            }

            if (warningSubtitle == null) return;

            if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
            _warningCoroutine = StartCoroutine(DisplayWarningRoutine(message));
        }

        private IEnumerator DisplayWarningRoutine(string message)
        {
            warningSubtitle.text = message;
            warningSubtitle.gameObject.SetActive(true);

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
            if (fireSound) _mainAudioSource.PlayOneShot(fireSound);

            if (flareProjectilePrefab != null && firePoint != null)
            {
                GameObject flare = Instantiate(flareProjectilePrefab, firePoint.position, Quaternion.identity);
                var flareScript = flare.GetComponent<FlareProjectile>();
                if (flareScript != null) flareScript.Launch();
            }

            StartCoroutine(RescueSequenceRoutine());
        }

        private IEnumerator RescueSequenceRoutine()
        {
            yield return new WaitForSeconds(rescueDelay);

            if (rescueHelicopterSound) _mainAudioSource.PlayOneShot(rescueHelicopterSound);

            if (headsetFader != null)
            {
                headsetFader.FadeIn();
                yield return new WaitForSeconds(2.0f);
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}