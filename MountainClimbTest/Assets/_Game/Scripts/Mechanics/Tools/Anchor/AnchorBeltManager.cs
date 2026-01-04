using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Wichtig für XRI 3.3.0
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AnchorBeltManager : MonoBehaviour
{
    [Header("Setup")]
    public List<XRSocketInteractor> beltSockets;
    public GameObject carabinerPrefab;

    [Header("Debug")]
    public bool respawnOnStart = true;

    private XRInteractionManager _interactionManager;

    void Start()
    {
        // Wir suchen den Manager global, falls er nicht zugewiesen ist
        if (_interactionManager == null)
            _interactionManager = FindFirstObjectByType<XRInteractionManager>();

        if (respawnOnStart)
        {
            StartCoroutine(RestockRoutine());
        }
    }

    // Hört auf Szenenwechsel für DDOL Support
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RestockRoutine());
    }

    private IEnumerator RestockRoutine()
    {
        // Warte einen Frame, damit XRI sich initialisieren kann
        yield return null;
        yield return new WaitForEndOfFrame();

        foreach (var socket in beltSockets)
        {
            // Sicherheitscheck: Ist der Socket leer?
            if (!socket.hasSelection)
            {
                // Prüfen ob wir manuell schon was reingelegt haben (Starting Selected Interactable)
                // XRI 3.3.0 updated hasSelection manchmal erst später, daher der Delay oben.
                SpawnAndAttach(socket);
            }
        }
    }

    private void SpawnAndAttach(XRSocketInteractor socket)
    {
        if (carabinerPrefab == null || _interactionManager == null)
        {
            Debug.LogWarning("AnchorBeltManager: Prefab oder InteractionManager fehlt!");
            return;
        }

        // 1. Spawnen an der Position des Sockets
        GameObject newItem = Instantiate(carabinerPrefab, socket.transform.position, socket.transform.rotation);

        // 2. Interactable Komponente holen
        var interactable = newItem.GetComponent<XRGrabInteractable>();
        if (interactable == null) return;

        // 3. WICHTIG: Manuelles Erzwingen der Auswahl durch den Interaction Manager
        // Das ist sauberer als Parenting, da XRI dann die Logik übernimmt
        _interactionManager.SelectEnter(socket as IXRSelectInteractor, interactable as IXRSelectInteractable);
    }
}