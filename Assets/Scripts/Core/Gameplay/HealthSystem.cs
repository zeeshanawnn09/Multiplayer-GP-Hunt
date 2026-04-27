using UnityEngine;
using Photon.Pun;
using System.Collections;

public class HealthSystem : MonoBehaviourPun
{
    [SerializeField]
    private int startingHealth = 100;

    [SerializeField]
    private float ghostRespawnDelaySeconds = 3f;

    [Header("Priest Death")]
    [SerializeField]
    private GameObject ashPotPrefab;

    [SerializeField]
    private Vector3 ashPotSpawnOffset = new Vector3(0f, 0.2f, 0f);

    public int CurrentHealth { get; private set; }
    public int MaxHealth => startingHealth;
    public bool IsDead => _isDead;

    private bool _isDead;
    private bool _hasDisplayedHealth;
    private bool _hasRequestedPriestAshPot;
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
        _hasRequestedPriestAshPot = false;
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

        ShieldSystem shieldSystem = GetComponent<ShieldSystem>();
        if (shieldSystem != null && shieldSystem.IsShieldActive)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateLocalHealthUI();

        if (CurrentHealth == 0)
        {
            _isDead = true;
            Debug.Log($"{photonView.Owner?.NickName} has died.");
            photonView.RPC(nameof(RPC_SetPlayerDeadState), RpcTarget.All, true);

            if (_playerControls != null && _playerControls.IsGhost)
            {
                StartGhostRespawnFlow();
                return;
            }

            StartPriestDeathFlow();
        }
    }

    [PunRPC]
    public void RPC_ApplyDamage(int amount)
    {
        ApplyDamage(amount);
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

    private void StartPriestDeathFlow()
    {
        photonView.RPC(nameof(RPC_SetVisualState), RpcTarget.All, false);

        if (photonView.IsMine)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            if (PhotonNetwork.InRoom)
            {
                RequestAshPotSpawn(transform.position + ashPotSpawnOffset);
            }
            else
            {
                SpawnAshPotLocally(transform.position + ashPotSpawnOffset, photonView.ViewID);
            }
        }
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
        _hasRequestedPriestAshPot = false;
        photonView.RPC(nameof(RPC_SetPlayerDeadState), RpcTarget.All, false);

        if (_characterController != null)
        {
            _characterController.enabled = true;
        }

        photonView.RPC(nameof(RPC_SetVisualState), RpcTarget.All, true);
        UpdateLocalHealthUI();
        _ghostRespawnCoroutine = null;
    }

    [PunRPC]
    private void RPC_SetPlayerDeadState(bool isDead)
    {
        _isDead = isDead;
        _playerControls?.RPC_SetDeadState(isDead);

        if (isDead && photonView.IsMine && _playerControls != null && _playerControls.IsPriest)
        {
            RequestAshPotSpawn(transform.position + ashPotSpawnOffset);
        }
    }

    [PunRPC]
    private void RPC_RequestSpawnAshPot(int deadPlayerViewId, Vector3 spawnPosition)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        SpawnAshPotOnMaster(deadPlayerViewId, spawnPosition);
    }

    private void RequestAshPotSpawn(Vector3 spawnPosition)
    {
        if (_hasRequestedPriestAshPot)
        {
            return;
        }

        _hasRequestedPriestAshPot = true;

        if (!PhotonNetwork.InRoom)
        {
            SpawnAshPotLocally(spawnPosition, photonView.ViewID);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            SpawnAshPotOnMaster(photonView.ViewID, spawnPosition);
            return;
        }

        photonView.RPC(nameof(RPC_RequestSpawnAshPot), RpcTarget.MasterClient, photonView.ViewID, spawnPosition);
    }

    private void SpawnAshPotOnMaster(int deadPlayerViewId, Vector3 spawnPosition)
    {

        AshPotPickup[] activePots = FindObjectsByType<AshPotPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < activePots.Length; i++)
        {
            if (activePots[i] != null && activePots[i].SourcePlayerViewId == deadPlayerViewId)
            {
                return;
            }
        }

        if (ashPotPrefab == null)
        {
            Debug.LogWarning("Ash pot prefab is not assigned. Drag and drop an Ash Pot prefab into HealthSystem.");
            return;
        }

        // Photon instantiates from Resources by prefab name, so we derive the name from the assigned prefab.
        string prefabName = ashPotPrefab.name;
        GameObject ashPotObject = PhotonNetwork.InstantiateRoomObject(prefabName, spawnPosition, Quaternion.identity);
        AshPotPickup ashPotPickup = ashPotObject.GetComponent<AshPotPickup>();

        if (ashPotPickup == null)
        {
            Debug.LogWarning($"Spawned ash pot prefab '{prefabName}' is missing AshPotPickup component.");
            return;
        }

        ashPotPickup.Initialize(deadPlayerViewId);
    }

    private void SpawnAshPotLocally(Vector3 spawnPosition, int deadPlayerViewId)
    {
        if (ashPotPrefab == null)
        {
            Debug.LogWarning("Ash pot prefab is not assigned. Drag and drop an Ash Pot prefab into HealthSystem.");
            return;
        }

        GameObject ashPotObject = Instantiate(ashPotPrefab, spawnPosition, Quaternion.identity);
        AshPotPickup ashPotPickup = ashPotObject.GetComponent<AshPotPickup>();

        if (ashPotPickup == null)
        {
            Debug.LogWarning($"Spawned ash pot prefab '{ashPotPrefab.name}' is missing AshPotPickup component.");
            return;
        }

        ashPotPickup.Initialize(deadPlayerViewId);
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

    [PunRPC]
    public void RPC_RespawnPriest(Vector3 respawnPosition)
    {
        Debug.Log($"[HealthSystem.RPC_RespawnPriest] Called for {photonView.Owner?.NickName} at position {respawnPosition}");

        CurrentHealth = startingHealth;
        _isDead = false;
        _hasDisplayedHealth = false;
        _hasRequestedPriestAshPot = false;
        photonView.RPC(nameof(RPC_SetPlayerDeadState), RpcTarget.All, false);

        if (photonView.IsMine)
        {
            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            _playerControls?.TeleportTo(respawnPosition);
        }

        photonView.RPC(nameof(RPC_SetVisualState), RpcTarget.All, true);

        if (photonView.IsMine)
        {
            UpdateLocalHealthUI();
        }

        Debug.Log($"[HealthSystem.RPC_RespawnPriest] SUCCESS! {photonView.Owner?.NickName} has been respawned");
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
