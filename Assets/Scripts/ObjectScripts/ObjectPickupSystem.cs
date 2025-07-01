using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ObjectPickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 5f; // Range for picking up/pulling
    public float pickupRadius = 0.2f; // Radius for sphere cast
    public float maxHoldDistance = 2f; // Max distance for held objects
    public float minHoldDistance = 0.2f; // Min distance from surfaces
    public float lightThrowForce = 5f; // Throw/push force for Light objects
    public float mediumThrowForce = 2f; // Throw/push force for Medium objects
    public float heavyPushForce = 10f; // Push force for Heavy objects
    public float doorPushForce = 5f; // Push force for Door objects
    public float doorPullSpeed = 90f; // Rotation speed for Door (degrees/sec)
    public float doorMaxAngle = 90f; // Max rotation angle for Door
    public float floorCheckDistance = 1f; // Distance to check for floor
    public float objectRadius = 0.3f; // Approx. object size
    public LayerMask pickupLayer; // Layer for pickable objects
    public LayerMask holdLayer; // Layers for hold point and floor
    public Camera playerCamera; // Reference to player's camera

    private GameObject heldObject; // Currently held object (Light/Medium only)
    private Rigidbody heldObjectRb; // Rigidbody of held object
    private IObject heldObjectInterface; // IObject interface of held object
    private GameObject interactingObject; // Object being pushed/pulled (Heavy/Door)
    private Rigidbody interactingObjectRb; // Rigidbody of pushed/pulled object
    private HingeJoint doorHinge; // HingeJoint for Door objects
    private Quaternion doorInitialRotation; // Initial rotation for Door
    private float doorCurrentAngle; // Current rotation angle for Door
    private bool interactInput;
    private bool throwInput;

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
        HandleInteraction();
    }

    private void FixedUpdate()
    {
        UpdateHeldObject();
        UpdateInteractingObject();
    }

    private bool GetHoldPointPosition(out Vector3 position)
    {
        // Raycast from camera to determine hold point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool isValid = true;
        if (Physics.Raycast(ray, out RaycastHit hit, maxHoldDistance, holdLayer))
        {
            float distanceToSurface = Vector3.Distance(playerCamera.transform.position, hit.point);
            if (distanceToSurface < minHoldDistance)
            {
                isValid = false;
            }
            position = hit.point;
        }
        else
        {
            position = playerCamera.transform.position + playerCamera.transform.forward * maxHoldDistance;
        }

        return isValid;
    }

    private void HandleInteraction()
    {
        if (interactInput && heldObject == null && interactingObject == null)
        {
            // Visualize pickup raycast
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupRange, Color.red, 1f);

            // Try to interact with an object
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer))
            {
                IObject pickupObject = hit.collider.GetComponent<IObject>();
                if (pickupObject != null)
                {
                    GameObject targetObject = pickupObject.GameObject;
                    string tag = targetObject.tag;

                    if (tag == "Light" || tag == "Medium")
                    {
                        // Pick up Light or Medium objects
                        if (GetHoldPointPosition(out Vector3 holdPosition))
                        {
                            heldObject = targetObject;
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
                    else if (tag == "Heavy")
                    {
                        // Start pushing Heavy object
                        interactingObject = targetObject;
                        interactingObjectRb = interactingObject.GetComponent<Rigidbody>();
                        heldObjectInterface = pickupObject;
                        heldObjectInterface.OnPickup();
                        Debug.Log($"Started pushing: {interactingObject.name}", interactingObject);
                    }
                    else if (tag == "Door")
                    {
                        // Start pulling Door
                        interactingObject = targetObject;
                        interactingObjectRb = interactingObject.GetComponent<Rigidbody>();
                        heldObjectInterface = pickupObject;
                        doorHinge = interactingObject.GetComponent<HingeJoint>();
                        doorInitialRotation = interactingObject.transform.rotation;
                        doorCurrentAngle = 0f;
                        heldObjectInterface.OnPickup();
                        Debug.Log($"Started pulling: {interactingObject.name}", interactingObject);
                    }
                }
            }
        }
        else if (throwInput && heldObject == null)
        {
            // Push any object in view when throw is triggered and no object is held
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer))
            {
                GameObject targetObject = hit.collider.gameObject;
                Rigidbody rb = targetObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    string tag = targetObject.tag;
                    float pushForce = tag switch
                    {
                        "Light" => lightThrowForce,
                        "Medium" => mediumThrowForce,
                        "Heavy" => heavyPushForce,
                        "Door" => doorPushForce,
                        _ => 0f
                    };
                    Vector3 pushDirection = playerCamera.transform.forward;
                    if (tag != "Door") pushDirection.y = 0f; // Keep push horizontal for non-Doors
                    rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Impulse);
                    Debug.Log($"Pushed {targetObject.name} with force {pushForce} (Tag: {tag})", targetObject);
                }
            }
        }
        else if (heldObject != null)
        {
            if (throwInput)
            {
                // Throw Light or Medium object
                ThrowObject();
            }
            else if (interactInput)
            {
                // Drop Light or Medium object
                DropObject();
            }
        }
        else if (interactingObject != null)
        {
            if (throwInput && interactingObject.CompareTag("Heavy"))
            {
                // Push Heavy object
                PushObject();
            }
            else if (interactInput)
            {
                // Stop interacting with Heavy or Door
                StopInteracting();
            }
        }

        interactInput = false; // Reset interact input
        throwInput = false; // Reset throw input
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

    private void UpdateInteractingObject()
    {
        if (interactingObject != null && interactingObject.CompareTag("Door"))
        {
            if (doorHinge != null)
            {
                // Use HingeJoint to control door rotation
                float inputDirection = interactInput ? 1f : 0f; // Pull when interact is held
                float targetVelocity = inputDirection * doorPullSpeed;
                JointMotor motor = doorHinge.motor;
                motor.targetVelocity = targetVelocity;
                doorHinge.motor = motor;
            }
            else
            {
                // Fallback to manual rotation if no HingeJoint
                float rotationAmount = doorPullSpeed * Time.fixedDeltaTime;
                doorCurrentAngle += rotationAmount;
                if (doorCurrentAngle <= doorMaxAngle)
                {
                    interactingObject.transform.rotation = doorInitialRotation * Quaternion.Euler(0f, rotationAmount, 0f);
                }
                else
                {
                    // Stop pulling if max angle reached
                    StopInteracting();
                }
            }
        }
    }

    private void DropObject()
    {
        // Drop the held object (Light/Medium)
        heldObjectRb.isKinematic = false;
        heldObjectRb.useGravity = true;

        // Apply slight forward velocity
        float dropForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        heldObjectRb.AddForce(playerCamera.transform.forward * dropForce * 0.2f, ForceMode.Impulse);

        // Notify object of drop
        heldObjectInterface.OnDrop();
        Debug.Log($"Dropped: {heldObject.name}", heldObject);

        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
    }

    private void ThrowObject()
    {
        // Throw the held object (Light/Medium)
        heldObjectRb.isKinematic = false;
        heldObjectRb.useGravity = true;

        // Apply throw force based on tag
        float throwForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        heldObjectRb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);

        // Notify object of drop
        heldObjectInterface.OnDrop();
        Debug.Log($"Threw: {heldObject.name}", heldObject);

        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
    }

    private void PushObject()
    {
        // Push Heavy object
        Vector3 pushDirection = playerCamera.transform.forward;
        pushDirection.y = 0f; // Keep push horizontal
        interactingObjectRb.AddForce(pushDirection.normalized * heavyPushForce, ForceMode.Impulse);
        Debug.Log($"Pushed: {interactingObject.name}", interactingObject);

        // Stop interacting after push
        StopInteracting();
    }

    private void StopInteracting()
    {
        // Stop interacting with Heavy or Door
        if (interactingObject != null)
        {
            heldObjectInterface.OnDrop();
            Debug.Log($"Stopped interacting with: {interactingObject.name}", interactingObject);
        }

        interactingObject = null;
        interactingObjectRb = null;
        doorHinge = null;
        heldObjectInterface = null;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        interactInput = context.performed;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        throwInput = context.performed;
    }
}