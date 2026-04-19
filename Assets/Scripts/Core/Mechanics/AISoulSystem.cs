using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class AISoulSystem : MonoBehaviourPunCallbacks
{
    public static AISoulSystem Instance { get; private set; }

    [SerializeField]
    private GameObject soulPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    [SerializeField]
    private float spawnSurfacePadding = 0.05f;

    [SerializeField]
    private int maxSoulsPerSpawnPoint = 15;

    [SerializeField]
    private float respawnDelaySeconds = 60f;

    [SerializeField]
    private bool debugLogs = true;

    private int[] spawnedSoulCounts;
    private SoulPickup[] activeSouls;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        InitializeSpawnPoints();
        SpawnInitialSouls();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient != null && newMasterClient.IsLocal)
        {
            InitializeSpawnPoints();

            if (spawnedSoulCounts == null || activeSouls == null)
            {
                SpawnInitialSouls();
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
            Debug.LogWarning("AISoulSystem has no spawn points assigned or children to use as spawn points.");
            spawnPoints = new Transform[0];
            spawnedSoulCounts = new int[0];
            activeSouls = new SoulPickup[0];
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

        if (spawnedSoulCounts == null || spawnedSoulCounts.Length != spawnPoints.Length)
        {
            spawnedSoulCounts = new int[spawnPoints.Length];
        }

        if (activeSouls == null || activeSouls.Length != spawnPoints.Length)
        {
            activeSouls = new SoulPickup[spawnPoints.Length];
        }
    }

    private void SpawnInitialSouls()
    {
        if (!PhotonNetwork.IsMasterClient || soulPrefab == null || spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnSoulAtIndex(i);
        }
    }

    private void SpawnSoulAtIndex(int index)
    {
        if (!PhotonNetwork.IsMasterClient || soulPrefab == null)
        {
            return;
        }

        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length || spawnPoints[index] == null)
        {
            return;
        }

        if (spawnedSoulCounts[index] >= maxSoulsPerSpawnPoint)
        {
            return;
        }

        if (activeSouls[index] != null)
        {
            return;
        }

        GameObject spawnedSoulObject = PhotonNetwork.InstantiateRoomObject(
            soulPrefab.name,
            GetSpawnPosition(spawnPoints[index]),
            spawnPoints[index].rotation);

        SoulPickup soulPickup = spawnedSoulObject.GetComponent<SoulPickup>();
        if (soulPickup != null)
        {
            soulPickup.Initialize(this, index);
            SoulAI soulAI = spawnedSoulObject.GetComponent<SoulAI>();
            if (soulAI != null)
            {
                soulAI.InitializeSpawned(this, soulPickup);
            }

            activeSouls[index] = soulPickup;
            spawnedSoulCounts[index]++;
        }
        else
        {
            Debug.LogWarning($"Spawned soul prefab '{soulPrefab.name}' does not have a SoulPickup component.");
        }
    }

    public void RequestCollectSoul(SoulPickup soulPickup, int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || soulPickup == null)
        {
            return;
        }

        if (!IsPickupValid(soulPickup, requestingActorNumber, out PlayerControls requestingPlayer))
        {
            return;
        }

        int spawnPointIndex = soulPickup.SpawnPointIndex;
        if (spawnPointIndex < 0 || spawnPointIndex >= activeSouls.Length)
        {
            return;
        }

        if (activeSouls[spawnPointIndex] != soulPickup)
        {
            return;
        }

        activeSouls[spawnPointIndex] = null;
        soulPickup.MarkCollected();

        int nextSoulCount = Mathf.Min(requestingPlayer.SoulCount + 1, PlayerControls.MaxSoulCount);
        requestingPlayer.photonView.RPC(nameof(PlayerControls.RPC_SetSoulCount), RpcTarget.AllBuffered, nextSoulCount);

        PhotonNetwork.Destroy(soulPickup.gameObject);

        if (spawnedSoulCounts[spawnPointIndex] < maxSoulsPerSpawnPoint)
        {
            StartCoroutine(RespawnSoulAfterDelay(spawnPointIndex));
        }
    }

    public bool TrySpawnDroppedSoul(Vector3 worldPosition, int ownerActorNumber, out int spawnedSoulViewId)
    {
        spawnedSoulViewId = -1;

        if (!PhotonNetwork.IsMasterClient || soulPrefab == null)
        {
            return false;
        }

        GameObject droppedSoulObject = PhotonNetwork.InstantiateRoomObject(
            soulPrefab.name,
            GetDroppedSoulSpawnPosition(worldPosition),
            Quaternion.identity);

        if (debugLogs)
        {
            Debug.Log($"[AISoulSystem] Dropping soul for actor {ownerActorNumber} at {worldPosition}. Spawn position: {droppedSoulObject.transform.position}");
        }

        SoulPickup droppedSoulPickup = droppedSoulObject.GetComponent<SoulPickup>();
        if (droppedSoulPickup == null)
        {
            Debug.LogWarning($"Dropped soul prefab '{soulPrefab.name}' does not have a SoulPickup component.");
            return false;
        }

        droppedSoulPickup.InitializeDropped(this, ownerActorNumber);
        SoulAI droppedSoulAI = droppedSoulObject.GetComponent<SoulAI>();
        if (droppedSoulAI != null)
        {
            droppedSoulAI.ActivateDroppedSoul(this, droppedSoulPickup);
        }

        PhotonView droppedSoulView = droppedSoulObject.GetComponent<PhotonView>();
        if (droppedSoulView != null)
        {
            spawnedSoulViewId = droppedSoulView.ViewID;
        }

        droppedSoulPickup.DisablePickupForever();
        return true;
    }

    private Vector3 GetDroppedSoulSpawnPosition(Vector3 groundPosition)
    {
        float soulHalfHeight = GetSoulPrefabHalfHeight();
        return groundPosition + Vector3.up * (soulHalfHeight + spawnSurfacePadding);
    }

    public void NotifySoulDestroyed(SoulPickup soulPickup)
    {
        if (!PhotonNetwork.IsMasterClient || soulPickup == null)
        {
            return;
        }

        int spawnPointIndex = soulPickup.SpawnPointIndex;
        if (spawnPointIndex >= 0 && activeSouls != null && spawnPointIndex < activeSouls.Length && activeSouls[spawnPointIndex] == soulPickup)
        {
            activeSouls[spawnPointIndex] = null;

            PhotonNetwork.Destroy(soulPickup.gameObject);

            if (spawnedSoulCounts[spawnPointIndex] < maxSoulsPerSpawnPoint)
            {
                StartCoroutine(RespawnSoulAfterDelay(spawnPointIndex));
            }
            return;
        }

        if (soulPickup.DroppedOwnerActorNumber > 0)
        {
            ClearDroppedSoulStateForOwner(soulPickup.DroppedOwnerActorNumber, soulPickup);
        }

        PhotonNetwork.Destroy(soulPickup.gameObject);
    }

    private void ClearDroppedSoulStateForOwner(int ownerActorNumber, SoulPickup soulPickup)
    {
        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        for (int i = 0; i < playerControls.Length; i++)
        {
            PlayerControls playerControl = playerControls[i];
            if (playerControl == null || playerControl.photonView == null)
            {
                continue;
            }

            if (playerControl.photonView.OwnerActorNr != ownerActorNumber)
            {
                continue;
            }

            playerControl.RPC_SetDroppedSoulState(false, -1);
            playerControl.photonView.RPC(nameof(PlayerControls.RPC_SetDroppedSoulState), RpcTarget.AllBuffered, false, -1);
            break;
        }
    }

    private bool IsPickupValid(SoulPickup soulPickup, int requestingActorNumber, out PlayerControls requestingPlayer)
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

        if (requestingPlayer == null || !requestingPlayer.IsGhost)
        {
            return false;
        }

        if (requestingPlayer.SoulCount >= PlayerControls.MaxSoulCount)
        {
            return false;
        }

        float distance = Vector3.Distance(requestingPlayer.transform.position, soulPickup.transform.position);
        return distance <= soulPickup.PickupDistance;
    }

    private IEnumerator RespawnSoulAfterDelay(int spawnPointIndex)
    {
        yield return new WaitForSeconds(respawnDelaySeconds);

        if (!PhotonNetwork.IsMasterClient)
        {
            yield break;
        }

        if (spawnPointIndex < 0 || spawnPointIndex >= activeSouls.Length)
        {
            yield break;
        }

        if (spawnedSoulCounts[spawnPointIndex] >= maxSoulsPerSpawnPoint)
        {
            yield break;
        }

        SpawnSoulAtIndex(spawnPointIndex);
    }

    private Vector3 GetSpawnPosition(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return Vector3.zero;
        }

        if (TryGetPlatformSurface(spawnPoint, out Vector3 surfaceCenter, out float surfaceTopY))
        {
            float soulHalfHeight = GetSoulPrefabHalfHeight();
            return new Vector3(surfaceCenter.x, surfaceTopY + soulHalfHeight + spawnSurfacePadding, surfaceCenter.z);
        }

        return spawnPoint.position + Vector3.up * spawnSurfacePadding;
    }

    private float GetSoulPrefabHalfHeight()
    {
        if (soulPrefab == null)
        {
            return 0f;
        }

        Renderer soulRenderer = soulPrefab.GetComponentInChildren<Renderer>(true);
        if (soulRenderer != null)
        {
            return soulRenderer.bounds.extents.y;
        }

        Collider soulCollider = soulPrefab.GetComponent<Collider>();
        if (soulCollider != null)
        {
            return soulCollider.bounds.extents.y;
        }

        return 0.25f;
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

}
