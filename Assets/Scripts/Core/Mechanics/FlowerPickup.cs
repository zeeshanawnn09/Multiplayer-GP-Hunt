using Photon.Pun;
using UnityEngine;

public class FlowerPickup : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private float pickupDistance = 3f;

    private FlowerSpawningSystem spawnSystem;

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
}