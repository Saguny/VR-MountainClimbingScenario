using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleTurn : MonoBehaviour
{
    [Header("Input Settings")]
    public InputActionProperty turnInput; // Assign "Right Hand Locomotion/Turn" here
    public InputActionProperty moveInput; // debug

    [Header("Movement Settings")]
    public float turnSpeed = 60f;

    void Start()
    {
        // Debug info to confirm the script has started
        Debug.Log("Waiting for input...");
    }

    void OnEnable()
    {
        // Must enable the input actions to receive data
        if (turnInput.action != null) turnInput.action.Enable();
        if (moveInput.action != null) moveInput.action.Enable();
    }

    void OnDisable()
    {
        if (turnInput.action != null) turnInput.action.Disable();
        if (moveInput.action != null) moveInput.action.Disable();
    }

    void Update()
    {
        
        float turnAmount = turnInput.action.ReadValue<Vector2>().x;

        
        if (Mathf.Abs(turnAmount) > 0.05f)
        {
            
            transform.Rotate(Vector3.up * turnAmount * turnSpeed * Time.deltaTime);
        }

        
        if (moveInput.action != null)
        {
            Vector2 moveValue = moveInput.action.ReadValue<Vector2>();
            if (moveValue != Vector2.zero && moveInput.action.activeControl != null)
            {
                
                // Debug.Log($"Moving via: {moveInput.action.activeControl.device.name}");
            }
        }
    }
}