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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        
        if(PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            SpawnPlayer();
        }
        
        

        /*
        for (int i = 0; i < PhotonNetwork.CurrentRoom.Players.Count; i++)
        {
            //print(PhotonNetwork.CurrentRoom.Players[i].ActorNumber);
            print(PhotonNetwork.CurrentRoom.Players[i].GetPlayerNumber());
        }
        */
        
    }

    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SpawnPlayer(newPlayer);
    }

    void SpawnPlayer(Player newPlayer)
    {
        //print("hi");
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        Transform spawnTf = transform;
        spawnTf.position = spawnPos;
        GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, transform.rotation);
        newCharacter.GetComponent<PhotonView>().TransferOwnership(newPlayer);
    }

    void SpawnPlayer()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        Transform spawnTf = transform;
        spawnTf.position = spawnPos;
        //GameObject newCharacter = Instantiate(playerCharacter, spawnTf);
        if (PlayerControls.localPlayerInstance == null)
        {
            GameObject newCharacter = PhotonNetwork.Instantiate(playerCharacter.name, spawnPos, transform.rotation);
            SpawnFeedback();
        }
        
        //newCharacter.GetComponent<PhotonView>().TransferOwnership(0);
        
    }

    void SpawnFeedback()
    {
        ui.GetComponent<TestConnectionText>().ChangeColour();
        
    }

    
}
