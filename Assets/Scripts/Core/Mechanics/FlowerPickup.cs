using Photon.Pun;
using UnityEngine;

public class FlowerPickup : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float pickupDistance = 3f;

    private FlowerSpawningSystem spawnSystem;
    private bool _localPlayerNearby;

    public int SpawnPointIndex { get; private set; } = -1;

    public float PickupDistance => pickupDistance;

    public void Initialize(FlowerSpawningSystem owningSpawnSystem, int spawnIndex)
    {
        spawnSystem = owningSpawnSystem;
        SpawnPointIndex = spawnIndex;
    }

    public void RequestPickup()
    {
        if (photonView == null || PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        photonView.RPC("RPC_RequestCollectFlower", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
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
    private void RPC_RequestCollectFlower(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || spawnSystem == null)
        {
            return;
        }

        spawnSystem.RequestCollectFlower(this, requestingActorNumber);
    }

    public void MarkCollected()
    {
        Collider flowerCollider = GetComponent<Collider>();
        if (flowerCollider != null)
        {
            flowerCollider.enabled = false;
        }

        Renderer[] flowerRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer flowerRenderer in flowerRenderers)
        {
            flowerRenderer.enabled = false;
        }
    }

    public Vector3 GetPickupWorldPosition()
    {
        Collider flowerCollider = GetComponent<Collider>();
        if (flowerCollider != null)
        {
            return flowerCollider.bounds.center;
        }

        Renderer flowerRenderer = GetComponentInChildren<Renderer>(true);
        if (flowerRenderer != null)
        {
            return flowerRenderer.bounds.center;
        }

        return transform.position;
    }
}