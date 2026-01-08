using UnityEngine;

public class FollowPlayerY : MonoBehaviour
{
    private Transform playerTransform;
    public float followSpeed = 10f; // Optional smoothing
    public bool useSmoothing = false;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float targetY = playerTransform.position.y;

        if (useSmoothing)
        {
            float smoothedY = Mathf.Lerp(transform.position.y, targetY, followSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
    }
}