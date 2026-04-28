using Photon.Pun;
using UnityEngine;

public class PriestRespawnTrigger : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Transform respawnPoint;

    [SerializeField]
    private string respawnPointTag = "PriestRespawnPoint";

    [SerializeField]
    private float respawnSurfacePadding = 0.1f;

    [SerializeField]
    private bool debugLogs = true;

    private void Awake()
    {
        ResolveRespawnPoint();
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if the colliding object is a player
        PlayerControls priestPlayer = other.GetComponentInParent<PlayerControls>();
        if (priestPlayer == null || !priestPlayer.IsPriest)
        {
            return;
        }

        // Check if this priest is carrying an ash pot
        if (!priestPlayer.HasAshPot)
        {
            return;
        }

        // Notify the master to respawn the dead priest
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RequestPriestRespawn), RpcTarget.MasterClient, priestPlayer.photonView.OwnerActorNr, priestPlayer.CarriedAshPotSourcePlayerViewId);
        }
        else
        {
            HandlePriestRespawn(priestPlayer, priestPlayer.CarriedAshPotSourcePlayerViewId);
        }
    }

    [PunRPC]
    private void RPC_RequestPriestRespawn(int carryingPriestActorNumber, int deadPlayerViewId)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // Find the dead priest by view ID and respawn them
        PlayerControls deadPriest = FindPlayerByViewId(deadPlayerViewId);
        if (deadPriest == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[PriestRespawnTrigger] Could not find dead priest with ViewID {deadPlayerViewId}");
            }

            return;
        }

        HealthSystem deadHealthSystem = deadPriest.GetComponent<HealthSystem>();
        if (deadHealthSystem == null || !deadHealthSystem.IsDead)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[PriestRespawnTrigger] Player {deadPriest.photonView.Owner?.NickName} is not dead or missing HealthSystem, ignoring respawn request");
            }

            return;
        }

        // Respawn the dead priest
        HandlePriestRespawn(deadPriest, deadPlayerViewId);

        // Tell the carrying priest to drop the ash pot (consume it)
        PlayerControls carryingPriest = FindPlayerByActorNumber(carryingPriestActorNumber);
        if (carryingPriest != null)
        {
            carryingPriest.photonView.RPC(nameof(PlayerControls.RPC_SetAshPotCarriedData), RpcTarget.AllBuffered, false, -1);
        }

        if (debugLogs)
        {
            Debug.Log($"[PriestRespawnTrigger] Respawned priest {deadPriest.photonView.Owner?.NickName} at respawn point via ash pot from {carryingPriest?.photonView.Owner?.NickName ?? "unknown"}");
        }
    }

    private void HandlePriestRespawn(PlayerControls deadPriest, int deadPlayerViewId)
    {
        if (deadPriest == null || deadPriest.photonView == null)
        {
            return;
        }

        // Get respawn position
        Vector3 respawnPos = GetRespawnPosition();

        // Use the priest's health system to respawn (handles all RPCs and state updates including teleport)
        HealthSystem healthSystem = deadPriest.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.RespawnPriest(respawnPos);
        }
    }

    private Vector3 GetRespawnPosition()
    {
        ResolveRespawnPoint();

        if (respawnPoint == null)
        {
            return transform.position;
        }

        if (TryGetPlatformSurface(respawnPoint, out Vector3 surfaceCenter, out float surfaceTopY))
        {
            return new Vector3(surfaceCenter.x, surfaceTopY + respawnSurfacePadding, surfaceCenter.z);
        }

        return respawnPoint.position + Vector3.up * respawnSurfacePadding;
    }

    private void ResolveRespawnPoint()
    {
        if (respawnPoint != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(respawnPointTag))
        {
            return;
        }

        GameObject point = GameObject.FindWithTag(respawnPointTag);
        if (point != null)
        {
            respawnPoint = point.transform;
        }
    }

    private bool TryGetPlatformSurface(Transform platformTransform, out Vector3 surfaceCenter, out float surfaceTopY)
    {
        surfaceCenter = platformTransform.position;
        surfaceTopY = platformTransform.position.y;

        Collider platformCollider = platformTransform.GetComponent<Collider>();
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        Renderer platformRenderer = platformTransform.GetComponentInChildren<Renderer>(true);
        if (platformRenderer != null)
        {
            Bounds bounds = platformRenderer.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        return false;
    }

    private PlayerControls FindPlayerByViewId(int viewId)
    {
        PlayerControls[] allPlayers = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        foreach (PlayerControls player in allPlayers)
        {
            if (player != null && player.photonView != null && player.photonView.ViewID == viewId)
            {
                return player;
            }
        }

        return null;
    }

    private PlayerControls FindPlayerByActorNumber(int actorNumber)
    {
        PlayerControls[] allPlayers = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        foreach (PlayerControls player in allPlayers)
        {
            if (player != null && player.photonView != null && player.photonView.OwnerActorNr == actorNumber)
            {
                return player;
            }
        }

        return null;
    }
}
