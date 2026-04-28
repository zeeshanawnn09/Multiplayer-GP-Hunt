using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class AshPotPickup : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float pickupDistance = 3f;

    public int SourcePlayerViewId { get; private set; } = -1;
    private bool _localPlayerNearby = false;

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
        if (photonView == null || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private void Update()
    {
        if (PlayerControls.localPlayerInstance == null)
        {
            if (_localPlayerNearby)
            {
                _localPlayerNearby = false;
                ClearPickupPrompt();
            }

            return;
        }

        Vector3 playerPos = PlayerControls.localPlayerInstance.transform.position;
        float distance = Vector3.Distance(playerPos, transform.position);
        bool nearby = distance <= Mathf.Max(0.1f, pickupDistance);

        if (nearby && !_localPlayerNearby)
        {
            _localPlayerNearby = true;
            ShowPickupPrompt();
        }
        else if (!nearby && _localPlayerNearby)
        {
            _localPlayerNearby = false;
            ClearPickupPrompt();
        }

        // Check for F key press to pickup
        if (nearby && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            RequestPickup();
        }
    }

    private void ShowPickupPrompt()
    {
        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText ui = TestConnectionText.TestUI.GetComponent<TestConnectionText>();
            ui?.DisplayPickupPrompt("Press F to pickup");
        }
    }

    private void ClearPickupPrompt()
    {
        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText ui = TestConnectionText.TestUI.GetComponent<TestConnectionText>();
            ui?.ClearPickupPrompt();
        }
    }

    [PunRPC]
    private void RPC_SetSourcePlayerViewId(int sourcePlayerViewId)
    {
        SourcePlayerViewId = sourcePlayerViewId;
    }

    [PunRPC]
    private void RPC_RequestPickup(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!TryGetRequestingPriest(requestingActorNumber, out PlayerControls requestingPlayer))
        {
            return;
        }

        if (requestingPlayer.HasAshPot)
        {
            return;
        }

        float distance = Vector3.Distance(requestingPlayer.transform.position, transform.position);
        if (distance > pickupDistance)
        {
            return;
        }

        requestingPlayer.photonView.RPC(nameof(PlayerControls.RPC_SetAshPotCarriedData), RpcTarget.AllBuffered, true, SourcePlayerViewId);
        
        // Disable visibility immediately before destroying
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
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
