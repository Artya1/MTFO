using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour, IObject
{
    public int health = 100; // Default health value, editable in Inspector
    public int score = 100; // Deprecated, kept for Inspector compatibility but unused
    public float scoreMultiplier = 1f; // Custom multiplier for score, editable in Inspector
    // public GameObject prefab; Prefab to replace this object's model, editable in Inspector
    public float minForceThreshold = 5f; // Minimum force to cause damage, editable in Inspector
    public float verticalChangeThreshold = 1f; // Minimum vertical change to trigger interaction, editable in Inspector
    public float positionCheckInterval = 0.5f; // Interval for position checks, editable in Inspector

    private bool hasBeenInteracted = false; // Tracks if object has been interacted with
    private Vector3 lastPosition; // Tracks last position for vertical change detection
    private Rigidbody rb; // Cached Rigidbody component
    private float nextPositionCheckTime; // Time for next position check

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
                _ => 10f // Default for unknown tags
            };
            return Mathf.RoundToInt(health * tagMultiplier * scoreMultiplier);
        }
    }

    void Start()
    {
        lastPosition = transform.position;
        rb = GetComponent<Rigidbody>(); // Cache Rigidbody
        nextPositionCheckTime = Time.time + positionCheckInterval;
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
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (hasBeenInteracted && collision.gameObject != gameObject)
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