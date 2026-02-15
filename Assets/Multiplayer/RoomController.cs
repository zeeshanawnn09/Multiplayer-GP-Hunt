using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    GameObject playerCharacter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        SpawnPlayer();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        //print("hi");
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        Transform spawnTf = transform;
        spawnTf.position = spawnPos;
        Instantiate(playerCharacter, spawnTf);
    }
}
