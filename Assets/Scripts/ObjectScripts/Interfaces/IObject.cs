using UnityEngine;

public interface IObject
{
    GameObject GameObject { get; } // Reference to the GameObject
    void OnPickup(); // Called when object is picked up
    void OnDrop();   // Called when object is dropped
}