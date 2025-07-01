using UnityEngine;

public class DEBUG_SpawnPoint : MonoBehaviour
{
    [Header("Debug Settings")]
    public float sphereRadius = 0.2f; // Size of debug sphere

    private void OnDrawGizmosSelected()
    {
        // Set color based on the GameObject's tag
        switch (gameObject.tag)
        {
            case "Light":
                Gizmos.color = Color.green;
                break;
            case "Medium":
                Gizmos.color = Color.blue;
                break;
            case "Heavy":
                Gizmos.color = Color.red;
                break;
            case "Door":
                Gizmos.color = Color.yellow;
                break;
            default:
                Gizmos.color = Color.cyan; // Fallback for untagged or other tags
                break;
        }

        // Draw a wireframe sphere at this GameObject's position
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}