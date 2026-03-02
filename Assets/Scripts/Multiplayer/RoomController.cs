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
        
        //Spawn first player after master client loads level
        if(PhotonNetwork.IsMasterClient)
        {
            SpawnPlayer();
        }
        
    }

    //Spawn other players on entering room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnPlayer(newPlayer);
            
            // Check if room is full and assign roles
            if (PhotonNetwork.CurrentRoom.PlayerCount >= REQUIRED_PLAYERS && !rolesAssigned)
            {
                AssignRolesToAllPlayers();
            }
        }
    }

    //Spawn character and assign to new player
    void SpawnPlayer(Player newPlayer)
    {
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        Transform spawnTf = transform;
        spawnTf.position = spawnPos;
        GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, transform.rotation);

        newCharacter.GetComponent<PhotonView>().TransferOwnership(newPlayer);
        newCharacter.GetComponent<PlayerControls>().SetCharacterMat(PhotonNetwork.CurrentRoom.PlayerCount - 1);
        newCharacter.GetComponent<PlayerControls>().playerCam = playerCam;

    }

    //Spawn initial player
    void SpawnPlayer()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        Transform spawnTf = transform;
        spawnTf.position = spawnPos;
        if (PlayerControls.localPlayerInstance == null)
        {
            GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, transform.rotation);
            newCharacter.GetComponent<PlayerControls>().SetCharacterMat(PhotonNetwork.CurrentRoom.PlayerCount - 1);
            newCharacter.GetComponent<PlayerControls>().playerCam = playerCam;

            //For testing purposes
            //SpawnFeedback();
        }
        
    }

    //Randomly assigns roles to all players in the room
    void AssignRolesToAllPlayers()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Get all players in room
        Player[] players = PhotonNetwork.CurrentRoom.Players.Values.ToArray();
        
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

        // Assign roles to players
        for (int i = 0; i < players.Length && i < roles.Count; i++)
        {
            Player player = players[i];
            PlayerRole role = roles[i];
            
            // Find the player's character in the scene and assign role
            PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
            foreach (PlayerControls pControl in playerControls)
            {
                if (pControl.photonView.Owner == player)
                {
                    pControl.photonView.RPC("AssignPlayerRole", RpcTarget.AllBuffered, (int)role);
                    print($"Assigned role {role} to player {player.NickName}");
                    break;
                }
            }
        }

        rolesAssigned = true;
        print($"Roles assigned to all {players.Length} players");
    }

    //Changes colour of connection text when initial player is spawned
    void SpawnFeedback()
    {
        ui.GetComponent<TestConnectionText>().ChangeColour();
        
    }

    
}
