using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene loading
using UnityEngine.UI; // Required if you use legacy Button component, not always needed for TMP buttons

public class MenuManager : MonoBehaviour // Ensure this class name matches your file name
{
    // This function will be called when the "Start Game" button is clicked
    public void StartGame()
    {
        Debug.Log("Start Game button clicked!"); // Log to console for debugging
        // Example: Load a new scene
        SceneManager.LoadScene("YourGameSceneName"); // Replace "YourGameSceneName" with the actual name of your game scene
    }

    // This function will be called when the "Options" button is clicked
    public void OpenOptions()
    {
        Debug.Log("Options button clicked!");
        // Example: Activate an options panel or load an options scene
        // If you have an options panel as a child of the Canvas:
        // GameObject optionsPanel = transform.Find("OptionsPanel").gameObject;
        // optionsPanel.SetActive(true);
        SceneManager.LoadScene("OptionsScene"); // Or load an Options scene
    }

    // This function will be called when the "Quit Game" button is clicked
    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked!");
        // Quits the application (only works in a built game, not in the Editor)
        Application.Quit();

        // If you want to stop play in the editor immediately (for testing QuitGame)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}