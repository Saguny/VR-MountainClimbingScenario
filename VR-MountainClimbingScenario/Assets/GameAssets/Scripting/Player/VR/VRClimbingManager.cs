using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRClimbingManager : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;

    [Tooltip("Drag your 'Move Provider' (Continuous Move) here.")]
    public ContinuousMoveProvider moveProvider;

    [Header("Interactors")]
    public XRBaseInteractor leftInteractor;
    public XRBaseInteractor rightInteractor;

    [Header("Settings")]
    [Tooltip("Tag your rocks with this string!")]
    public string climbableTag = "Stone/Climbable";

    private bool _isClimbing = false;
    private XRBaseInteractor _activeInteractor;
    private Vector3 _previousHandPos;

    void Update()
    {
        ManageClimbing();
    }

    void ManageClimbing()
    {
        bool isLeftClimbing = CheckIfClimbing(leftInteractor);
        bool isRightClimbing = CheckIfClimbing(rightInteractor);

        if (isLeftClimbing || isRightClimbing)
        {
            if (!_isClimbing)
            {
                Debug.Log("starting climb!");
                StartClimbing(isLeftClimbing ? leftInteractor : rightInteractor);
            }
            else if (isLeftClimbing && _activeInteractor != leftInteractor)
            {
                _activeInteractor = leftInteractor;
                _previousHandPos = _activeInteractor.transform.position;
            }
            else if (isRightClimbing && _activeInteractor != rightInteractor)
            {
                _activeInteractor = rightInteractor;
                _previousHandPos = _activeInteractor.transform.position;
            }

            MovePlayerWithHand();
        }
        else
        {
            if (_isClimbing)
            {
                Debug.Log("stopping climb!");
                StopClimbing();
            }
        }
    }

    void StartClimbing(XRBaseInteractor interactor)
    {
        _isClimbing = true;
        _activeInteractor = interactor;
        _previousHandPos = interactor.transform.position;

        if (moveProvider != null)
        {
            moveProvider.useGravity = false;
        }
    }

    void StopClimbing()
    {
        _isClimbing = false;
        _activeInteractor = null;

        if (moveProvider != null)
        {
            moveProvider.useGravity = true;
        }
    }

    private void MovePlayerWithHand()
    {
        if (_activeInteractor == null) return;

        Vector3 currentHandPos = _activeInteractor.transform.position;
        Vector3 handDelta = currentHandPos - _previousHandPos;

        characterController.Move(-handDelta);

        _previousHandPos = _activeInteractor.transform.position;
    }

    private bool CheckIfClimbing(XRBaseInteractor interactor)
    {
        if (interactor == null) return false;

        if (interactor.hasSelection)
        {
            if (interactor.interactablesSelected.Count > 0)
            {
                var grabbedObject = interactor.interactablesSelected[0].transform;
                Debug.Log($"grabbing {grabbedObject.name} with tag {grabbedObject.tag}");

                if (grabbedObject.CompareTag(climbableTag))
                {
                    return true;
                }
            }
        }
        return false;
    }
}