using UnityEngine;

public class FollowPlayerY : MonoBehaviour
{
    private Transform playerTransform;

    [Header("Settings")]
    public float followSpeed = 10f; // Optional smoothing
    public bool useSmoothing = false;

    [Tooltip("Add to the player's Y height. Use negative values to track below the player (e.g. -2.0).")]
    public float yOffset = 0f;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Apply the offset here
        float targetY = playerTransform.position.y + yOffset;

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