using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class RoomController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    GameObject playerCharacter;

    [SerializeField]
    GameObject ui;

    [SerializeField]
    Camera playerCam;

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

    //Changes colour of connection text when initial player is spawned
    void SpawnFeedback()
    {
        ui.GetComponent<TestConnectionText>().ChangeColour();
        
    }

    
}
