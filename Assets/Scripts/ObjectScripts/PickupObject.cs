// Tested by Brian Spayd

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupObject : MonoBehaviour, IObject
{
    public GameObject GameObject => gameObject;

    public void OnPickup()
    {
        // Example: Disable renderer or change color when picked up
        Debug.Log($"{gameObject.name} was picked up!");
    }

    public void OnDrop()
    {
        // Example: Re-enable renderer or reset state when dropped
        Debug.Log($"{gameObject.name} was dropped!");
    }
}