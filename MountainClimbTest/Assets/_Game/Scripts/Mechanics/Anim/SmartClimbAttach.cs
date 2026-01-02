using UnityEngine;

public class SmartClimbAttach : MonoBehaviour
{
    [SerializeField] private Transform staticAttachPoint;

    public Transform GetVisualPoint()
    {
        return staticAttachPoint;
    }
}