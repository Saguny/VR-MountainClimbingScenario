using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GlovePoseController : MonoBehaviour
{
    [SerializeField] private Animator gloveAnimator;
    [SerializeField] private Transform handVisualMesh; // Dein hand.r Modell
    [SerializeField] private bool isLeftHand = false;

    [Header("Visual Fine-Tuning")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private IXRSelectInteractor interactor;
    private bool isLockedToStone = false;
    private Transform currentTargetAttach;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    void Awake()
    {
        interactor = GetComponentInChildren<IXRSelectInteractor>();

        // Wir speichern die Standard-Position am Controller
        initialLocalPos = handVisualMesh.localPosition;
        initialLocalRot = handVisualMesh.localRotation;

        if (interactor != null)
        {
            interactor.selectEntered.AddListener(OnItemSelected);
            interactor.selectExited.AddListener(OnItemReleased);
        }
    }

    private void OnItemSelected(SelectEnterEventArgs args)
    {
        // 1. Werkzeug Check (Flaregun etc.)
        if (args.interactableObject.transform.TryGetComponent(out HandPoseInfo poseInfo))
        {
            gloveAnimator.SetInteger("PoseID", poseInfo.PoseID);
            isLockedToStone = false;
        }
        // 2. Stein Check (SmartClimbAttach)
        else if (args.interactableObject.transform.TryGetComponent(out SmartClimbAttach climbInfo))
        {
            gloveAnimator.SetInteger("PoseID", 3); // Deine Kletter-Pose
            currentTargetAttach = climbInfo.GetVisualPoint();
            isLockedToStone = (currentTargetAttach != null);
        }
    }

    private void OnItemReleased(SelectExitEventArgs args)
    {
        // Alles zurücksetzen
        isLockedToStone = false;
        currentTargetAttach = null;
        gloveAnimator.SetInteger("PoseID", 0);

        // Die Hand springt visuell sofort wieder an den Controller
        handVisualMesh.localPosition = initialLocalPos;
        handVisualMesh.localRotation = initialLocalRot;
    }

    void LateUpdate()
    {
        if (isLockedToStone && currentTargetAttach != null)
        {
            Vector3 targetPos = currentTargetAttach.position;
            Quaternion targetRot = currentTargetAttach.rotation;

            if (isLeftHand)
            {
                // 1. Position Offset: Use InverseTransformPoint logic or simply mirror the local offset
                // We use the right-hand offset but flip the X coordinate for the left hand symmetry
                Vector3 mirroredOffset = new Vector3(-positionOffset.x, positionOffset.y, positionOffset.z);
                targetPos = currentTargetAttach.TransformPoint(mirroredOffset);

                // 2. Rotation Mirroring:
                // To mirror a rotation: reflect the Forward and Up vectors
                Vector3 forward = currentTargetAttach.forward;
                Vector3 up = currentTargetAttach.up;

                // Reflect the vectors across the local plane of the hand
                // This prevents the 180-degree flip when the parent is rotated
                Vector3 mirroredForward = Vector3.Reflect(forward, currentTargetAttach.right);
                targetRot = Quaternion.LookRotation(mirroredForward, up);

                // 3. Apply the manual fine-tuning rotation offset
                targetRot *= Quaternion.Euler(rotationOffset);
            }

            handVisualMesh.position = targetPos;
            handVisualMesh.rotation = targetRot;
        }
    }
}