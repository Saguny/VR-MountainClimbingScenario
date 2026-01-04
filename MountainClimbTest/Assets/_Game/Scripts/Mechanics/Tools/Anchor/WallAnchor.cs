using UnityEngine;

public class WallAnchor : MonoBehaviour
{
    public Transform snapPoint;

    public Transform GetSnapPoint()
    {
        return snapPoint != null ? snapPoint : transform;
    }
}