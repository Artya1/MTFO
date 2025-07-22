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
    public float lightThrowForce = 5f; // Base throw/push force for Light objects
    public float mediumThrowForce = 2f; // Base throw/push force for Medium objects
    public float heavyPushForce = 10f; // Push force for Heavy objects
    public float doorPushForce = 5f; // Push force for Door objects
    public float doorPullSpeed = 90f; // Rotation speed for Door (degrees/sec)
    public float doorMaxAngle = 90f; // Max rotation angle for Door
    public float floorCheckDistance = 1f; // Distance to check for floor
    public float objectRadius = 0.3f; // Approx. object size
    public float rotationSensitivity = 1f; // Scroll wheel rotation sensitivity, editable in Inspector
    [Header("Throw Charge Settings")]
    public float maxThrowForceMultiplier = 2f; // Max multiplier for throw force
    public float maxChargeTime = 1f; // Time (seconds) to reach max charge
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
    private float throwChargeTime; // Current charge duration for throw
    private bool isChargingThrow; // Whether throw input is being held
    private enum RotationAxis { X, Y } // Enum for rotation axis
    private RotationAxis currentRotationAxis = RotationAxis.Y; // Current rotation axis
    private Vector3 rotationOffset = Vector3.zero; // Rotation offset for X, Y, Z axes
    private readonly Collider[] overlapResults = new Collider[8]; // Cached array for overlap checks

    private void Awake()
    {
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
        if (heldObject != null)
        {
            RotateObject();
            if (isChargingThrow)
            {
                throwChargeTime += Time.deltaTime; // Accumulate charge time
            }
        }
    }

    private void FixedUpdate()
    {
        UpdateHeldObject();
        UpdateInteractingObject();
    }

    private bool GetHoldPointPosition(out Vector3 position)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool isValid = true;
        if (Physics.Raycast(ray, out RaycastHit hit, maxHoldDistance, holdLayer, QueryTriggerInteraction.Ignore))
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
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.TryGetComponent<IObject>(out var pickupObject))
                {
                    GameObject targetObject = pickupObject.GameObject;
                    string tag = targetObject.tag;

                    if (tag == "Light" || tag == "Medium")
                    {
                        if (GetHoldPointPosition(out Vector3 holdPosition))
                        {
                            heldObject = targetObject;
                            heldObjectRb = heldObject.GetComponent<Rigidbody>();
                            heldObjectInterface = pickupObject;

                            heldObjectRb.isKinematic = true;
                            heldObjectRb.useGravity = false;
                            heldObjectRb.angularVelocity = Vector3.zero;

                            heldObject.transform.position = holdPosition;
                            heldObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                            rotationOffset = Vector3.zero;

                            heldObjectInterface.OnPickup();
                        }
                    }
                    else if (tag == "Door")
                    {
                        interactingObject = targetObject;
                        interactingObjectRb = interactingObject.GetComponent<Rigidbody>();
                        heldObjectInterface = pickupObject;
                        doorHinge = interactingObject.GetComponent<HingeJoint>();
                        doorInitialRotation = interactingObject.transform.rotation;
                        doorCurrentAngle = 0f;
                        heldObjectInterface.OnPickup();
                    }
                    // Heavy objects are not picked up here; they are handled by throw/push action
                }
            }
        }
        else if (throwInput && heldObject == null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Ignore))
            {
                GameObject targetObject = hit.collider.gameObject;
                if (targetObject.TryGetComponent<Rigidbody>(out var rb) && targetObject.TryGetComponent<IObject>(out var pickupObject))
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
                    if (tag != "Door") pushDirection.y = 0f;
                    // Temporarily reduce mass/drag for Heavy objects during push
                    if (tag == "Heavy")
                    {
                        interactingObject = targetObject;
                        interactingObjectRb = rb;
                        heldObjectInterface = pickupObject;
                        heldObjectInterface.OnPickup();
                        PickupObject pickup = targetObject.GetComponent<PickupObject>();
                        if (pickup != null)
                        {
                            pickup.ApplyPushPhysics();
                            rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Impulse);
                            // Schedule physics reset after push
                            Invoke(nameof(ResetInteractingObjectPhysics), 0.5f);
                        }
                    }
                    else
                    {
                        rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Impulse);
                    }
                }
            }
        }
        else if (heldObject != null)
        {
            if (throwInput)
            {
                ThrowObject();
            }
            else if (interactInput)
            {
                DropObject();
            }
        }
        else if (interactingObject != null && interactingObject.CompareTag("Door") && interactInput)
        {
            StopInteracting();
        }

        interactInput = false;
        throwInput = false;
    }

    private void ResetInteractingObjectPhysics()
    {
        if (interactingObject != null && interactingObject.CompareTag("Heavy"))
        {
            PickupObject pickup = interactingObject.GetComponent<PickupObject>();
            if (pickup != null)
            {
                pickup.ResetPhysics();
            }
        }
        StopInteracting();
    }

    private void UpdateHeldObject()
    {
        if (heldObject == null) return;

        heldObjectRb.useGravity = false;

        if (!GetHoldPointPosition(out Vector3 holdPosition))
        {
            DropObject();
            return;
        }

        if (Physics.Raycast(heldObject.transform.position, Vector3.down, out RaycastHit floorHit, floorCheckDistance, holdLayer, QueryTriggerInteraction.Ignore))
        {
            float minHeight = floorHit.point.y + objectRadius;
            if (holdPosition.y < minHeight)
            {
                holdPosition.y = minHeight;
            }
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(holdPosition, objectRadius, overlapResults, holdLayer, QueryTriggerInteraction.Ignore);
        if (overlapCount > 0)
        {
            DropObject();
            return;
        }

        Vector3 directionToPlayer = (playerCamera.transform.position - holdPosition).normalized;
        directionToPlayer.y = 0f;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            Quaternion targetRotation = baseRotation * Quaternion.Euler(rotationOffset);
            heldObject.transform.rotation = targetRotation;
        }

        heldObject.transform.position = holdPosition;
    }

    private void UpdateInteractingObject()
    {
        if (interactingObject == null || !interactingObject.CompareTag("Door")) return;

        if (doorHinge != null)
        {
            float inputDirection = interactInput ? 1f : 0f;
            float targetVelocity = inputDirection * doorPullSpeed;
            JointMotor motor = doorHinge.motor;
            motor.targetVelocity = targetVelocity;
            doorHinge.motor = motor;
        }
        else
        {
            float rotationAmount = doorPullSpeed * Time.fixedDeltaTime;
            doorCurrentAngle += rotationAmount;
            if (doorCurrentAngle <= doorMaxAngle)
            {
                interactingObject.transform.rotation = doorInitialRotation * Quaternion.Euler(0f, rotationAmount, 0f);
            }
            else
            {
                StopInteracting();
            }
        }
    }

    private void DropObject()
    {
        heldObjectRb.isKinematic = false;
        heldObjectRb.useGravity = true;
        heldObjectRb.linearVelocity = Vector3.zero;
        heldObjectRb.angularVelocity = Vector3.zero;

        float dropForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        heldObjectRb.AddForce(playerCamera.transform.forward * dropForce * 0.2f, ForceMode.Impulse);

        heldObjectInterface.OnDrop();
        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
        rotationOffset = Vector3.zero;
    }

    private void ThrowObject()
    {
        heldObjectRb.isKinematic = false;
        heldObjectRb.useGravity = true;
        heldObjectRb.linearVelocity = Vector3.zero;
        heldObjectRb.angularVelocity = Vector3.zero;

        float baseThrowForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        float chargeFactor = Mathf.Clamp01(throwChargeTime / maxChargeTime);
        float throwForce = baseThrowForce * (1f + chargeFactor * (maxThrowForceMultiplier - 1f));
        heldObjectRb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);

        heldObjectInterface.OnDrop();
        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
        rotationOffset = Vector3.zero;
        throwChargeTime = 0f;
        isChargingThrow = false;
    }

    private void RotateObject()
    {
        if (heldObject == null) return;

        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
        float rotationAmount = scrollDelta.y * rotationSensitivity * Time.deltaTime;

        switch (currentRotationAxis)
        {
            case RotationAxis.X:
                rotationOffset.x += rotationAmount;
                break;
            case RotationAxis.Y:
                rotationOffset.y += rotationAmount;
                break;
        }
    }

    private void StopInteracting()
    {
        if (interactingObject != null)
        {
            heldObjectInterface.OnDrop();
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
        if (context.started)
        {
            isChargingThrow = true;
            throwChargeTime = 0f;
        }
        else if (context.performed)
        {
            throwInput = true;
        }
        else if (context.canceled)
        {
            isChargingThrow = false;
            throwChargeTime = 0f;
        }
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.duration < 0.2f)
            {
                currentRotationAxis = currentRotationAxis switch
                {
                    RotationAxis.X => RotationAxis.Y,
                    RotationAxis.Y => RotationAxis.X,
                    _ => RotationAxis.Y
                };
                Debug.Log($"Rotation Axis Changed to: {currentRotationAxis}");
            }
            else if (heldObject != null)
            {
                rotationOffset = Vector3.zero;
                Debug.Log("Reset rotation to upright position");
            }
        }
    }
}