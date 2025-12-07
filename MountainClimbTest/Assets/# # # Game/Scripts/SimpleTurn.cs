using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleTurn : MonoBehaviour
{
    public InputActionProperty turnInput;
    public InputActionProperty moveInput;
    public float speed = 60f;

    void Start()
    {
        Debug.Log("--- LISTING ALL DEVICES ---");
        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"Device: {device.name} | Role: {device.description.interfaceName}");
        }
        Debug.Log("---------------------------");
    }

    // --- ADD THIS SECTION ---
    void OnEnable()
    {
        turnInput.action.Enable(); // <--- Wake up the input!
    }

    void OnDisable()
    {
        turnInput.action.Disable();
    }
    // ------------------------

    void Update()
    {
        Vector2 input = moveInput.action.ReadValue<Vector2>();

        if (input != Vector2.zero)
        {
            // This is the magic line. It prints exactly what device is working.
            if (moveInput.action.activeControl != null)
            {
                Debug.Log($"I am being moved by: {moveInput.action.activeControl.device.name}");
            }
        }

    }
}