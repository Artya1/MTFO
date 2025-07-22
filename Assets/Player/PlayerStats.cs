using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerStats : MonoBehaviour
{
    public int health;
    public GameObject DeathCanvas;
    void Awake()
    {
        health = 100;
        if (DeathCanvas != null)
        {
            DeathCanvas.SetActive(false);
        }
    }
    public void dmgTaken(int damage)
    {
        health -= damage;
        Debug.Log("Health: " + health);
        if (health <= 0)
        {
            DeathCanvas.SetActive(true);
            Destroy(gameObject);
        }
    }

    public void Restart()
    {
        Scene currentScene = SceneManager.GetActiveScene();

    // Load the scene again by its name.
        SceneManager.LoadScene(currentScene.name);
    }

}
