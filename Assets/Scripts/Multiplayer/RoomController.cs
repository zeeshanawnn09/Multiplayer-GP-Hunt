using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    GameObject playerCharacter;

    [SerializeField]
    GameObject ui;

    [SerializeField]
    Camera playerCam;

    private const int REQUIRED_PLAYERS = 4;
    private const int GHOST_COUNT = 1;
    private const int PRIEST_COUNT = 3;
    private const float ROLE_ASSIGNMENT_TIMEOUT_SECONDS = 8f;

    [Header("Spawn Settings")]
    [SerializeField]
    private Transform[] priestSpawnPoints;

    [SerializeField]
    private Transform ghostSpawnPoint;

    [SerializeField]
    private float spawnRange = 5f;

    [SerializeField]
    private float spawnRaycastStartHeight = 50f;

    [SerializeField]
    private float spawnRaycastDistance = 200f;

    [SerializeField]
    private float spawnSurfacePadding = 0.05f;

    [SerializeField]
    private LayerMask spawnGroundMask = ~0;

    private bool rolesAssigned = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        CleanupInvalidScenePlayers();
        
        // Each client spawns their own player
        SpawnLocalPlayer();
        
        // Check if we should assign roles (after a delay to ensure all players spawn and UI is ready)
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= REQUIRED_PLAYERS)
        {
            StartCoroutine(AssignRolesWhenReady());
        }
    }

    private void CleanupInvalidScenePlayers()
    {
        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        for (int i = 0; i < playerControls.Length; i++)
        {
            PlayerControls playerControl = playerControls[i];
            if (playerControl == null)
            {
                continue;
            }

            PhotonView view = playerControl.GetComponent<PhotonView>();
            if (view == null)
            {
                Debug.LogWarning($"Destroying scene PlayerControls '{playerControl.name}' because PhotonView is missing.");
                Destroy(playerControl.gameObject);
                continue;
            }

            bool hasInvalidNetworkIdentity = view.ViewID == 0 || view.Owner == null;
            if (hasInvalidNetworkIdentity)
            {
                Debug.LogWarning($"Destroying scene PlayerControls '{playerControl.name}' because it is not a valid Photon-instantiated player (ViewID={view.ViewID}, Owner={(view.Owner != null ? view.Owner.NickName : "null")}).");
                Destroy(playerControl.gameObject);
            }
        }
    }

    //Spawn other players on entering room - only for role assignment check
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"OnPlayerEnteredRoom: {newPlayer.NickName} joined. Total players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // Check if room is full and assign roles (only master does this)
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= REQUIRED_PLAYERS && !rolesAssigned)
            {
                StartCoroutine(AssignRolesWhenReady());
            }
        }
    }

    private System.Collections.IEnumerator AssignRolesWhenReady()
    {
        float elapsedTime = 0f;
        bool hasLoggedTimeout = false;

        while (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            int roomPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int spawnedPlayerCount = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None).Length;

            if (roomPlayerCount >= REQUIRED_PLAYERS && spawnedPlayerCount >= roomPlayerCount)
            {
                if (AssignRolesToAllPlayers())
                {
                    yield break;
                }
            }

            if (!hasLoggedTimeout && elapsedTime >= ROLE_ASSIGNMENT_TIMEOUT_SECONDS)
            {
                Debug.LogWarning($"Timed out waiting for player objects. Room players={roomPlayerCount}, spawned player objects={spawnedPlayerCount}. Attempting role assignment anyway.");
                hasLoggedTimeout = true;
            }

            elapsedTime += 0.25f;
            yield return new WaitForSeconds(hasLoggedTimeout ? 0.5f : 0.25f);
        }
    }

    //Spawn the local player for this client
    void SpawnLocalPlayer()
    {
        Debug.Log($"SpawnLocalPlayer called. LocalPlayer: {PhotonNetwork.LocalPlayer?.NickName}, InRoom: {PhotonNetwork.InRoom}, PlayerCount: {PhotonNetwork.CurrentRoom?.PlayerCount}");

        if (PlayerControls.localPlayerInstance != null)
        {
            PhotonView existingView = PlayerControls.localPlayerInstance.GetComponent<PhotonView>();
            bool hasValidOwnership = existingView != null
                && existingView.Owner != null
                && PhotonNetwork.LocalPlayer != null
                && existingView.OwnerActorNr == PhotonNetwork.LocalPlayer.ActorNumber;

            if (!hasValidOwnership)
            {
                Debug.LogWarning("Clearing stale localPlayerInstance before spawning the local player.");
                Destroy(PlayerControls.localPlayerInstance);
                PlayerControls.localPlayerInstance = null;
            }
        }

        if (PlayerControls.localPlayerInstance == null)
        {
            Vector3 spawnPos = GetInitialSpawnPosition();
            
            // Each client instantiates their own player - this automatically sets photonView.IsMine = true
            GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, Quaternion.identity);

            Debug.Log($"Spawned local player for {PhotonNetwork.LocalPlayer.NickName}. IsMine: {newCharacter.GetComponent<PhotonView>().IsMine}, ViewID: {newCharacter.GetComponent<PhotonView>().ViewID}");
        }
        else
        {
            Debug.Log($"Skipping spawn - localPlayerInstance already exists");
        }
    }

    private Vector3 GetInitialSpawnPosition()
    {
        List<Transform> configuredPoints = new List<Transform>();

        if (priestSpawnPoints != null)
        {
            for (int i = 0; i < priestSpawnPoints.Length; i++)
            {
                if (priestSpawnPoints[i] != null)
                {
                    configuredPoints.Add(priestSpawnPoints[i]);
                }
            }
        }

        if (ghostSpawnPoint != null)
        {
            configuredPoints.Add(ghostSpawnPoint);
        }

        if (configuredPoints.Count == 0)
        {
            return GetGroundedSpawnPosition();
        }

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 1;
        int spawnIndex = Mathf.Abs(actorNumber - 1) % configuredPoints.Count;
        return GetConfiguredSpawnPosition(configuredPoints[spawnIndex]);
    }

    private Vector3 GetConfiguredSpawnPosition(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return GetGroundedSpawnPosition();
        }

        return spawnPoint.position + Vector3.up * GetSpawnYOffset();
    }

    private Vector3 GetGroundedSpawnPosition()
    {
        Vector3 horizontalSpawn = new Vector3(
            Random.Range(-spawnRange, spawnRange),
            0f,
            Random.Range(-spawnRange, spawnRange));

        Vector3 rayOrigin = horizontalSpawn + Vector3.up * spawnRaycastStartHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnRaycastDistance, spawnGroundMask, QueryTriggerInteraction.Ignore))
        {
            float yOffset = GetSpawnYOffset();
            Vector3 groundedSpawn = hit.point + Vector3.up * yOffset;
            Debug.Log($"Grounded spawn resolved at {groundedSpawn} using hit object '{hit.collider.name}'");
            return groundedSpawn;
        }

        Vector3 fallback = new Vector3(horizontalSpawn.x, 1f, horizontalSpawn.z);
        Debug.LogWarning($"Ground raycast failed. Using fallback spawn position {fallback}");
        return fallback;
    }

    private float GetSpawnYOffset()
    {
        if (playerCharacter != null && playerCharacter.TryGetComponent<CharacterController>(out CharacterController characterController))
        {
            // Place the CharacterController bottom on the hit point.
            float bottomOffsetFromTransform = characterController.center.y - (characterController.height * 0.5f);
            float requiredYOffset = -bottomOffsetFromTransform;
            return requiredYOffset + characterController.skinWidth + spawnSurfacePadding;
        }

        return spawnSurfacePadding;
    }
    
    //Randomly assigns roles to all players in the room
    bool AssignRolesToAllPlayers()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not master client, skipping role assignment");
            return false;
        }

        if (rolesAssigned)
        {
            Debug.Log("Roles already assigned, skipping");
            return true;
        }

        // Get all players in room
        Player[] players = PhotonNetwork.CurrentRoom.Players.Values.ToArray();
        Debug.Log($"AssignRolesToAllPlayers called. Players in room: {players.Length}");
        
        // Create a list of roles: 1 Ghost + 3 Priests
        List<PlayerRole> roles = new List<PlayerRole>();
        for (int i = 0; i < PRIEST_COUNT; i++)
        {
            roles.Add(PlayerRole.Priest);
        }
        roles.Add(PlayerRole.Ghost);

        // Shuffle the roles randomly
        for (int i = roles.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            // Swap
            PlayerRole temp = roles[i];
            roles[i] = roles[randomIndex];
            roles[randomIndex] = temp;
        }

        // Find all player characters in the scene
        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        Debug.Log($"Found {playerControls.Length} PlayerControls in scene");
        
        // Log all PlayerControls and their owners
        for (int j = 0; j < playerControls.Length; j++)
        {
            var pc = playerControls[j];
            Debug.Log($"  PlayerControl {j}: Owner={pc.photonView.Owner?.NickName ?? "NULL"}, ActorNumber={pc.photonView.Owner?.ActorNumber ?? -1}, ViewID={pc.photonView.ViewID}, IsMine={pc.photonView.IsMine}");
        }

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            bool hasPlayerObject = false;

            foreach (PlayerControls pControl in playerControls)
            {
                if (pControl.photonView != null && pControl.photonView.OwnerActorNr == player.ActorNumber)
                {
                    hasPlayerObject = true;
                    break;
                }
            }

            if (!hasPlayerObject)
            {
                Debug.Log($"Waiting for PlayerControl to spawn for player {player.NickName} (ActorNumber: {player.ActorNumber}).");
                return false;
            }
        }

        // Assign roles to players
        int assignedCount = 0;
        int priestSpawnIndex = 0;
        for (int i = 0; i < players.Length && i < roles.Count; i++)
        {
            Player player = players[i];
            PlayerRole role = roles[i];
            Debug.Log($"Trying to assign {role} to player {player.NickName} (ActorNumber: {player.ActorNumber})");
            
            // Find the player's character in the scene and assign role
            bool found = false;
            foreach (PlayerControls pControl in playerControls)
            {
                if (pControl.photonView != null && pControl.photonView.OwnerActorNr == player.ActorNumber)
                {
                    pControl.photonView.RPC("AssignPlayerRole", RpcTarget.AllBuffered, (int)role);

                    Vector3 spawnPosition = GetRoleSpawnPosition(role, priestSpawnIndex);
                    if (role == PlayerRole.Priest)
                    {
                        priestSpawnIndex++;
                    }

                    pControl.photonView.RPC(nameof(PlayerControls.RPC_SetSpawnPosition), RpcTarget.AllBuffered, spawnPosition);
                    Debug.Log($"✓ SUCCESS: Assigned role {role} to player {player.NickName} (ViewID: {pControl.photonView.ViewID})");
                    assignedCount++;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.LogError($"✗ FAILED: Could not find PlayerControl for player {player.NickName} (ActorNumber: {player.ActorNumber})");
            }
        }

        rolesAssigned = assignedCount == players.Length;
        Debug.Log($"Role assignment complete: {assignedCount}/{players.Length} players assigned");

        if (!rolesAssigned)
        {
            Debug.LogWarning("Role assignment was not completed for every player. The master will be able to retry on the next join event.");
        }

        return rolesAssigned;
    }

    private Vector3 GetRoleSpawnPosition(PlayerRole role, int priestIndex)
    {
        if (role == PlayerRole.Ghost && ghostSpawnPoint != null)
        {
            return GetConfiguredSpawnPosition(ghostSpawnPoint);
        }

        if (role == PlayerRole.Priest && priestSpawnPoints != null)
        {
            List<Transform> validPriestPoints = new List<Transform>();
            for (int i = 0; i < priestSpawnPoints.Length; i++)
            {
                if (priestSpawnPoints[i] != null)
                {
                    validPriestPoints.Add(priestSpawnPoints[i]);
                }
            }

            if (validPriestPoints.Count > 0)
            {
                int index = Mathf.Abs(priestIndex) % validPriestPoints.Count;
                return GetConfiguredSpawnPosition(validPriestPoints[index]);
            }
        }

        return GetGroundedSpawnPosition();
    }

    //Changes colour of connection text when initial player is spawned
    void SpawnFeedback()
    {
        ui.GetComponent<TestConnectionText>().ChangeColour();
        
    }

    
}
