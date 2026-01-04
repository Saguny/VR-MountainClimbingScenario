using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AnchorRespawner : MonoBehaviour
{
    public GameObject anchorPrefab;
    public float respawnDelay = 1.0f;

    private XRSocketInteractor socket;
    private XRInteractionManager interactionManager;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
    }

    void Start()
    {
        // Ersten Anker beim Spielstart spawnen
        SpawnAnchor();
    }

    void OnEnable()
    {
        socket.selectExited.AddListener(OnAnchorRemoved);
    }

    void OnDisable()
    {
        socket.selectExited.RemoveListener(OnAnchorRemoved);
    }

    private void OnAnchorRemoved(SelectExitEventArgs args)
    {
        // Wir prüfen, ob das Objekt, das den Socket verlassen hat, ein Anker war
        // und starten dann den Timer für den nächsten
        StartCoroutine(SpawnWithDelay());
    }

    private System.Collections.IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnAnchor();
    }

    public void SpawnAnchor()
    {
        if (anchorPrefab == null) return;

        // 1. Objekt erzeugen
        GameObject newAnchor = Instantiate(anchorPrefab);
        XRGrabInteractable interactable = newAnchor.GetComponent<XRGrabInteractable>();

        if (interactable != null && interactionManager != null)
        {
            // 2. Den Interaction Manager anweisen, den Anker in den Socket zu legen
            interactionManager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)interactable);
        }
    }
}