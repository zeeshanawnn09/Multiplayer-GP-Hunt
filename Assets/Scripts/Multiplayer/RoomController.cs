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

    private bool rolesAssigned = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // Each client spawns their own player
        SpawnLocalPlayer();
        
        // Check if we should assign roles (after a delay to ensure all players spawn and UI is ready)
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= REQUIRED_PLAYERS)
        {
            Invoke("AssignRolesToAllPlayers", 2f); // Increased delay to 2 seconds
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
                // Add a small delay to ensure the new player's GameObject exists
                Invoke("AssignRolesToAllPlayers", 0.5f);
            }
        }
    }

    //Spawn the local player for this client
    void SpawnLocalPlayer()
    {
        Debug.Log($"SpawnLocalPlayer called. LocalPlayer: {PhotonNetwork.LocalPlayer?.NickName}, InRoom: {PhotonNetwork.InRoom}, PlayerCount: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        
        if (PlayerControls.localPlayerInstance == null)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
            
            // Each client instantiates their own player - this automatically sets photonView.IsMine = true
            GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, Quaternion.identity);
            
            // Get player's position in room (1-based, so subtract 1 for 0-based index)
            int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer);
            
            PlayerControls pc = newCharacter.GetComponent<PlayerControls>();
            if (pc != null)
            {
                pc.SetCharacterMat(playerIndex);
            }

            Debug.Log($"Spawned local player for {PhotonNetwork.LocalPlayer.NickName}. IsMine: {newCharacter.GetComponent<PhotonView>().IsMine}, Index: {playerIndex}, ViewID: {newCharacter.GetComponent<PhotonView>().ViewID}");
        }
        else
        {
            Debug.Log($"Skipping spawn - localPlayerInstance already exists");
        }
    }
    
    // Get the index of a player in the room (0-based)
    private int GetPlayerIndex(Player player)
    {
        Player[] players = PhotonNetwork.PlayerList;
        Debug.Log($"GetPlayerIndex for {player.NickName}. Total players: {players.Length}");
        
        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log($"  Player {i}: {players[i].NickName} (ActorID: {players[i].ActorNumber})");
            if (players[i] == player)
            {
                Debug.Log($"  -> Found at index {i}");
                return i;
            }
        }
        Debug.LogWarning($"Player {player.NickName} not found in player list! Returning 0");
        return 0;
    }

    //Randomly assigns roles to all players in the room
    void AssignRolesToAllPlayers()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not master client, skipping role assignment");
            return;
        }

        if (rolesAssigned)
        {
            Debug.Log("Roles already assigned, skipping");
            return;
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

        // Assign roles to players
        int assignedCount = 0;
        for (int i = 0; i < players.Length && i < roles.Count; i++)
        {
            Player player = players[i];
            PlayerRole role = roles[i];
            Debug.Log($"Trying to assign {role} to player {player.NickName} (ActorNumber: {player.ActorNumber})");
            
            // Find the player's character in the scene and assign role
            bool found = false;
            foreach (PlayerControls pControl in playerControls)
            {
                if (pControl.photonView.Owner == player)
                {
                    pControl.photonView.RPC("AssignPlayerRole", RpcTarget.AllBuffered, (int)role);
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

        rolesAssigned = true;
        Debug.Log($"Role assignment complete: {assignedCount}/{players.Length} players assigned");
    }

    //Changes colour of connection text when initial player is spawned
    void SpawnFeedback()
    {
        ui.GetComponent<TestConnectionText>().ChangeColour();
        
    }

    
}
