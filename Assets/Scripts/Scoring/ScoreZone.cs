// tested by Brian Spayd

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ScoreZone : MonoBehaviour
{
    private BoxCollider boxCollider;
    private HashSet<GameObject> scoredObjects = new HashSet<GameObject>(); // Track objects in zone
    private bool isPlayerInZone; // Track player presence

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true; // Ensure collider is a trigger
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            LogTotalScore();
            return;
        }

        // Check for PickupObject on the entering object
        PickupObject pickup = other.GetComponent<PickupObject>();
        if (pickup != null && !scoredObjects.Contains(pickup.GameObject))
        {
            scoredObjects.Add(pickup.GameObject);
            LogTotalScore();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Update player presence
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
        }
        // Remove object when it exits
        else
        {
            PickupObject pickup = other.GetComponent<PickupObject>();
            if (pickup != null && scoredObjects.Remove(pickup.GameObject))
            {
                LogTotalScore();
            }
        }
    }

    private void LogTotalScore()
    {
        int totalScore = 0;
        foreach (GameObject obj in scoredObjects)
        {
            PickupObject pickup = obj.GetComponent<PickupObject>();
            if (pickup != null)
            {
                totalScore += pickup.Score;
            }
        }
        Debug.Log($"ScoreZone: Total Score of {scoredObjects.Count} overlapping objects: {totalScore}");
    }
}