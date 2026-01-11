using UnityEngine;

public class OpenXRHandSpaceFix : MonoBehaviour
{
    private bool fixedSpace = false;

    void Start()
    {
        InvokeRepeating(nameof(CheckAndFix), 0.5f, 1f);
        Invoke(nameof(StopChecking), 6f);
    }

    void StopChecking()
    {
        CancelInvoke(nameof(CheckAndFix));
    }

    void CheckAndFix()
    {
        if (fixedSpace) return;

        // mirrored OpenXR sessions always produce a flipped basis
        if (transform.lossyScale.x < 0f ||
            Vector3.Dot(transform.right, Vector3.right) < 0f)
        {
            Debug.LogWarning("Mirrored OpenXR controller space detected. Fixing tracking space.");

            // fix tracking space ONCE
            transform.localRotation *= Quaternion.Euler(0, 180f, 0);

            fixedSpace = true;
        }
    }
}
