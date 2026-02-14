using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UIElements;
using TMPro;


public class LobbyMenu : MonoBehaviourPunCallbacks
{
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text regionText;
    public TMPro.TMP_InputField iFNewRoom;
    public ScrollView scrollView;
    public GameObject roomPanelClass;
    public Transform scrollViewTransform;

    private string gameVer = "0.0";
    private List<RoomInfo>roomsInfo = new List<RoomInfo>();
    private List<GameObject>roomPanels = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = gameVer;

            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // Update is called once per frame
    void Update()
    {
        statusText.text = "Connection status: " + PhotonNetwork.NetworkClientState;
    }

    public override void OnConnectedToMaster()
    {
        regionText.text = "Server region: " + PhotonNetwork.CloudRegion;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //print(roomsInfo.Count);
        roomsInfo = roomList;
        //print(roomsInfo.Count);
        UpdateDisplayedRooms();
        ColourStart();
    }

    public void NewRoom()
    {
        string inputName = iFNewRoom.text;
        if(inputName != "")
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = (byte)4;

            PhotonNetwork.JoinOrCreateRoom(inputName, roomOptions, TypedLobby.Default);
            //UpdateDisplayedRooms();
            //RemoteRefresh();
            if (PhotonNetwork.NetworkClientState != ClientState.Joined
            && PhotonNetwork.NetworkClientState != ClientState.Joining)
            {
                RefreshLobby();
            }
        }
        
    }

    private void UpdateDisplayedRooms()
    {
        //print("UpdateRooms");
        print(roomsInfo.Count);
        for (int i = 0; i < roomsInfo.Count; i++)
        {
            //print("RoomInfo");
            
            if (i >= roomPanels.Count)
            {
                GameObject newPanel = Instantiate(roomPanelClass);
                newPanel.transform.SetParent(scrollViewTransform);
                newPanel.transform.localScale = Vector3.one;
                roomPanels.Add(newPanel);
            }
            

            roomPanels[i].GetComponentInChildren<TMP_Text>().text = roomsInfo[i].Name + ": " + roomsInfo[i].PlayerCount + "/" + roomsInfo[i].MaxPlayers;
        }
    }

    public void RefreshLobby()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby(TypedLobby.Default);
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    [PunRPC]
    private void RemoteRefresh()
    {
        statusText.color = Color.green;
        if (PhotonNetwork.NetworkClientState != ClientState.Joined 
            && PhotonNetwork.NetworkClientState != ClientState.Joining)
        {
            RefreshLobby();
        }
        if (photonView.IsMine)
        {
            photonView.RPC("RemoteRefresh", RpcTarget.OthersBuffered);
            
        }
    }

    private void ColourStart()
    {
        regionText.color = Color.cyan;
        Invoke("ColourStop", 1.0f);
    }

    private void ColourStop() 
    {
        regionText.color = Color.white;
    }
}
