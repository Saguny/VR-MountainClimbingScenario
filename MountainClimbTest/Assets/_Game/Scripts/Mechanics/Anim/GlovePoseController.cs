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
            // 1. Basis-Werte vom Anker holen
            Vector3 targetPos = currentTargetAttach.position;
            Quaternion targetRot = currentTargetAttach.rotation;

            if (isLeftHand)
            {
                // 2. Spiegel-Logik (Invertierung von Y und Z für die Symmetrie)
                targetRot = new Quaternion(targetRot.x, -targetRot.y, -targetRot.z, targetRot.w);

                // 3. Offset-Korrektur (Lokal zum Ziel-Anker)
                // Wir wenden den Positions-Offset im lokalen Raum des Ankers an,
                // damit "Links" auch wirklich "Links" bleibt, egal wie der Stein gedreht ist.
                targetPos += currentTargetAttach.TransformDirection(positionOffset);

                // Rotationsoffset hinzufügen (z.B. falls die Hand noch leicht schräg ist)
                targetRot *= Quaternion.Euler(rotationOffset);
            }

            // 4. Finale Zuweisung
            handVisualMesh.position = targetPos;
            handVisualMesh.rotation = targetRot;
        }
    }
}