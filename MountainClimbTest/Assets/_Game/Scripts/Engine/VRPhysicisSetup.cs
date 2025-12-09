using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class VRPhysicisSetup : MonoBehaviour
{
    [Tooltip("If true, Physics will update exactly once per visual frame")]
    public bool matchDisplayRefreshRate = true;

    private void Start()
    {
        StartCoroutine(SyncPhysicsToRefreshRate());
    }

    private IEnumerator SyncPhysicsToRefreshRate()
    {
        yield return null;

#pragma warning disable CS0618 // Type or member is obsolete
        float refreshRate = XRDevice.refreshRate;
#pragma warning restore CS0618 // Type or member is obsolete

        if (refreshRate < 1.0f)
        {
            refreshRate = 90f;
        }

        if (matchDisplayRefreshRate)
        {
            Time.fixedDeltaTime = 1.0f / refreshRate;
        }

        Debug.Log($"[VR Setup] Headset Refresh Rate: {refreshRate}Hz | Physics Timestamp set to: {Time.fixedDeltaTime}");
    }

}
