using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent AudioManager — attach to a GameObject in your first scene.
/// Assign one AudioClip per scene name in the Inspector.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
    }

    [Header("Scene Music Assignments")]
    public SceneMusic[] sceneMusicList;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton — destroy duplicate if one already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip clipForScene = GetClipForScene(scene.name);

        if (clipForScene == null)
        {
            // No music assigned for this scene — stop playback
            audioSource.Stop();
            return;
        }

        // Already playing this exact clip — let it continue seamlessly
        if (audioSource.clip == clipForScene && audioSource.isPlaying)
            return;

        audioSource.clip = clipForScene;
        audioSource.Play();
    }

    AudioClip GetClipForScene(string sceneName)
    {
        foreach (var entry in sceneMusicList)
        {
            if (entry.sceneName == sceneName)
                return entry.clip;
        }
        return null;
    }
}