// Tested by Brian Spayd

using UnityEngine;

public class KillZ : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object has a component that stores spawn point
        SpawnPointHolder spawnHolder = other.GetComponent<SpawnPointHolder>();
        
        if (spawnHolder != null && spawnHolder.spawnPoint != null)
        {
            // Teleport the object to its spawn point
            other.transform.position = spawnHolder.spawnPoint.position;
        }
        else
        {
            Debug.LogWarning($"Object {other.name} entered kill zone but has no spawn point defined!");
        }
    }
}