using Photon.Pun;
using UnityEngine;

public class AshPotPickup : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float pickupDistance = 3f;

    public int SourcePlayerViewId { get; private set; } = -1;

    public void Initialize(int sourcePlayerViewId)
    {
        if (photonView == null)
        {
            return;
        }

        photonView.RPC(nameof(RPC_SetSourcePlayerViewId), RpcTarget.AllBuffered, sourcePlayerViewId);
    }

    public void RequestPickup()
    {
        Debug.Log($"[AshPotPickup.RequestPickup] Called. PhotonView: {(photonView != null ? "OK" : "NULL")}, LocalPlayer: {(PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.NickName : "NULL")}");
        
        if (photonView == null || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("[AshPotPickup.RequestPickup] PhotonView or LocalPlayer is null, aborting pickup request");
            return;
        }

        Debug.Log($"[AshPotPickup.RequestPickup] Sending RPC to master client. ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RPC_SetSourcePlayerViewId(int sourcePlayerViewId)
    {
        SourcePlayerViewId = sourcePlayerViewId;
    }

    [PunRPC]
    private void RPC_RequestPickup(int requestingActorNumber)
    {
        Debug.Log($"[AshPotPickup.RPC_RequestPickup] Called. IsMasterClient: {PhotonNetwork.IsMasterClient}, ActorNumber: {requestingActorNumber}");
        
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[AshPotPickup.RPC_RequestPickup] Not master client, ignoring");
            return;
        }

        if (!TryGetRequestingPriest(requestingActorNumber, out PlayerControls requestingPlayer))
        {
            Debug.LogWarning($"[AshPotPickup.RPC_RequestPickup] Could not find requesting priest with ActorNumber {requestingActorNumber}");
            return;
        }

        Debug.Log($"[AshPotPickup.RPC_RequestPickup] Found priest: {requestingPlayer.photonView.Owner?.NickName}. HasAshPot: {requestingPlayer.HasAshPot}");

        if (requestingPlayer.HasAshPot)
        {
            Debug.LogWarning($"[AshPotPickup.RPC_RequestPickup] Priest already has ash pot, rejecting");
            return;
        }

        float distance = Vector3.Distance(requestingPlayer.transform.position, transform.position);
        Debug.Log($"[AshPotPickup.RPC_RequestPickup] Distance: {distance:F2}m, PickupDistance: {pickupDistance}m");
        if (distance > pickupDistance)
        {
            Debug.LogWarning($"[AshPotPickup.RPC_RequestPickup] Priest too far away (distance: {distance:F2}m > {pickupDistance}m)");
            return;
        }

        Debug.Log($"[AshPotPickup.RPC_RequestPickup] SUCCESS! Setting ash pot carried state and destroying object");
        requestingPlayer.photonView.RPC(nameof(PlayerControls.RPC_SetAshPotCarriedData), RpcTarget.AllBuffered, true, SourcePlayerViewId);
        
        // Disable visibility immediately before destroying
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
            Debug.Log("[AshPotPickup.RPC_RequestPickup] Disabled renderer");
        }
        
        PhotonNetwork.Destroy(gameObject);
    }

    private bool TryGetRequestingPriest(int requestingActorNumber, out PlayerControls requestingPlayer)
    {
        requestingPlayer = null;

        PlayerControls[] allPlayers = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        Debug.Log($"[TryGetRequestingPriest] Looking for ActorNumber {requestingActorNumber} among {allPlayers.Length} players");
        
        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerControls candidate = allPlayers[i];
            if (candidate == null || candidate.photonView == null)
            {
                continue;
            }

            Debug.Log($"  Player {i}: ActorNumber={candidate.photonView.OwnerActorNr}, IsPriest={candidate.IsPriest}, HasAssignedRole={candidate.HasAssignedRole}");

            if (candidate.photonView.OwnerActorNr != requestingActorNumber)
            {
                continue;
            }

            if (!candidate.IsPriest)
            {
                Debug.LogWarning($"[TryGetRequestingPriest] Found player but is not a priest (IsPriest={candidate.IsPriest}, HasAssignedRole={candidate.HasAssignedRole})");
                return false;
            }

            requestingPlayer = candidate;
            Debug.Log($"[TryGetRequestingPriest] SUCCESS! Found priest: {candidate.photonView.Owner?.NickName}");
            return true;
        }

        Debug.LogWarning($"[TryGetRequestingPriest] No player found with ActorNumber {requestingActorNumber}");
        return false;
    }
}
