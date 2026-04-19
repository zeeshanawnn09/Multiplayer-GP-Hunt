using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShieldSystem : MonoBehaviourPun
{
    [Header("Shield Timing")]
    [SerializeField]
    private float shieldDurationSeconds = 5f;

    [SerializeField]
    private float cooldownSeconds = 0f;

    [Header("Shield Visual")]
    [SerializeField]
    private GameObject shieldVisualRoot;

    public bool IsShieldActive { get; private set; }

    private float _nextAllowedActivateTime;
    private Coroutine _activeShieldRoutine;
    private PlayerControls _playerControls;

    private void Start()
    {
        _playerControls = GetComponent<PlayerControls>();
        SetShieldVisual(false);
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        bool wasPressed = Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
        if (!wasPressed)
        {
            return;
        }

        if (!CanUseShield())
        {
            return;
        }

        TryActivateShield();
    }

    public void TryActivateShield()
    {
        if (!CanUseShield())
        {
            return;
        }

        if (IsShieldActive)
        {
            return;
        }

        if (Time.time < _nextAllowedActivateTime)
        {
            return;
        }

        if (_activeShieldRoutine != null)
        {
            StopCoroutine(_activeShieldRoutine);
        }

        _activeShieldRoutine = StartCoroutine(ShieldActiveRoutine());
    }

    private IEnumerator ShieldActiveRoutine()
    {
        photonView.RPC(nameof(RPC_SetShieldState), RpcTarget.All, true);

        float duration = Mathf.Max(0f, shieldDurationSeconds);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        photonView.RPC(nameof(RPC_SetShieldState), RpcTarget.All, false);
        _nextAllowedActivateTime = Time.time + Mathf.Max(0f, cooldownSeconds);
        _activeShieldRoutine = null;
    }

    [PunRPC]
    private void RPC_SetShieldState(bool isActive)
    {
        IsShieldActive = isActive;
        SetShieldVisual(isActive);
    }

    private void SetShieldVisual(bool isVisible)
    {
        if (shieldVisualRoot != null)
        {
            shieldVisualRoot.SetActive(isVisible);
        }
    }

    private bool CanUseShield()
    {
        return _playerControls != null && _playerControls.IsPriest;
    }
}
