using UnityEngine;
using Photon.Pun;

public class RoomPanel : MonoBehaviourPunCallbacks
{
    private string roomName;

    public void SetRoomName(string name)
    {
        roomName = name;
    }

    public void Join()
    {
        PhotonNetwork.JoinRoom(roomName);
    }
}
