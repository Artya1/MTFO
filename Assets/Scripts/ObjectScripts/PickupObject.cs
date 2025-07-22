using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour, IObject
{
    public int health = 100; // Default health value, editable in Inspector
    public int score = 100; // Deprecated, kept for Inspector compatibility but unused
    public float scoreMultiplier = 1f; // Custom multiplier for score, editable in Inspector
    public float minForceThreshold = 5f; // Minimum force to cause damage, editable in Inspector
    public float verticalChangeThreshold = 1f; // Minimum vertical change to trigger interaction, editable in Inspector
    public float positionCheckInterval = 0.5f; // Interval for position checks, editable in Inspector
    public float heavyMass = 1000f; // High mass for Heavy objects to resist player collisions
    public float heavyDrag = 5f; // High drag for Heavy objects to resist movement
    public float pushMass = 10f; // Reduced mass during push action for Heavy objects
    public float pushDrag = 0.5f; // Reduced drag during push action for Heavy objects

    private bool hasBeenInteracted = false; // Tracks if object has been interacted with
    private Vector3 lastPosition; // Tracks last position for vertical change detection
    private Rigidbody rb; // Cached Rigidbody component
    private float nextPositionCheckTime; // Time for next position check
    private float defaultMass; // Default mass to restore after push
    private float defaultDrag; // Default drag to restore after push

    public GameObject GameObject => gameObject;

    public int Health
    {
        get => health;
        set => health = value;
    }

    public int Score
    {
        get
        {
            float tagMultiplier = gameObject.tag switch
            {
                "Light" => 10f,
                "Medium" => 20f,
                "Heavy" => 50f,
                "Door" => 30f,
                _ => 10f
            };
            return Mathf.RoundToInt(health * tagMultiplier * scoreMultiplier);
        }
    }

    void Start()
    {
        lastPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        nextPositionCheckTime = Time.time + positionCheckInterval;
        defaultMass = rb.mass;
        defaultDrag = rb.linearDamping;

        // Apply high mass/drag to Heavy objects at start
        if (gameObject.CompareTag("Heavy"))
        {
            rb.mass = heavyMass;
            rb.linearDamping = heavyDrag;
        }
    }

    void Update()
    {
        if (hasBeenInteracted && Time.time >= nextPositionCheckTime)
        {
            nextPositionCheckTime = Time.time + positionCheckInterval;
            if (Mathf.Abs(transform.position.y - lastPosition.y) > verticalChangeThreshold)
            {
                // Significant vertical change detected, additional logic can be added if needed
            }
            lastPosition = transform.position;
        }
    }

    public void OnPickup()
    {
        hasBeenInteracted = true;
    }

    public void OnDrop()
    {
        hasBeenInteracted = true;
        if (gameObject.CompareTag("Heavy"))
        {
            ResetPhysics();
        }
    }

    public void ApplyPushPhysics()
    {
        if (gameObject.CompareTag("Heavy"))
        {
            rb.mass = pushMass;
            rb.linearDamping = pushDrag;
        }
    }

    public void ResetPhysics()
    {
        if (gameObject.CompareTag("Heavy"))
        {
            rb.mass = heavyMass;
            rb.linearDamping = heavyDrag;
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (hasBeenInteracted && collision.gameObject != gameObject && !collision.gameObject.CompareTag("Player"))
        {
            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > minForceThreshold)
            {
                int damage = Mathf.RoundToInt(impactForce);
                Health -= damage;
                if (Health <= 0)
                {
                    Destroy(gameObject);
                }
            }
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}