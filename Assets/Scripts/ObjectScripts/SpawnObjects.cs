using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnObjects : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnableObject
    {
        public GameObject prefab; // Prefab to spawn, weight type inferred from tag
    }

    [Header("Spawn Settings")]
    public List<SpawnableObject> spawnableObjects; // Objects to spawn
    public List<Transform> rootSpawnPoints; // Root spawn points
    public float spawnDelay = 0.5f; // Delay between spawns
    public float lightSpawnChance = 0.5f; // Chance for Light objects (0 to 1)
    public float mediumSpawnChance = 0.5f; // Chance for Medium objects (0 to 1)
    public float heavySpawnChance = 0.7f; // Chance for Heavy objects (0 to 1)
    public float doorSpawnChance = 0.7f; // Chance for Door objects (0 to 1)
    private List<Transform> allSpawnPoints; // All spawn points, including children
    private Dictionary<string, int> spawnCounters; // Tracks spawn count per prefab

    private void Start()
    {
        allSpawnPoints = new List<Transform>();
        spawnCounters = new Dictionary<string, int>();
        CollectRootSpawnPoints();
        StartCoroutine(SpawnAllObjectsCoroutine());
    }

    private void CollectRootSpawnPoints()
    {
        allSpawnPoints.Clear();
        foreach (Transform root in rootSpawnPoints)
        {
            if (root != null && root.gameObject.activeInHierarchy)
            {
                allSpawnPoints.Add(root);
            }
        }
        Debug.Log($"Collected {allSpawnPoints.Count} root spawn points");
    }

    private void CollectChildSpawnPoints(Transform parent)
    {
        if (parent == null || !parent.gameObject.activeInHierarchy) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                allSpawnPoints.Add(child);
                CollectChildSpawnPoints(child);
            }
        }
    }

    private IEnumerator SpawnAllObjectsCoroutine()
    {
        // Pass 1: Spawn Heavy and Door objects at root spawn points
        foreach (Transform spawnPoint in rootSpawnPoints)
        {
            if (spawnPoint != null && spawnPoint.gameObject.activeInHierarchy)
            {
                string spawnTag = spawnPoint.tag;
                float chance = spawnTag switch
                {
                    "Heavy" => heavySpawnChance,
                    "Door" => doorSpawnChance,
                    _ => 0f
                };

                if (UnityEngine.Random.value <= chance)
                {
                    GameObject spawnedObject = SpawnObjectAtPoint(spawnPoint, new[] { spawnTag });
                    if (spawnedObject != null)
                    {
                        CollectChildSpawnPoints(spawnedObject.transform);
                    }
                    if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);
                }
            }
        }

        Debug.Log($"After Heavy/Door pass, collected {allSpawnPoints.Count} spawn points");

        // Pass 2: Spawn Light and Medium objects at all spawn points
        foreach (Transform spawnPoint in allSpawnPoints)
        {
            if (spawnPoint != null && spawnPoint.gameObject.activeInHierarchy)
            {
                string spawnTag = spawnPoint.tag;
                float chance = spawnTag switch
                {
                    "Light" => lightSpawnChance,
                    "Medium" => mediumSpawnChance,
                    _ => 0f
                };

                if (chance > 0f && UnityEngine.Random.value <= chance)
                {
                    SpawnObjectAtPoint(spawnPoint, new[] { spawnTag });
                    if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);
                }
            }
        }
    }

    private GameObject SpawnObjectAtPoint(Transform spawnPoint, string[] allowedTags)
    {
        List<SpawnableObject> validObjects = spawnableObjects.FindAll(obj =>
            obj.prefab != null && System.Array.Exists(allowedTags, tag => string.Equals(tag, obj.prefab.tag, StringComparison.OrdinalIgnoreCase)));

        if (validObjects.Count == 0)
        {
            Debug.LogWarning($"No valid objects for tag {spawnPoint.tag} at {spawnPoint.name}");
            return null;
        }

        SpawnableObject selectedObject = validObjects[UnityEngine.Random.Range(0, validObjects.Count)];

        // Check for required components
        if (selectedObject.prefab.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning($"Prefab {selectedObject.prefab.name} lacks Rigidbody, skipping spawn at {spawnPoint.name}");
            return null;
        }
        if (selectedObject.prefab.GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"Prefab {selectedObject.prefab.name} lacks Collider, skipping spawn at {spawnPoint.name}");
            return null;
        }

        GameObject spawnedObject = Instantiate(selectedObject.prefab, spawnPoint.position, spawnPoint.rotation);

        // Assign unique ID: [PrefabName][Counter]
        string prefabName = selectedObject.prefab.name;
        if (!spawnCounters.ContainsKey(prefabName))
        {
            spawnCounters[prefabName] = 0;
        }
        spawnCounters[prefabName]++;
        spawnedObject.name = $"{prefabName}{spawnCounters[prefabName]}";
        Debug.Log($"Spawned {spawnedObject.name} at {spawnPoint.name}");

        spawnedObject.tag = selectedObject.prefab.tag;

        // Add or get SpawnPointHolder and set spawn point
        SpawnPointHolder spawnPointHolder = spawnedObject.GetComponent<SpawnPointHolder>() ?? spawnedObject.AddComponent<SpawnPointHolder>();
        spawnPointHolder.spawnPoint = spawnPoint;

        // Add PickupObject if missing
        PickupObject pickup = spawnedObject.GetComponent<PickupObject>() ?? spawnedObject.AddComponent<PickupObject>();

        // Parent Light/Medium objects to spawn point's parent
        if (spawnPoint.parent != null && (string.Equals(spawnedObject.tag, "Light", StringComparison.OrdinalIgnoreCase) || string.Equals(spawnedObject.tag, "Medium", StringComparison.OrdinalIgnoreCase)))
        {
            spawnedObject.transform.SetParent(spawnPoint.parent, worldPositionStays: true);
        }

        // Log physics settings for debugging
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        Collider collider = spawnedObject.GetComponent<Collider>();
        Debug.Log($"Configured {spawnedObject.name}: Mass={rb.mass}, IsKinematic={rb.isKinematic}, Constraints={rb.constraints}, Collider={collider.GetType().Name}, Drag={rb.linearDamping}, AngularDrag={rb.angularDamping}");

        return spawnedObject;
    }
}