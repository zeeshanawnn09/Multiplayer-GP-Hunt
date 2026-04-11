using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class FlowerSpawningSystem : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private FlowerPickup flowerPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private int maxFlowersPerSpawnPoint = 5;

    [SerializeField]
    private float minRespawnDelay = 5f;

    [SerializeField]
    private float maxRespawnDelay = 10f;

    private int[] spawnedFlowerCounts;
    private FlowerPickup[] activeFlowers;

    private void Start()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        InitializeSpawnPoints();
        SpawnInitialFlowers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient != null && newMasterClient.IsLocal)
        {
            InitializeSpawnPoints();

            if (spawnedFlowerCounts == null || activeFlowers == null)
            {
                SpawnInitialFlowers();
            }
        }
    }

    private void InitializeSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            EnsureRuntimeArrays();
            return;
        }

        int childCount = transform.childCount;
        if (childCount == 0)
        {
            Debug.LogWarning("FlowerSpawningSystem has no spawn points assigned or children to use as spawn points.");
            spawnPoints = new Transform[0];
            spawnedFlowerCounts = new int[0];
            activeFlowers = new FlowerPickup[0];
            return;
        }

        spawnPoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            spawnPoints[i] = transform.GetChild(i);
        }

        EnsureRuntimeArrays();
    }

    private void EnsureRuntimeArrays()
    {
        if (spawnPoints == null)
        {
            spawnPoints = new Transform[0];
        }

        if (spawnedFlowerCounts == null || spawnedFlowerCounts.Length != spawnPoints.Length)
        {
            spawnedFlowerCounts = new int[spawnPoints.Length];
        }

        if (activeFlowers == null || activeFlowers.Length != spawnPoints.Length)
        {
            activeFlowers = new FlowerPickup[spawnPoints.Length];
        }
    }

    private void SpawnInitialFlowers()
    {
        if (!PhotonNetwork.IsMasterClient || flowerPrefab == null || spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnFlowerAtIndex(i);
        }
    }

    private void SpawnFlowerAtIndex(int index)
    {
        if (!PhotonNetwork.IsMasterClient || flowerPrefab == null)
        {
            return;
        }

        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length || spawnPoints[index] == null)
        {
            return;
        }

        if (spawnedFlowerCounts[index] >= maxFlowersPerSpawnPoint)
        {
            return;
        }

        if (activeFlowers[index] != null)
        {
            return;
        }

        GameObject spawnedFlowerObject = PhotonNetwork.InstantiateRoomObject(
            flowerPrefab.name,
            spawnPoints[index].position,
            spawnPoints[index].rotation);

        FlowerPickup flowerPickup = spawnedFlowerObject.GetComponent<FlowerPickup>();
        if (flowerPickup != null)
        {
            flowerPickup.Initialize(this, index);
            activeFlowers[index] = flowerPickup;
            spawnedFlowerCounts[index]++;
        }
        else
        {
            Debug.LogWarning($"Spawned flower prefab '{flowerPrefab.name}' does not have a FlowerPickup component.");
        }
    }

    public void RequestCollectFlower(FlowerPickup flowerPickup, int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || flowerPickup == null)
        {
            return;
        }

        if (!IsPickupValid(flowerPickup, requestingActorNumber, out PlayerControls requestingPlayer))
        {
            return;
        }

        int spawnPointIndex = flowerPickup.SpawnPointIndex;
        if (spawnPointIndex < 0 || spawnPointIndex >= activeFlowers.Length)
        {
            return;
        }

        if (activeFlowers[spawnPointIndex] != flowerPickup)
        {
            return;
        }

        activeFlowers[spawnPointIndex] = null;
        flowerPickup.MarkCollected();

        if (requestingPlayer != null && requestingPlayer.photonView != null)
        {
            requestingPlayer.photonView.RPC("RPC_RequestCollectFlower", RpcTarget.All, 1);
        }

        PhotonNetwork.Destroy(flowerPickup.gameObject);

        if (spawnedFlowerCounts[spawnPointIndex] < maxFlowersPerSpawnPoint)
        {
            StartCoroutine(RespawnFlowerAfterDelay(spawnPointIndex));
        }
    }

    private bool IsPickupValid(FlowerPickup flowerPickup, int requestingActorNumber, out PlayerControls requestingPlayer)
    {
        requestingPlayer = null;

        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        foreach (PlayerControls playerControl in playerControls)
        {
            if (playerControl == null || playerControl.photonView == null)
            {
                continue;
            }

            if (playerControl.photonView.OwnerActorNr != requestingActorNumber)
            {
                continue;
            }

            requestingPlayer = playerControl;
            break;
        }

        if (requestingPlayer == null || !requestingPlayer.IsPriest)
        {
            return false;
        }

        float distance = Vector3.Distance(requestingPlayer.transform.position, flowerPickup.transform.position);
        return distance <= flowerPickup.PickupDistance;
    }

    private IEnumerator RespawnFlowerAfterDelay(int spawnPointIndex)
    {
        float respawnDelay = Random.Range(minRespawnDelay, maxRespawnDelay);
        yield return new WaitForSeconds(respawnDelay);

        if (!PhotonNetwork.IsMasterClient)
        {
            yield break;
        }

        if (spawnPointIndex < 0 || spawnPointIndex >= activeFlowers.Length)
        {
            yield break;
        }

        if (spawnedFlowerCounts[spawnPointIndex] >= maxFlowersPerSpawnPoint)
        {
            yield break;
        }

        SpawnFlowerAtIndex(spawnPointIndex);
    }
}
