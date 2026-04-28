using UnityEngine;

/// <summary>
/// Plays a looping audio clip for a single scene.
/// Attach to any GameObject in the scene — does NOT persist.
/// </summary>
public class SceneAmbience : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    void Start()
    {
        if (clip != null)
            audioSource.Play();
        else
            Debug.LogWarning("SceneAmbience: No AudioClip assigned on " + gameObject.name);
    }

    // Lets you tweak volume live in the Inspector during Play Mode
    void Update()
    {
        if (audioSource.volume != volume)
            audioSource.volume = volume;
    }
}