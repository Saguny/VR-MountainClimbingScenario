using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MountainRescue.Dialogue
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class NPCDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private NPCDialogueController controller;
        [SerializeField] private float interactionRange = 2.0f; // Increased slightly for comfort

        private XRSimpleInteractable _interactable;
        private Transform _playerHead;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            if (Camera.main != null) _playerHead = Camera.main.transform;
        }

        private void OnEnable()
        {
            
            _interactable.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            _interactable.activated.RemoveListener(OnActivated);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (_playerHead == null) return;

            float dist = Vector3.Distance(transform.position, _playerHead.position);

            if (dist <= interactionRange)
            {
                controller.Interact();
            }
        }
    }
}