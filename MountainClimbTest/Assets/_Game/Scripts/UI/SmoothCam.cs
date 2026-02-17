using UnityEngine;

public class SpectatorCameraFollow : MonoBehaviour
{
    public Transform vrTarget;
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 5f;

    void LateUpdate()
    {
        if (vrTarget == null) return;

        transform.position = Vector3.Lerp(transform.position, vrTarget.position, positionSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation, vrTarget.rotation, rotationSmoothing * Time.deltaTime);
    }
}