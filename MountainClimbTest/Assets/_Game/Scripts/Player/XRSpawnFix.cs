using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;

public class XRSpawnFix : MonoBehaviour
{
    public Transform xrOrigin;
    public Transform spawnPoint;

    void Start()
    {
        StartCoroutine(InitializeXR());
    }

    IEnumerator InitializeXR()
    {
        while (XRGeneralSettings.Instance.Manager.activeLoader == null)
            yield return null;

        yield return new WaitForSeconds(1.5f);

        Recenter();
        MoveToSpawn();
    }

    void Recenter()
    {
        var subsystems = new System.Collections.Generic.List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        foreach (var subsystem in subsystems)
        {
            subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            subsystem.TryRecenter();
        }
    }

    void MoveToSpawn()
    {
        Transform cam = Camera.main.transform;

        Vector3 horizontalOffset = xrOrigin.position - cam.position;
        horizontalOffset.y = 0;

        Vector3 targetPos = spawnPoint.position + horizontalOffset;
        targetPos.y = xrOrigin.position.y;

        xrOrigin.position = targetPos;

        Vector3 euler = spawnPoint.rotation.eulerAngles;
        xrOrigin.rotation = Quaternion.Euler(0, euler.y, 0);
    }

}
