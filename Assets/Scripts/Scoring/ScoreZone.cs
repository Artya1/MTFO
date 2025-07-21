using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ScoreZone : MonoBehaviour
{
    private BoxCollider boxCollider;
    private HashSet<GameObject> scoredObjects = new HashSet<GameObject>(); // Track objects in zone
    private bool isPlayerInZone; // Track player presence
    private float dollarAmount; // Dollar amount based on total score

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true; // Ensure collider is a trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            LogTotalScore();
            return;
        }

        if (other.TryGetComponent<PickupObject>(out var pickup) && scoredObjects.Add(pickup.GameObject))
        {
            LogTotalScore();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            LogTotalScore();
            return;
        }

        if (other.TryGetComponent<PickupObject>(out var pickup) && scoredObjects.Remove(pickup.GameObject))
        {
            LogTotalScore();
        }
    }

    private void LogTotalScore()
    {
        int totalScore = 0;
        foreach (GameObject obj in scoredObjects)
        {
            if (obj != null && obj.TryGetComponent<PickupObject>(out var pickup))
            {
                totalScore += pickup.Score;
            }
        }
        dollarAmount = totalScore / 100f; // Convert score to dollar amount
        if (Debug.isDebugBuild)
        {
            Debug.Log($"ScoreZone: Total Score of {scoredObjects.Count} overlapping objects: {totalScore}");
            Debug.Log($"ScoreZone: Dollar amount = ${dollarAmount:F2}");
        }
    }
}