using UnityEngine;

public class LitLampBehavior : MonoBehaviour
{
    public GameObject Light;
    [SerializeField] private GameObject interactPrompt;

    private bool _isLit = false;
    private PlayerControls _localPlayerInRange;

    private void Awake()
    {
        SetPromptVisible(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls.gameObject == PlayerControls.localPlayerInstance)
        {
            _localPlayerInRange = playerControls;
            SetPromptVisible(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls == _localPlayerInRange)
        {
            _localPlayerInRange = null;
            SetPromptVisible(false);
        }
    }

    private void Update()
    {
        if (_localPlayerInRange == null)
        {
            return;
        }

        if (_localPlayerInRange.ConsumeInteractPressed())
        {
            _isLit = !_isLit;
            Light.SetActive(_isLit);
        }
    }

    private void OnDisable()
    {
        _localPlayerInRange = null;
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(isVisible);
        }
    }

}
