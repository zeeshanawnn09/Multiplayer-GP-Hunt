using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField]
    private Image crosshairImage;

    [SerializeField]
    private Color crosshairColor = Color.white;

    [SerializeField]
    private float crosshairScale = 1f;

    private Canvas crosshairCanvas;

    private void Start()
    {
        // If no crosshair image is assigned, try to find one in children
        if (crosshairImage == null)
        {
            crosshairImage = GetComponent<Image>();
        }

        if (crosshairImage == null)
        {
            Debug.LogWarning("CrosshairUI: No crosshair image assigned! Please assign an Image component in the Inspector.");
            return;
        }

        // Set initial properties
        SetCrosshairColor(crosshairColor);
        SetCrosshairScale(crosshairScale);
    }

    /// <summary>
    /// Sets the color of the crosshair
    /// </summary>
    public void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null)
        {
            crosshairImage.color = color;
        }
    }

    /// <summary>
    /// Sets the scale of the crosshair
    /// </summary>
    public void SetCrosshairScale(float scale)
    {
        if (crosshairImage != null)
        {
            crosshairImage.rectTransform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// Shows the crosshair
    /// </summary>
    public void ShowCrosshair()
    {
        if (crosshairImage != null)
        {
            crosshairImage.enabled = true;
        }
    }

    /// <summary>
    /// Hides the crosshair
    /// </summary>
    public void HideCrosshair()
    {
        if (crosshairImage != null)
        {
            crosshairImage.enabled = false;
        }
    }

    /// <summary>
    /// Checks if the crosshair is currently visible
    /// </summary>
    public bool IsCrosshairVisible()
    {
        return crosshairImage != null && crosshairImage.enabled;
    }
}
