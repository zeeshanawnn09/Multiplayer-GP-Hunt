using Photon.Pun;
using UnityEngine;

public class SoulPickup : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float pickupDistance = 3f;

    private AISoulSystem spawnSystem;
    private bool isPickupEnabled = true;
    private bool _localPlayerNearby = false;

    public int SpawnPointIndex { get; private set; } = -1;
    public int DroppedOwnerActorNumber { get; private set; } = -1;

    public float PickupDistance => pickupDistance;
    public bool IsPickupEnabled => isPickupEnabled;

    public void Initialize(AISoulSystem owningSpawnSystem, int spawnIndex)
    {
        spawnSystem = owningSpawnSystem;
        SpawnPointIndex = spawnIndex;
        DroppedOwnerActorNumber = -1;
    }

    public void InitializeDropped(AISoulSystem owningSpawnSystem, int ownerActorNumber)
    {
        spawnSystem = owningSpawnSystem;
        SpawnPointIndex = -1;
        DroppedOwnerActorNumber = ownerActorNumber;
    }

    public void RequestPickup()
    {
        if (!isPickupEnabled || photonView == null || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RequestCollectSoul), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private void Update()
    {
        if (PlayerControls.localPlayerInstance == null)
        {
            return;
        }

        Vector3 playerPos = PlayerControls.localPlayerInstance.transform.position;
        float distance = Vector3.Distance(playerPos, GetPickupWorldPosition());
        bool nearby = distance <= Mathf.Max(0.1f, pickupDistance);

        if (nearby && !_localPlayerNearby)
        {
            _localPlayerNearby = true;
            // Auto-pickup when nearby
            RequestPickup();
        }
        else if (!nearby && _localPlayerNearby)
        {
            _localPlayerNearby = false;
        }
    }

    [PunRPC]
    private void RPC_RequestCollectSoul(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || spawnSystem == null || !isPickupEnabled)
        {
            return;
        }

        spawnSystem.RequestCollectSoul(this, requestingActorNumber);
    }

    public void DisablePickupForever()
    {
        if (photonView == null)
        {
            return;
        }

        photonView.RPC(nameof(RPC_SetPickupEnabled), RpcTarget.AllBuffered, false);
    }

    [PunRPC]
    private void RPC_SetPickupEnabled(bool enabled)
    {
        isPickupEnabled = enabled;
    }

    public void MarkCollected()
    {
        isPickupEnabled = false;

        Collider soulCollider = GetComponent<Collider>();
        if (soulCollider != null)
        {
            soulCollider.enabled = false;
        }

        Renderer[] soulRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer soulRenderer in soulRenderers)
        {
            soulRenderer.enabled = false;
        }
    }

    public Vector3 GetPickupWorldPosition()
    {
        Collider soulCollider = GetComponent<Collider>();
        if (soulCollider != null)
        {
            return soulCollider.bounds.center;
        }

        Renderer soulRenderer = GetComponentInChildren<Renderer>(true);
        if (soulRenderer != null)
        {
            return soulRenderer.bounds.center;
        }

        return transform.position;
    }
}