using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using TMPro;

// Responsible for evaluating win/lose conditions and showing result images.
public class GameEndManager : MonoBehaviourPunCallbacks
{
    [Header("Result Images")]
    [SerializeField] private GameObject priestWinForPriest; // priests see this when priests win
    [SerializeField] private GameObject priestWinForGhost;  // ghost sees this when priests win
    [SerializeField] private GameObject ghostWinForPriest; // priests see this when ghost wins
    [SerializeField] private GameObject ghostWinForGhost;  // ghost sees this when ghost wins
    
    [Header("Result Hints")]
    [SerializeField] private TMP_Text priestWinForPriestHint;
    [SerializeField] private TMP_Text priestWinForGhostHint;
    [SerializeField] private TMP_Text ghostWinForPriestHint;
    [SerializeField] private TMP_Text ghostWinForGhostHint;

    [Header("Ritual")]
    [SerializeField] private string ritualTaskValueKey = "RitualTaskValue";
    [SerializeField] private int totalTaskSteps = 5;
    
    [Header("Lobby")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    private bool matchEnded = false;

    private void Start()
    {
        HideAllResults();
    }

    private void Update()
    {
        // allow returning to lobby with Esc when match ended
        if (matchEnded && Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToLobby();
            return;
        }

        if (matchEnded)
            return;

        EvaluateWinConditions();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (matchEnded)
            return;

        // If ritual task value changed, re-evaluate
        if (propertiesThatChanged.ContainsKey(ritualTaskValueKey))
        {
            EvaluateWinConditions();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (matchEnded)
            return;

        EvaluateWinConditions();
    }

    private void EvaluateWinConditions()
    {
        // 1) Check priest victory via ritual completion
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ritualTaskValueKey, out object val) && val is int taskValue)
        {
            if (taskValue >= totalTaskSteps)
            {
                OnMatchEnded(priestsWon: true);
                return;
            }
        }

        // 2) Check ghost victory: all priests dead or disconnected
        int totalPriests = 0;
        int alivePriests = 0;

        PlayerControls[] players = FindObjectsByType<PlayerControls>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            PlayerControls pc = players[i];
            if (pc == null) continue;

            if (!pc.HasAssignedRole) continue;

            if (pc.IsPriest)
            {
                totalPriests++;
                HealthSystem hs = pc.GetComponent<HealthSystem>();
                if (hs == null || !hs.IsDead)
                {
                    alivePriests++;
                }
            }
        }

        // If there were priests assigned but none alive => ghost wins
        if (totalPriests > 0 && alivePriests == 0)
        {
            OnMatchEnded(priestsWon: false);
            return;
        }

        // No conclusion yet
    }

    private void OnMatchEnded(bool priestsWon)
    {
        matchEnded = true;

        // Determine local player's role
        bool localIsPriest = false;
        if (PlayerControls.localPlayerInstance != null)
        {
            PlayerControls localPc = PlayerControls.localPlayerInstance.GetComponent<PlayerControls>();
            if (localPc != null && localPc.HasAssignedRole)
            {
                localIsPriest = localPc.IsPriest;
            }
        }

        // Hide any existing UI/controls if necessary
        // Show the appropriate result image and set hint text
        HideAllResults();

        const string escHint = "Press 'Esc' to return to lobby";

        if (priestsWon)
        {
            if (localIsPriest)
            {
                if (priestWinForPriest != null) priestWinForPriest.SetActive(true);
                if (priestWinForPriestHint != null) priestWinForPriestHint.text = escHint;
            }
            else
            {
                if (priestWinForGhost != null) priestWinForGhost.SetActive(true);
                if (priestWinForGhostHint != null) priestWinForGhostHint.text = escHint;
            }
        }
        else // ghost won
        {
            if (localIsPriest)
            {
                if (ghostWinForPriest != null) ghostWinForPriest.SetActive(true);
                if (ghostWinForPriestHint != null) ghostWinForPriestHint.text = escHint;
            }
            else
            {
                if (ghostWinForGhost != null) ghostWinForGhost.SetActive(true);
                if (ghostWinForGhostHint != null) ghostWinForGhostHint.text = escHint;
            }
        }

        // Optionally: disable player inputs or pause the game here.
    }

    

    private void HideAllResults()
    {
        if (priestWinForPriest != null) priestWinForPriest.SetActive(false);
        if (priestWinForGhost != null) priestWinForGhost.SetActive(false);
        if (ghostWinForPriest != null) ghostWinForPriest.SetActive(false);
        if (ghostWinForGhost != null) ghostWinForGhost.SetActive(false);
    }

    private void ReturnToLobby()
    {
        // Leave the room if we're in one, then load the lobby scene locally.
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        SceneManager.LoadScene(lobbySceneName);
    }
}
