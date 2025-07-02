// tested by Brian Spayd

using UnityEngine;

public interface IObject
{
    GameObject GameObject { get; } // Reference to the GameObject
    int Health { get; set; } // Numeric health value
    int Score { get; } // Numeric score value
    void OnPickup(); // Called when object is picked up
    void OnDrop();   // Called when object is dropped
}