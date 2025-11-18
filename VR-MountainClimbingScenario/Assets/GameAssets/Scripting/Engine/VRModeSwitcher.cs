using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class VRModeSwitcher : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The GameObject containing your XR Origin (VR Player).")]
    public GameObject vrRig;

    [Tooltip("The GameObject containing your standard Desktop Camera/Player.")]
    public GameObject desktopPlayer;

    void Start()
    {
        // we wait one frame to let the xr system finish its initialization attempts
        StartCoroutine(CheckAndSwitchMode());
    }

    IEnumerator CheckAndSwitchMode()
    {
        yield return null; // wait for end of frame

        bool isVRActive = false;

        // check if xr settings are set up and if a loader (like openxr) is currently active
        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                isVRActive = true;
            }
        }

        if (isVRActive)
        {
            Debug.Log("VR Headset Detected");
            if (vrRig != null) vrRig.SetActive(true);
            if (desktopPlayer != null) desktopPlayer.SetActive(false);
        }
        else
        {
            Debug.Log("No VR Headset found, or OXR Failed");
            if (vrRig != null) vrRig.SetActive(false);
            if (desktopPlayer != null) desktopPlayer.SetActive(true);
        }
    }
}