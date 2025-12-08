using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public class SocketItemPreview : MonoBehaviour
{
    [Header("Visuals")]
    public Material previewMaterial;

    private XRSocketInteractor socket;
    private GameObject ghostRoot;

    void Awake() => socket = GetComponent<XRSocketInteractor>();

    void OnEnable()
    {
        socket.hoverEntered.AddListener(OnHoverEnter);
        socket.hoverExited.AddListener(OnHoverExit);
        socket.selectEntered.AddListener(OnSnap);
    }

    void OnDisable()
    {
        socket.hoverEntered.RemoveListener(OnHoverEnter);
        socket.hoverExited.RemoveListener(OnHoverExit);
        socket.selectEntered.RemoveListener(OnSnap);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        // If the socket is already full, don't show a ghost
        if (socket.hasSelection) return;

        CreateGhostTree(args.interactableObject.transform);
    }

    private void CreateGhostTree(Transform originalTool)
    {
        // 1. Create the root for the ghost
        ghostRoot = new GameObject($"{originalTool.name}_GhostRoot");

        // 2. Snap it to the socket's attach point (where the real tool will go)
        Transform snapPoint = socket.attachTransform != null ? socket.attachTransform : transform;
        ghostRoot.transform.SetParent(snapPoint);
        ghostRoot.transform.localPosition = Vector3.zero;
        ghostRoot.transform.localRotation = Quaternion.identity;
        ghostRoot.transform.localScale = originalTool.lossyScale; // Match the size

        // 3. Find EVERY mesh in the tool (Handle, Tip, Accessories, etc.)
        MeshFilter[] allMeshes = originalTool.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter originalMesh in allMeshes)
        {
            // Create a child object for this specific part
            GameObject ghostPart = new GameObject(originalMesh.name);
            ghostPart.transform.SetParent(ghostRoot.transform);

            // Match position/rotation relative to the tool's root
            // This ensures the Tip stays at the tip, and Handle stays at the handle
            ghostPart.transform.position = originalMesh.transform.position;
            ghostPart.transform.rotation = originalMesh.transform.rotation;

            // Note: Scale is handled by the root, so we keep local scale 1 unless the part is distorted
            ghostPart.transform.localScale = originalMesh.transform.lossyScale;
            // Correction: Since we parented to a scaled root, we need to be careful. 
            // Safest way for complex hierarchies is usually to copy localRelative, 
            // but this World-Position match method is robust for flat hierarchies.

            // Add the Mesh
            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            mf.sharedMesh = originalMesh.sharedMesh;

            // Add the Ghost Material
            MeshRenderer mr = ghostPart.AddComponent<MeshRenderer>();
            mr.material = previewMaterial;
        }
    }

    private void OnHoverExit(HoverExitEventArgs args) => DestroyGhost();
    private void OnSnap(SelectEnterEventArgs args) => DestroyGhost();

    private void DestroyGhost()
    {
        if (ghostRoot != null) Destroy(ghostRoot);
    }
}