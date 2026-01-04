using UnityEngine;
using UnityEngine.Audio;

namespace MountainRescue.Engine
{
    public class FootstepManager : MonoBehaviour
    {
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private LayerMask groundLayer;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] snowSteps;
        [SerializeField] private AudioClip[] rockSteps;
        [SerializeField] private AudioClip[] iceSteps;

        [Header("Settings")]
        [SerializeField] private float stepDistance = 1.5f;
        [SerializeField] private float rayDistance = 0.5f;
        [SerializeField] private float rayOffset = 0.1f;

        private Vector3 lastStepPosition;
        private CharacterController characterController;

        private void Start()
        {
            characterController = GetComponentInParent<CharacterController>();
            lastStepPosition = transform.position;
        }

        private void Update()
        {
            if (CheckFootstepCondition())
            {
                float distanceMoved = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                                       new Vector3(lastStepPosition.x, 0, lastStepPosition.z));

                if (distanceMoved > stepDistance)
                {
                    TryPlayStep();
                    lastStepPosition = transform.position;
                }
            }
        }

        private bool CheckFootstepCondition()
        {
            Vector3 rayStart = transform.position + Vector3.up * rayOffset;
            return Physics.Raycast(rayStart, Vector3.down, rayDistance, groundLayer);
        }

        private void TryPlayStep()
        {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up * rayOffset;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, groundLayer))
            {
                AudioClip[] selectedArray = snowSteps; // Default

                if (hit.collider.CompareTag("Stone"))
                {
                    selectedArray = rockSteps;
                }
                else if (hit.collider.CompareTag("Ice"))
                {
                    selectedArray = iceSteps;
                }

                if (selectedArray != null && selectedArray.Length > 0)
                {
                    AudioClip clip = selectedArray[Random.Range(0, selectedArray.Length)];
                    footstepSource.pitch = Random.Range(0.85f, 1.15f);
                    footstepSource.PlayOneShot(clip);
                }
            }
        }
    }
}