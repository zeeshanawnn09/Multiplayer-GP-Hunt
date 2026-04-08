using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class LitLampBehavior : MonoBehaviour
{
    public GameObject Light;
    [SerializeField] private GameObject interactPrompt;

    private bool _isLit = false;
    private PlayerControls _localPlayerInRange;
    private PhotonView _photonView;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        EnsureLampLightReference();

        if (Light != null)
        {
            Light.SetActive(_isLit);
        }

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
            bool targetLitState = !_isLit;

            if (PhotonNetwork.InRoom && _photonView != null && _photonView.ViewID != 0)
            {
                _photonView.RPC(nameof(RPC_RequestLightLamp), RpcTarget.MasterClient, targetLitState);
            }
            else
            {
                Debug.LogWarning($"{name}: Lamp networking is not ready (InRoom={PhotonNetwork.InRoom}, ViewID={(_photonView != null ? _photonView.ViewID : 0)}). Applying local fallback.");
                RPC_SetLampLitState(targetLitState);

                if (LampProgressManager.Instance != null)
                {
                    LampProgressManager.Instance.NotifyLampStateChangedLocalFallback(targetLitState);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_RequestLightLamp(bool targetLitState, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || _isLit == targetLitState)
        {
            return;
        }

        _photonView.RPC(nameof(RPC_SetLampLitState), RpcTarget.AllBuffered, targetLitState);

        if (LampProgressManager.Instance != null)
        {
            LampProgressManager.Instance.NotifyLampStateChangedByMaster(targetLitState);
        }
        else
        {
            Debug.LogWarning("LampProgressManager is missing in scene. Progress bar will not update.");
        }
    }

    [PunRPC]
    private void RPC_SetLampLitState(bool isLit)
    {
        _isLit = isLit;

        if (Light != null)
        {
            Light.SetActive(_isLit);
        }

        SetPromptVisible(_localPlayerInRange != null);
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

    private void EnsureLampLightReference()
    {
        if (Light != null)
        {
            return;
        }

        UnityEngine.Light childPointLight = GetComponentInChildren<UnityEngine.Light>(true);
        if (childPointLight != null)
        {
            Light = childPointLight.gameObject;
        }
        else
        {
            Debug.LogWarning($"{name}: No Point Light found for LitLampBehavior. Assign a light object or add a child Light component.");
        }
    }

}
