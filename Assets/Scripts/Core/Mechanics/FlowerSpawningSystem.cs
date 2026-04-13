using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class FlowerSpawningSystem : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject flowerPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private float spawnSurfacePadding = 0.05f;

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
        InitializeSharedFlowerCount();
        SpawnInitialFlowers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient != null && newMasterClient.IsLocal)
        {
            InitializeSpawnPoints();
            InitializeSharedFlowerCount();

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
            GetSpawnPosition(spawnPoints[index]),
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

        IncrementSharedFlowerCount();

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

    private Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return Vector3.zero;
        }

        if (TryGetPlatformSurface(spawnPoint, out Vector3 surfaceCenter, out float surfaceTopY))
        {
            float flowerHalfHeight = GetFlowerPrefabHalfHeight();
            return new Vector3(surfaceCenter.x, surfaceTopY + flowerHalfHeight + spawnSurfacePadding, surfaceCenter.z);
        }

        return spawnPoint.position + Vector3.up * spawnSurfacePadding;
    }

    private bool TryGetPlatformSurface(Transform spawnPoint, out Vector3 surfaceCenter, out float surfaceTopY)
    {
        surfaceCenter = spawnPoint.position;
        surfaceTopY = spawnPoint.position.y;

        Collider platformCollider = spawnPoint.GetComponent<Collider>();
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        Renderer platformRenderer = spawnPoint.GetComponentInChildren<Renderer>(true);
        if (platformRenderer != null)
        {
            Bounds bounds = platformRenderer.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        return false;
    }

    private float GetFlowerPrefabHalfHeight()
    {
        if (flowerPrefab == null)
        {
            return 0f;
        }

        Collider flowerCollider = flowerPrefab.GetComponent<Collider>();
        if (flowerCollider is BoxCollider boxCollider)
        {
            return Mathf.Abs(boxCollider.size.y * flowerPrefab.transform.lossyScale.y) * 0.5f;
        }

        if (flowerCollider is SphereCollider sphereCollider)
        {
            float scale = Mathf.Max(Mathf.Abs(flowerPrefab.transform.lossyScale.x), Mathf.Abs(flowerPrefab.transform.lossyScale.z));
            return Mathf.Abs(sphereCollider.radius * scale);
        }

        if (flowerCollider is CapsuleCollider capsuleCollider)
        {
            Vector3 scale = flowerPrefab.transform.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));

            if (capsuleCollider.direction == 0)
            {
                return Mathf.Max(Mathf.Abs(capsuleCollider.radius * radiusScale), Mathf.Abs(capsuleCollider.height * scale.x) * 0.5f);
            }

            if (capsuleCollider.direction == 2)
            {
                return Mathf.Max(Mathf.Abs(capsuleCollider.radius * radiusScale), Mathf.Abs(capsuleCollider.height * scale.z) * 0.5f);
            }

            return Mathf.Max(Mathf.Abs(capsuleCollider.radius * radiusScale), Mathf.Abs(capsuleCollider.height * scale.y) * 0.5f);
        }

        MeshFilter flowerMeshFilter = flowerPrefab.GetComponentInChildren<MeshFilter>(true);
        if (flowerMeshFilter != null && flowerMeshFilter.sharedMesh != null)
        {
            return Mathf.Abs(flowerMeshFilter.sharedMesh.bounds.size.y * flowerPrefab.transform.lossyScale.y) * 0.5f;
        }

        Renderer flowerRenderer = flowerPrefab.GetComponentInChildren<Renderer>(true);
        if (flowerRenderer != null)
        {
            return flowerRenderer.bounds.extents.y;
        }

        return 0.5f;
    }

    private void InitializeSharedFlowerCount()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PlayerControls.FlowerCountRoomPropertyKey))
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { PlayerControls.FlowerCountRoomPropertyKey, 0 }
            });
        }
    }

    private void IncrementSharedFlowerCount()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        int currentCount = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PlayerControls.FlowerCountRoomPropertyKey, out object value) && value is int storedCount)
        {
            currentCount = storedCount;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { PlayerControls.FlowerCountRoomPropertyKey, currentCount + 1 }
        });
    }
}
