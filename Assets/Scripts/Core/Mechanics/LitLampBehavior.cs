using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class LitLampBehavior : MonoBehaviour
{
    public GameObject Light;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private bool debugLogs = true;

    private bool _isLit = false;
    private PlayerControls _localPlayerInRange;
    private PhotonView _photonView;
    private UnityEngine.Light _pointLight;

    public bool IsLit => GetCurrentLitState();

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        EnsureLampLightReference();

        _isLit = false;
        ApplyLightState(_isLit);

        if (debugLogs)
        {
            Debug.Log($"[LitLampBehavior] Awake on '{name}'. PointLight found: {_pointLight != null}, ParentLightGO: {Light != null}");
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
            bool targetLitState = !GetCurrentLitState();

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
        if (!PhotonNetwork.IsMasterClient || GetCurrentLitState() == targetLitState)
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

        ApplyLightState(_isLit);

        if (debugLogs)
        {
            Debug.Log($"[LitLampBehavior] '{name}' set lit state to {_isLit}. PointLight enabled: {_pointLight != null && _pointLight.enabled}");
        }

        SetPromptVisible(_localPlayerInRange != null);
    }

    public bool TryExtinguishFromSoul()
    {
        if (debugLogs)
        {
            Debug.Log($"[LitLampBehavior] Soul trying to extinguish '{name}'. Current lit state: {GetCurrentLitState()}");
        }

        if (!GetCurrentLitState())
        {
            return false;
        }

        if (!PhotonNetwork.InRoom || _photonView == null)
        {
            RPC_SetLampLitState(false);
            if (LampProgressManager.Instance != null)
            {
                LampProgressManager.Instance.NotifyLampStateChangedLocalFallback(false);
            }

            return true;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            _photonView.RPC(nameof(RPC_RequestExtinguishLamp), RpcTarget.MasterClient);
            return true;
        }

        _photonView.RPC(nameof(RPC_SetLampLitState), RpcTarget.AllBuffered, false);

        if (LampProgressManager.Instance != null)
        {
            LampProgressManager.Instance.NotifyLampStateChangedByMaster(false);
        }

        if (debugLogs)
        {
            Debug.Log($"[LitLampBehavior] '{name}' extinguished by soul.");
        }

        return true;
    }

    [PunRPC]
    private void RPC_RequestExtinguishLamp()
    {
        if (!PhotonNetwork.IsMasterClient || !GetCurrentLitState())
        {
            return;
        }

        _photonView.RPC(nameof(RPC_SetLampLitState), RpcTarget.AllBuffered, false);

        if (LampProgressManager.Instance != null)
        {
            LampProgressManager.Instance.NotifyLampStateChangedByMaster(false);
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

    private void EnsureLampLightReference()
    {
        if (_pointLight != null)
        {
            return;
        }

        _pointLight = GetComponentInChildren<UnityEngine.Light>(true);
        if (_pointLight != null)
        {
            Light = _pointLight.gameObject;
        }
        else
        {
            Debug.LogWarning($"{name}: No Point Light found for LitLampBehavior. Assign a light object or add a child Light component.");
        }
    }

    private void ApplyLightState(bool isLit)
    {
        if (_pointLight != null)
        {
            _pointLight.enabled = isLit;
        }

        if (Light != null)
        {
            Light.SetActive(isLit);
        }

        if (debugLogs)
        {
            Debug.Log($"[LitLampBehavior] '{name}' ApplyLightState({isLit}) -> pointLightEnabled={_pointLight != null && _pointLight.enabled}");
        }
    }

    private bool GetCurrentLitState()
    {
        if (_pointLight != null)
        {
            return _pointLight.enabled;
        }

        if (Light != null)
        {
            return Light.activeSelf;
        }

        return _isLit;
    }

    public Vector3 GetLightWorldPosition()
    {
        if (_pointLight != null)
        {
            return _pointLight.transform.position;
        }

        if (Light != null)
        {
            return Light.transform.position;
        }

        return transform.position;
    }

}
