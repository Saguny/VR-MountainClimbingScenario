using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DesktopCharacterController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Main Camera here")]
    public Transform playerCamera;

    [Header("Input Actions")]
    public InputActionProperty moveAction;
    public InputActionProperty lookAction;

    [Header("Settings")]
    public float walkSpeed = 4.0f;
    public float gravity = -15.0f;

    [Header("Look Settings")]
    public float mouseSensitivity = 15.0f;
    public float lookXLimit = 85.0f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _xRotation = 0f;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible  = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        bool isGrounded = _controller.isGrounded;

        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>(); 
        
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        _controller.Move(move * walkSpeed * Time.deltaTime);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        if (playerCamera == null) return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity * Time.deltaTime);

        _xRotation -= lookInput.y * mouseSensitivity * Time.deltaTime;
        _xRotation = Mathf.Clamp(_xRotation, -lookXLimit, lookXLimit);

        playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
