using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    public Transform player;
    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, player.position);
    }
}
