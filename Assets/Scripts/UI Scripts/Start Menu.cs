using UnityEngine;
using UnityEngine.SceneManagement;
public class StartMenu : MonoBehaviour
{
    // --- Public Fields ---
    // Assign these in the Unity Inspector by dragging the corresponding
    // Canvas GameObjects from your scene hierarchy into these slots.

    [Header("Canvases")]
    [Tooltip("The main canvas with the Start and Quit buttons.")]
    public GameObject startCanvas;

    [Tooltip("The canvas that asks the user to confirm if they want to quit.")]
    public GameObject quitConfirmCanvas;

    [Header("Scene Configuration")]
    [Tooltip("The name of the main game scene to load when the Start button is clicked.")]
    public string mainGameSceneName = "Brandon Scene";


    // --- Unity Methods ---

    void Start()
    {
        // Ensure the initial state is correct when the scene loads.
        // The start canvas should be visible, and the quit confirmation should be hidden.
        if (startCanvas != null)
        {
            startCanvas.SetActive(true);
        }
        if (quitConfirmCanvas != null)
        {
            quitConfirmCanvas.SetActive(false);
        }
    }


    // --- Public Methods for UI Buttons ---

    /// <summary>
    /// This method should be linked to the 'Start' button's OnClick event.
    /// It loads the main game scene.
    /// </summary>
    public void StartGame()
    {
        // Check if the scene name is provided to avoid errors.
        if (!string.IsNullOrEmpty(mainGameSceneName))
        {
            Debug.Log("Starting game... Loading scene: " + mainGameSceneName);
            SceneManager.LoadScene(mainGameSceneName);
        }
        else
        {
            Debug.LogError("Main Game Scene Name is not set in the StartMenuManager script!");
        }
    }

    /// <summary>
    /// This method should be linked to the 'Quit' button on the main start canvas.
    /// It disables the start canvas and enables the quit confirmation canvas.
    /// </summary>
    public void ShowQuitConfirmation()
    {
        if (startCanvas != null && quitConfirmCanvas != null)
        {
            Debug.Log("Showing quit confirmation canvas.");
            startCanvas.SetActive(false);
            quitConfirmCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("One or both canvas references are not set in the Inspector!");
        }
    }

    /// <summary>
    /// This method should be linked to the 'Go Back' button on the quit confirmation canvas.
    /// It disables the quit confirmation canvas and re-enables the main start canvas.
    /// </summary>
    public void HideQuitConfirmation()
    {
        if (startCanvas != null && quitConfirmCanvas != null)
        {
            Debug.Log("Hiding quit confirmation canvas.");
            quitConfirmCanvas.SetActive(false);
            startCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("One or both canvas references are not set in the Inspector!");
        }
    }

    /// <summary>
    /// This method should be linked to the 'Yes, Quit' button on the quit confirmation canvas.
    /// It closes the application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting application...");

        // Application.Quit() only works in a built game, not in the Unity Editor.
        // This preprocessor directive will stop play mode in the editor for testing purposes.
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}