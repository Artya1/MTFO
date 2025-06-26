using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ObjectPickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 5f; // For easier pickup
    [SerializeField] private float pickupRadius = 0.2f; // Radius for sphere cast
    [SerializeField] private float maxHoldDistance = 2f;
    [SerializeField] private float minHoldDistance = 0.2f; // Min distance from surfaces
    [SerializeField] private float floorCheckDistance = 1f;
    [SerializeField] private float objectRadius = 0.3f; // Approx. object size
    [SerializeField] private LayerMask pickupLayer;
    [SerializeField] private LayerMask holdLayer; // Layers for hold point and floor
    [SerializeField] private Camera playerCamera; // Reference to player's camera

    private GameObject heldObject; // Currently held object
    private Rigidbody heldObjectRb;
    private IObject heldObjectInterface; // Reference to IObject interface
    private bool interactInput;

    private void Awake()
    {
        // Ensure playerCamera is assigned
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("Player Camera not found! Assign a Camera in the Inspector.", gameObject);
            }
        }
    }

    private void Update()
    {
        HandlePickup();
    }

    private void FixedUpdate()
    {
        UpdateHeldObject();
    }

    private bool GetHoldPointPosition(out Vector3 position)
    {
        // Raycast from camera to determine hold point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool isValid = true;
        if (Physics.Raycast(ray, out RaycastHit hit, maxHoldDistance, holdLayer))
        {
            // Check if hit point is too close to surface
            float distanceToSurface = Vector3.Distance(playerCamera.transform.position, hit.point);
            if (distanceToSurface < minHoldDistance)
            {
                isValid = false;
            }
            position = hit.point;
        }
        else
        {
            // No hit, place at max distance
            position = playerCamera.transform.position + playerCamera.transform.forward * maxHoldDistance;
        }

        return isValid;
    }

    private void HandlePickup()
    {
        if (interactInput && heldObject == null)
        {
            // Visualize pickup raycast
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupRange, Color.red, 1f);

            // Try to pick up an object with sphere cast
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer))
            {
                IObject pickupObject = hit.collider.GetComponent<IObject>();
                if (pickupObject != null)
                {
                    // Check if hold point is valid
                    if (GetHoldPointPosition(out Vector3 holdPosition))
                    {
                        heldObject = pickupObject.GameObject;
                        heldObjectRb = heldObject.GetComponent<Rigidbody>();
                        heldObjectInterface = pickupObject;

                        // Disable physics while held
                        heldObjectRb.isKinematic = true;
                        heldObjectRb.useGravity = false;

                        // Position at hold point with player-relative rotation
                        heldObject.transform.position = holdPosition;
                        heldObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

                        // Notify object of pickup
                        heldObjectInterface.OnPickup();
                        Debug.Log($"Picked up: {heldObject.name}", heldObject);
                    }
                }
            }
        }
        else if (interactInput && heldObject != null)
        {
            DropObject();
        }

        interactInput = false; // Reset input after processing
    }

    private void UpdateHeldObject()
    {
        if (heldObject != null)
        {
            // Get hold point position
            if (!GetHoldPointPosition(out Vector3 holdPosition))
            {
                // Hold point too close to surface, drop object
                DropObject();
                return;
            }

            // Check for floor penetration
            Debug.DrawRay(heldObject.transform.position, Vector3.down * floorCheckDistance, Color.green, 1f);
            if (Physics.Raycast(heldObject.transform.position, Vector3.down, out RaycastHit floorHit, floorCheckDistance, holdLayer))
            {
                // Adjust position only if object would penetrate floor
                float minHeight = floorHit.point.y + objectRadius;
                if (holdPosition.y < minHeight)
                {
                    holdPosition.y = minHeight;
                    Debug.Log($"Adjusted {heldObject.name} above floor at {minHeight}", heldObject);
                }
            }

            // Check for wall overlap
            if (Physics.OverlapSphere(holdPosition, objectRadius, holdLayer).Length > 0)
            {
                // Object would intersect a wall, drop it
                DropObject();
                return;
            }

            // Move held object to hold point with player-relative rotation
            heldObject.transform.position = holdPosition;
            heldObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }
    }

    private void DropObject()
    {
        // Drop the held object
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

    public void OnInteract(InputAction.CallbackContext context)
    {
        interactInput = context.performed;
    }
}