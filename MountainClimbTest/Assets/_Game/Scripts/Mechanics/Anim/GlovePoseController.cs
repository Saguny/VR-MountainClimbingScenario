using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GlovePoseController : MonoBehaviour
{
    [SerializeField] private Animator gloveAnimator;
    [SerializeField] private Transform handVisualMesh; // Dein hand.r Modell

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
        // REIN VISUELL: Wir zwingen nur das Mesh an den Punkt.
        // Das hat 0,0 Auswirkung auf die Kletter-Physik oder den ClimbProvider.
        if (isLockedToStone && currentTargetAttach != null)
        {
            handVisualMesh.position = currentTargetAttach.position;
            handVisualMesh.rotation = currentTargetAttach.rotation;
        }
    }
}