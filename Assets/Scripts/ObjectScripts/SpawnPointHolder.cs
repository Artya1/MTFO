using UnityEngine;
public class SpawnPointHolder : MonoBehaviour
{
    public Transform spawnPoint;

    private void Start()
    {
        // If no spawn point is assigned, use the object's initial position
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject($"{name}_SpawnPoint").transform;
            spawnPoint.position = transform.position;
        }
    }

    // Draw gizmo for spawn point visualization in Scene view
    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            // Set color for the gizmo
            Gizmos.color = Color.cyan;

            // Draw a wire sphere at the spawn point
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);

            // Draw a line from the object to the spawn point
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}