using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Camera Settings")]
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float lookXLimit = 80f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.4f;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private LayerMask pickupLayer;
    [SerializeField] private Transform holdPoint; // Empty GameObject as child for holding objects

    private Rigidbody rb;
    private Camera playerCamera;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool interactInput;
    private bool isGrounded;
    private float xRotation = 0f;

    private GameObject heldObject; // Currently held object
    private Rigidbody heldObjectRb;
    private IObject heldObjectInterface; // Reference to IObject interface

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = GetComponentInChildren<Camera>();
        playerInput = GetComponent<PlayerInput>();

        // Ensure holdPoint is assigned
        if (holdPoint == null)
        {
            Debug.LogWarning("HoldPoint not assigned! Creating default hold point.");
            GameObject holdPointObj = new GameObject("HoldPoint");
            holdPointObj.transform.SetParent(playerCamera.transform);
            holdPointObj.transform.localPosition = Vector3.forward * holdDistance;
            holdPoint = holdPointObj.transform;
        }
    }

    private void Update()
    {
        CheckGroundStatus();
        HandleCameraLook();
        HandleInteract();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        UpdateHeldObject();
    }

    private void CheckGroundStatus()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void HandleMovement()
    {
        // Calculate movement direction relative to camera
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Combine forward/back (moveInput.y) and strafe (moveInput.x)
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // Apply movement
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        // Align player to face camera's forward direction (XZ plane)
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.fixedDeltaTime
            );
        }
    }

    private void HandleCameraLook()
    {
        // Horizontal rotation (yaw) for camera
        transform.Rotate(Vector3.up * lookInput.x * lookSpeed * Time.deltaTime);

        // Vertical rotation (pitch) for camera
        xRotation -= lookInput.y * lookSpeed * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false; // Reset jump input after jumping
        }
    }

    private void HandleInteract()
    {
        if (interactInput && heldObject == null)
        {
            // Try to pick up an object
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
            {
                IObject pickupObject = hit.collider.GetComponent<IObject>();
                if (pickupObject != null)
                {
                    heldObject = pickupObject.GameObject;
                    heldObjectRb = heldObject.GetComponent<Rigidbody>();
                    heldObjectInterface = pickupObject;

                    // Disable physics while held
                    heldObjectRb.isKinematic = true;
                    heldObjectRb.useGravity = false;

                    // Parent to hold point
                    heldObject.transform.SetParent(holdPoint);
                    heldObject.transform.localPosition = Vector3.zero;
                    heldObject.transform.localRotation = Quaternion.identity;

                    // Notify object of pickup
                    heldObjectInterface.OnPickup();
                    Debug.Log($"Picked up: {heldObject.name}", heldObject);
                }
            }
        }
        else if (interactInput && heldObject != null)
        {
            // Drop the held object
            heldObject.transform.SetParent(null);
            heldObjectRb.isKinematic = false;
            heldObjectRb.useGravity = true;

            // Apply slight forward velocity to avoid dropping on player
            heldObjectRb.AddForce(playerCamera.transform.forward * 2f, ForceMode.Impulse);

            // Notify object of drop
            heldObjectInterface.OnDrop();
            Debug.Log($"Dropped: {heldObject.name}", heldObject);

            heldObject = null;
            heldObjectRb = null;
            heldObjectInterface = null;
        }

        interactInput = false; // Reset input after processing
    }

    private void UpdateHeldObject()
    {
        if (heldObject != null)
        {
            // Ensure held object stays at hold point
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
        }
    }

    // Input callback methods
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpInput = context.performed;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        interactInput = context.performed;
    }
}