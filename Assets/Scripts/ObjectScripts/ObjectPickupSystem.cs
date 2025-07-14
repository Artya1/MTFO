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
                IObject pickupObject = hit.collider.GetComponent<IObject>();
                if (pickupObject != null)
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

                            heldObject.transform.position = holdPosition;
                            heldObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

                            heldObjectInterface.OnPickup();
                        }
                    }
                    else if (tag == "Heavy")
                    {
                        interactingObject = targetObject;
                        interactingObjectRb = interactingObject.GetComponent<Rigidbody>();
                        heldObjectInterface = pickupObject;
                        heldObjectInterface.OnPickup();
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
                }
            }
        }
        else if (throwInput && heldObject == null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupLayer, QueryTriggerInteraction.Ignore))
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
                    if (tag != "Door") pushDirection.y = 0f;
                    rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Impulse);
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
        else if (interactingObject != null)
        {
            if (throwInput && interactingObject.CompareTag("Heavy"))
            {
                PushObject();
            }
            else if (interactInput)
            {
                StopInteracting();
            }
        }

        interactInput = false;
        throwInput = false;
    }

    private void UpdateHeldObject()
    {
        if (heldObject == null) return;

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

        heldObject.transform.SetPositionAndRotation(holdPosition, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
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

        float dropForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        heldObjectRb.AddForce(playerCamera.transform.forward * dropForce * 0.2f, ForceMode.Impulse);

        heldObjectInterface.OnDrop();
        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
    }

    private void ThrowObject()
    {
        heldObjectRb.isKinematic = false;
        heldObjectRb.useGravity = true;

        float throwForce = heldObject.CompareTag("Light") ? lightThrowForce : mediumThrowForce;
        heldObjectRb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);

        heldObjectInterface.OnDrop();
        heldObject = null;
        heldObjectRb = null;
        heldObjectInterface = null;
    }

    private void PushObject()
    {
        Vector3 pushDirection = playerCamera.transform.forward;
        pushDirection.y = 0f;
        interactingObjectRb.AddForce(pushDirection.normalized * heavyPushForce, ForceMode.Impulse);
        StopInteracting();
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
        throwInput = context.performed;
    }
}