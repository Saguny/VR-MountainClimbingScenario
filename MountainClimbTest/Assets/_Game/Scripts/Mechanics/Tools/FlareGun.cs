using MountainRescue.Systems;
using MountainRescue.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // Added for scene events
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
        [SerializeField] private AudioSource warningAudioSource;

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
        private AudioSource _mainAudioSource;
        private bool _hasFired = false;
        private Coroutine _warningCoroutine;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _mainAudioSource = GetComponent<AudioSource>();

            if (warningSubtitle != null) warningSubtitle.gameObject.SetActive(false);

            // 1. Listen for Scene Changes (since this object is likely DDOL)
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 2. Initial Search (in case we start in the scene with the target)
            FindRescueTarget();
        }

        private void OnDestroy()
        {
            // Always unsubscribe from static events to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Called automatically by Unity whenever a new scene finishes loading
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindRescueTarget();
        }

        private void FindRescueTarget()
        {
            // Search for the object by tag in the currently active scene
            GameObject targetObj = GameObject.FindGameObjectWithTag("RescueTarget");
            if (targetObj != null)
            {
                objectiveTarget = targetObj.transform;
            }
            else
            {
                // Optional: set to null if no target exists in this scene (e.g. main menu)
                objectiveTarget = null;
            }
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

            // 1. Range Check (Now uses the auto-found target)
            if (objectiveTarget != null)
            {
                float dist = Vector3.Distance(transform.position, objectiveTarget.position);
                if (dist > requiredRange)
                {
                    reason = $"I need to save this until I reach the victim";
                    return false;
                }
            }
            else
            {
                // Optional: Block firing if no target is found in the scene?
                // reason = "No valid rescue target found nearby.";
                // return false; 
            }

            // 2. Sensor Check
            if (sensorSuite != null)
            {
                // Note: You previously had 'currentLevel = 0f' here. 
                // Assuming you want to read the actual sensor data:
                float currentLevel = 0f; // TODO: Hook this up to sensorSuite.GetLevel() or similar if implemented

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

            // Make the text face the player
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
                var flareScript = flare.GetComponent<FlareProjectile>(); // Assuming you have this script
                if (flareScript != null) flareScript.Launch(); // Assuming Launch() method exists
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