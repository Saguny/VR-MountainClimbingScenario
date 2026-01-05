using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerSafetyManager : MonoBehaviour
{
    public static PlayerSafetyManager Instance { get; private set; }

    [Header("Detection Settings")]
    [Tooltip("Wähle hier GENAU den Layer deines Terrains aus!")]
    public LayerMask groundLayer;

    [Tooltip("Wie hoch über dem Boden soll der Spieler landen?")]
    public float safetyBuffer = 0.1f;

    [Header("References")]
    public XROrigin xrOrigin;
    public CharacterController characterController;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (xrOrigin == null) xrOrigin = GetComponent<XROrigin>() ?? FindFirstObjectByType<XROrigin>();
        if (characterController == null) characterController = GetComponent<CharacterController>() ?? FindFirstObjectByType<CharacterController>();
    }

    private void FixedUpdate()
    {
        // Wir prüfen permanent im Hintergrund
        RequestGroundSafetyCheck();
    }

    private void LateUpdate()
    {
        if (xrOrigin != null && characterController != null && xrOrigin.Camera != null)
        {
            UpdateCharacterControllerHeight();
        }
    }

    private void UpdateCharacterControllerHeight()
    {
        float headHeight = xrOrigin.Camera.transform.localPosition.y;
        float finalHeight = Mathf.Max(headHeight, 0.5f);
        characterController.height = finalHeight;
        characterController.center = new Vector3(0, finalHeight / 2f, 0);
    }

    // DIESE METHODE WAR VORHER PRIVAT ODER HIESS ANDERS - JETZT IST SIE DA
    public void RequestGroundSafetyCheck()
    {
        if (xrOrigin == null || characterController == null || xrOrigin.Camera == null) return;

        // 1. Startpunkt: Kopf
        Vector3 headPos = xrOrigin.Camera.transform.position;

        // 2. Raycast nach unten (10 Meter Reichweite)
        if (Physics.Raycast(headPos, Vector3.down, out RaycastHit hit, 10.0f, groundLayer))
        {
            float feetPos = xrOrigin.transform.position.y - 0.6f;
            float groundY = hit.point.y;

            // 3. Wenn die Füße TIEFER sind als der Boden (minus 5cm Toleranz)
            if (feetPos < (groundY - 0.05f))
            {
                // Debuggen, damit wir sehen, wann er eingreift
                Debug.LogWarning($"[SafetyManager] Spieler unter Terrain erkannt! Teleportiere zu: {hit.point}");
                TeleportPlayerToSurface(hit.point);
            }
        }
    }

    private void TeleportPlayerToSurface(Vector3 targetPoint)
    {
        characterController.enabled = false;

        Vector3 newPos = xrOrigin.transform.position;
        newPos.y = targetPoint.y + safetyBuffer;
        xrOrigin.transform.position = newPos;

        // Erzwingt sofortiges Update der Physik-Engine
        Physics.SyncTransforms();

        characterController.enabled = true;
        characterController.Move(Vector3.zero);
    }
}