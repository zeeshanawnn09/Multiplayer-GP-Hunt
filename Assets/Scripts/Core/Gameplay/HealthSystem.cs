using UnityEngine;
using Photon.Pun;
using System.Collections;

public class HealthSystem : MonoBehaviourPun
{
    [SerializeField]
    private int startingHealth = 100;

    [SerializeField]
    private float ghostRespawnDelaySeconds = 3f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => startingHealth;

    private bool _isDead;
    private bool _hasDisplayedHealth;
    private PlayerControls _playerControls;
    private PlayerRespawnSystem _playerRespawnSystem;
    private CharacterController _characterController;
    private Coroutine _ghostRespawnCoroutine;

    private void Start()
    {
        _playerControls = GetComponent<PlayerControls>();
        _playerRespawnSystem = GetComponent<PlayerRespawnSystem>();
        _characterController = GetComponent<CharacterController>();

        CurrentHealth = startingHealth;
        UpdateLocalHealthUI();
    }

    private void Update()
    {
        if (!_hasDisplayedHealth)
        {
            UpdateLocalHealthUI();
        }
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Each client should only modify health for its own player instance.
        if (!photonView.IsMine || _isDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateLocalHealthUI();

        if (CurrentHealth == 0)
        {
            _isDead = true;
            Debug.Log($"{photonView.Owner?.NickName} has died.");

            if (_playerControls != null && _playerControls.IsGhost)
            {
                StartGhostRespawnFlow();
            }
        }
    }

    private void StartGhostRespawnFlow()
    {
        if (_ghostRespawnCoroutine != null)
        {
            StopCoroutine(_ghostRespawnCoroutine);
        }

        photonView.RPC(nameof(RPC_SetVisualState), RpcTarget.All, false);

        if (photonView.IsMine && _characterController != null)
        {
            _characterController.enabled = false;
        }

        _ghostRespawnCoroutine = StartCoroutine(GhostRespawnCoroutine());
    }

    private IEnumerator GhostRespawnCoroutine()
    {
        float waitSeconds = Mathf.Max(0f, ghostRespawnDelaySeconds);
        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        if (!photonView.IsMine)
        {
            yield break;
        }

        if (_playerRespawnSystem != null && _playerRespawnSystem.TryGetRespawnPosition(out Vector3 respawnPosition))
        {
            _playerControls?.TeleportTo(respawnPosition);
        }

        CurrentHealth = startingHealth;
        _isDead = false;
        _hasDisplayedHealth = false;

        if (_characterController != null)
        {
            _characterController.enabled = true;
        }

        photonView.RPC(nameof(RPC_SetVisualState), RpcTarget.All, true);
        UpdateLocalHealthUI();
        _ghostRespawnCoroutine = null;
    }

    [PunRPC]
    private void RPC_SetVisualState(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererComponent in renderers)
        {
            rendererComponent.enabled = isVisible;
        }
    }

    private void UpdateLocalHealthUI()
    {
        // Only the local player should see and update their own health.
        if (!photonView.IsMine)
        {
            return;
        }

        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayHealth(CurrentHealth, MaxHealth);
            _hasDisplayedHealth = true;
        }
    }
}
