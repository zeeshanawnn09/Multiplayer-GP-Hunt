using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages cinematic intro video playback with skip functionality.
/// - Plays video from VideoPlayer component
/// - Displays "Press any key to skip" text
/// - Loads game scene when player presses any key or video finishes
/// </summary>
public class CinematicIntroController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text skipPromptText;
    [SerializeField] private string gameSceneName = "Test"; // Scene to load after video
    [SerializeField] private float skipPromptFadeInDelay = 1f; // Delay before showing skip text

    private bool hasVideoEnded = false;
    private bool canSkip = false;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[CinematicIntroController] VideoPlayer not assigned! Please assign it in the Inspector.");
            return;
        }

        // Hide skip prompt initially
        if (skipPromptText != null)
        {
            skipPromptText.gameObject.SetActive(false);
        }

        // Register for video ended event
        videoPlayer.loopPointReached += OnVideoEnded;

        // Start video
        videoPlayer.Play();

        // Show skip prompt after delay
        if (skipPromptText != null)
        {
            Invoke(nameof(ShowSkipPrompt), skipPromptFadeInDelay);
        }
    }

    private void Update()
    {
        // Check if player pressed any key to skip
        if (canSkip && Input.anyKeyDown)
        {
            SkipToGame();
        }
    }

    /// <summary>
    /// Called when video finishes playing
    /// </summary>
    private void OnVideoEnded(VideoPlayer vp)
    {
        hasVideoEnded = true;
        LoadGameScene();
    }

    /// <summary>
    /// Shows the skip prompt text after a delay
    /// </summary>
    private void ShowSkipPrompt()
    {
        if (skipPromptText != null)
        {
            skipPromptText.gameObject.SetActive(true);
        }

        canSkip = true;
    }

    /// <summary>
    /// Skip video and load game scene
    /// </summary>
    private void SkipToGame()
    {
        // Stop video
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        LoadGameScene();
    }

    /// <summary>
    /// Loads the game scene
    /// </summary>
    private void LoadGameScene()
    {
        // Unregister from video event
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
        }

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnded;
        }
    }
}
