using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool loadOnAnyKey = true;
    [SerializeField] private TMP_Text blinkingText;
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    private bool isLoading;

    // Update is called once per frame
    void Update()
    {
        UpdateBlinkingText();

        if (!loadOnAnyKey || isLoading)
        {
            return;
        }

        if (Input.anyKeyDown)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("Scene name is empty on SceneManager. Assign a scene name in the Inspector.", this);
                return;
            }

            isLoading = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void UpdateBlinkingText()
    {
        if (blinkingText == null)
        {
            return;
        }

        float safeSpeed = Mathf.Max(0.01f, blinkSpeed);
        float low = Mathf.Min(minAlpha, maxAlpha);
        float high = Mathf.Max(minAlpha, maxAlpha);
        float t = (Mathf.Sin(Time.time * safeSpeed) + 1f) * 0.5f;

        Color color = blinkingText.color;
        color.a = Mathf.Lerp(low, high, t);
        blinkingText.color = color;
    }
}
