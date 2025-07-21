using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int health;

    void Awake()
    {
        health = 100;
    }
    public void dmgTaken(int damage)
    {
        health -= damage;
        Debug.Log("Health: " + health);
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    

}
