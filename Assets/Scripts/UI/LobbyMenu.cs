using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UIElements;
using TMPro;
using UnityEngine.SceneManagement;


public class LobbyMenu : MonoBehaviourPunCallbacks
{
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text regionText;
    public TMPro.TMP_InputField iFNewRoom;
    public ScrollView scrollView;
    public GameObject roomPanelClass;
    public Transform scrollViewTransform;
    public GameObject playerCharacter;

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

    //Join lobby on connection to master
    public override void OnConnectedToMaster()
    {
        regionText.text = "Server region: " + PhotonNetwork.CloudRegion;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    //Updates to room list
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        roomsInfo = roomList;
        UpdateDisplayedRooms();
        //For testing, to signify an update to the room list
        //ColourStart();
    }

    //Create a new room
    public void NewRoom()
    {
        string inputName = iFNewRoom.text;
        if (inputName != "")
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                statusText.text = "Connection status: Not connected";
                return;
            }

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = (byte)4;

            PhotonNetwork.JoinOrCreateRoom(inputName, roomOptions, TypedLobby.Default);
        }
        
    }

    //Create a UI panel for each room
    private void UpdateDisplayedRooms()
    {
        print(roomsInfo.Count);
        for (int i = 0; i < roomsInfo.Count; i++)
        {
            
            if (i >= roomPanels.Count)
            {
                GameObject newPanel = Instantiate(roomPanelClass);
                newPanel.transform.SetParent(scrollViewTransform);
                newPanel.transform.localScale = Vector3.one;
                roomPanels.Add(newPanel);
            }
            

            roomPanels[i].GetComponentInChildren<TMP_Text>().text = 
                roomsInfo[i].Name + ": " + roomsInfo[i].PlayerCount + "/" + roomsInfo[i].MaxPlayers;
            roomPanels[i].GetComponent<RoomPanel>().SetRoomName(roomsInfo[i].Name);
        }
    }

    //Refresh the lobby
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

    //Text colour change for testing
    private void ColourStart()
    {
        regionText.color = Color.cyan;
        Invoke("ColourStop", 1.0f);
    }

    private void ColourStop() 
    {
        regionText.color = Color.white;
    }

    //On joining a room, load the level if master client (AutomaticallySyncScene = true)
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Level_1");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Create room failed: " + message;
        RefreshLobby();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Join room failed: " + message;
        RefreshLobby();
    }

}
