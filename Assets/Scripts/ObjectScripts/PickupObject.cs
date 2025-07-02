// tested by Brian Spayd

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour, IObject
{
    [SerializeField] private int health = 100; // Default health value, editable in Inspector
    [SerializeField] private int score = 100; // Deprecated, kept for Inspector compatibility but unused
    [SerializeField] private float scoreMultiplier = 1f; // Custom multiplier for score, editable in Inspector

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

    public void OnPickup()
    {
        Debug.Log($"{gameObject.name} was picked up! Health: {health}, Score: {Score}");
    }

    public void OnDrop()
    {
        Debug.Log($"{gameObject.name} was dropped! Health: {health}, Score: {Score}");
    }
}