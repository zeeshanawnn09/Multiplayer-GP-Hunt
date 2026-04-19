using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PhotonView))]
public class PriestRespawnPoint : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject respawnPrompt;

    [SerializeField]
    private TMP_Text respawnPromptText;

    [SerializeField]
    private float interactionDistance = 3f;

    private PlayerControls _localPlayerInRange;

    private void Awake()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        // Check if local player with ash pot is in range
        if (_localPlayerInRange == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (!_localPlayerInRange.HasAshPot)
        {
            SetPromptVisible(false);
            return;
        }

        SetPromptVisible(true);
        HandleRespawnInput();
    }

    private void HandleRespawnInput()
    {
        bool isRPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (!isRPressed)
        {
            return;
        }

        Debug.Log("[PriestRespawnPoint] R key pressed, requesting respawn");
        RequestRespawn();
    }

    private void RequestRespawn()
    {
        if (_localPlayerInRange == null || !_localPlayerInRange.HasAshPot)
        {
            Debug.LogWarning("[PriestRespawnPoint] Cannot respawn: player not in range or player doesn't have ash pot");
            return;
        }

        int deadPlayerViewId = _localPlayerInRange.CarriedAshPotSourcePlayerViewId;
        if (deadPlayerViewId <= 0)
        {
            Debug.LogWarning("[PriestRespawnPoint] Cannot respawn: carried ash pot source player ID is invalid");
            return;
        }

        Debug.Log($"[PriestRespawnPoint] Requesting respawn for dead priest with ViewID {deadPlayerViewId}");

        photonView.RPC(nameof(RPC_RequestRespawn), RpcTarget.MasterClient, deadPlayerViewId, transform.position, _localPlayerInRange.photonView.OwnerActorNr);
    }

    [PunRPC]
    private void RPC_RequestRespawn(int deadPlayerViewId, Vector3 respawnPosition, int requestingActorNumber)
    {
        Debug.Log($"[PriestRespawnPoint.RPC_RequestRespawn] Called. IsMasterClient: {PhotonNetwork.IsMasterClient}, DeadPlayerViewID: {deadPlayerViewId}, RequestingActorNumber: {requestingActorNumber}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[PriestRespawnPoint.RPC_RequestRespawn] Not master client, ignoring");
            return;
        }

        // Find the dead priest's PlayerControls by ViewID
        PlayerControls[] allPlayers = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        PlayerControls deadPriest = null;

        foreach (PlayerControls player in allPlayers)
        {
            if (player.photonView.ViewID == deadPlayerViewId)
            {
                deadPriest = player;
                break;
            }
        }

        if (deadPriest == null)
        {
            Debug.LogWarning($"[PriestRespawnPoint.RPC_RequestRespawn] Could not find dead priest with ViewID {deadPlayerViewId}");
            return;
        }

        // Get the health system of the dead priest
        HealthSystem healthSystem = deadPriest.GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogWarning($"[PriestRespawnPoint.RPC_RequestRespawn] Dead priest has no HealthSystem component");
            return;
        }

        if (!healthSystem.IsDead)
        {
            Debug.LogWarning($"[PriestRespawnPoint.RPC_RequestRespawn] Respawn rejected because player {deadPriest.photonView.Owner?.NickName} is not dead.");
            return;
        }

        Debug.Log($"[PriestRespawnPoint.RPC_RequestRespawn] SUCCESS! Respawning priest {deadPriest.photonView.Owner?.NickName} at position {respawnPosition}");

        // Respawn the dead priest
        healthSystem.photonView.RPC(nameof(HealthSystem.RPC_RespawnPriest), RpcTarget.All, respawnPosition);

        // Clear ash pot carried state from the requesting priest after successful respawn.
        PlayerControls[] playersForCarrierLookup = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        for (int i = 0; i < playersForCarrierLookup.Length; i++)
        {
            PlayerControls player = playersForCarrierLookup[i];
            if (player == null || player.photonView == null)
            {
                continue;
            }

            if (player.photonView.OwnerActorNr == requestingActorNumber)
            {
                player.photonView.RPC(nameof(PlayerControls.RPC_SetAshPotCarriedData), RpcTarget.AllBuffered, false, -1);
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls.photonView.IsMine)
        {
            _localPlayerInRange = playerControls;
            Debug.Log($"[PriestRespawnPoint] Local player entered respawn point range");
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls.photonView.IsMine && _localPlayerInRange == playerControls)
        {
            _localPlayerInRange = null;
            Debug.Log($"[PriestRespawnPoint] Local player exited respawn point range");
            return;
        }
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (respawnPrompt != null)
        {
            respawnPrompt.SetActive(isVisible);
        }

        if (respawnPromptText != null)
        {
            respawnPromptText.text = isVisible ? "Press R to respawn" : string.Empty;
        }
    }
}
