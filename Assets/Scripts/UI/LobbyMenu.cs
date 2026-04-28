using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UIElements;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LobbyMenu : MonoBehaviourPunCallbacks
{
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text regionText;
    public TMPro.TMP_Text createdRoomText;
    public TMPro.TMP_InputField iFNewRoom;
    public ScrollView scrollView;
    public GameObject roomPanelClass;
    public Transform scrollViewTransform;
    public GameObject playerCharacter;
    public UnityEngine.UI.Button connectButton;
    public TMPro.TMP_Text playerCountText;
    [Header("Room List Style")]
    [SerializeField] private TMP_FontAsset roomListFont;
    
    [Header("Panel Management")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject controlsPanel;

    private string gameVer = "0.0";
    private List<RoomInfo>roomsInfo = new List<RoomInfo>();
    private List<GameObject>roomPanels = new List<GameObject>();
    private const int REQUIRED_PLAYERS = 4;
    private bool didCreateRoom;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = gameVer;
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu";

            PhotonNetwork.ConnectUsingSettings();
        }

        // Initialize connect button state
        if (connectButton != null)
        {
            connectButton.gameObject.SetActive(false);
            connectButton.interactable = false;
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

            didCreateRoom = false;
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
            

            TMP_Text roomEntryText = roomPanels[i].GetComponentInChildren<TMP_Text>();
            if (roomListFont != null)
            {
                roomEntryText.font = roomListFont;
            }
            roomEntryText.enableAutoSizing = false;
            roomEntryText.fontSize = 30f;
            roomEntryText.text = roomsInfo[i].Name + ": " + roomsInfo[i].PlayerCount + "/" + roomsInfo[i].MaxPlayers;
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

    //On joining a room, show connect button and update its state
    public override void OnJoinedRoom()
    {
        if (createdRoomText != null && !didCreateRoom)
        {
            createdRoomText.text = string.Empty;
        }

        if (connectButton != null)
        {
            connectButton.gameObject.SetActive(true);
        }
        UpdateConnectButton();
    }

    //Called only on the client that successfully created the room
    public override void OnCreatedRoom()
    {
        didCreateRoom = true;

        if (createdRoomText != null && PhotonNetwork.CurrentRoom != null)
        {
            createdRoomText.text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        }
    }

    //Update button state when a player joins
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateConnectButton();
    }

    //Update button state when a player leaves
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateConnectButton();
    }

    //Update the connect button based on player count and master client status
    private void UpdateConnectButton()
    {
        if (connectButton == null || !PhotonNetwork.InRoom)
            return;

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        bool allPlayersJoined = currentPlayers >= REQUIRED_PLAYERS;
        bool isMaster = PhotonNetwork.IsMasterClient;

        // Update player count text if available
        if (playerCountText != null)
        {
            playerCountText.text = "Players: " + currentPlayers + "/" + REQUIRED_PLAYERS;
        }

        // Enable button only if all players joined AND local player is master
        connectButton.interactable = allPlayersJoined && isMaster;

        // Optional: Change button color to indicate state
        var colors = connectButton.colors;
        if (allPlayersJoined && isMaster)
        {
            colors.normalColor = Color.green;
        }
        else
        {
            colors.normalColor = Color.gray;
        }
        connectButton.colors = colors;
    }

    //Called when Connect button is pressed - loads the game scene
    public void OnConnectButtonPressed()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= REQUIRED_PLAYERS)
        {
            PhotonNetwork.LoadLevel("Blockout");
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

    /// <summary>
    /// Shows the controls panel and hides the lobby panel.
    /// Wire this to the Controls button's OnClick event.
    /// </summary>
    public void ShowControlsPanel()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Shows the lobby panel and hides the controls panel.
    /// Wire this to the Back button's OnClick event.
    /// </summary>
    public void ShowLobbyPanel()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Generic panel swap function. Disables panelToHide and enables panelToShow.
    /// </summary>
    public void SwapPanels(GameObject panelToHide, GameObject panelToShow)
    {
        if (panelToHide != null)
        {
            panelToHide.SetActive(false);
        }

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
    }

}
