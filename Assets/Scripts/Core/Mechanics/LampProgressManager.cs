using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LampProgressManager : MonoBehaviourPunCallbacks
{
    public static LampProgressManager Instance { get; private set; }

    [SerializeField] private int totalLamps = 15;
    [SerializeField] private int lampsNeededToOpenDoor = 10;
    [SerializeField] private Slider progressBar;

    private const string LitLampCountKey = "LitLampCount";

    private int _currentLitLamps;
    private bool _ritualDoorLogged;
    private int _lastLoggedLitLamps = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ValidateLampSetup();
        ConfigureProgressBar();

        if (!PhotonNetwork.InRoom)
        {
            ApplyLitLampCount(0);
            return;
        }

        if (PhotonNetwork.IsMasterClient && !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(LitLampCountKey))
        {
            SetRoomLitLampCount(0);
        }

        ApplyLitLampCount(GetRoomLitLampCount());
    }

    private void ValidateLampSetup()
    {
        LitLampBehavior[] lampsInScene = FindObjectsByType<LitLampBehavior>(FindObjectsSortMode.None);
        if (lampsInScene.Length != totalLamps)
        {
            Debug.LogWarning($"LampProgressManager: totalLamps is {totalLamps}, but found {lampsInScene.Length} LitLampBehavior instances in scene.");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(LitLampCountKey, out object litLampCountObj) && litLampCountObj is int litLampCount)
        {
            ApplyLitLampCount(litLampCount);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        ApplyLitLampCount(GetRoomLitLampCount());
    }

    public void NotifyLampLitByMaster()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int roomCount = GetRoomLitLampCount();
        int updatedCount = Mathf.Clamp(roomCount + 1, 0, totalLamps);

        if (updatedCount == roomCount)
        {
            return;
        }

        SetRoomLitLampCount(updatedCount);
    }

    public void NotifyLampLitLocalFallback()
    {
        if (PhotonNetwork.InRoom)
        {
            // In-room progress must come from room properties to stay in sync.
            return;
        }

        ApplyLitLampCount(Mathf.Clamp(_currentLitLamps + 1, 0, totalLamps));
    }

    private void ConfigureProgressBar()
    {
        if (progressBar == null)
        {
            return;
        }

        progressBar.minValue = 0f;
        progressBar.maxValue = totalLamps;
        progressBar.wholeNumbers = true;
    }

    private int GetRoomLitLampCount()
    {
        if (!PhotonNetwork.InRoom)
        {
            return 0;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LitLampCountKey, out object litLampCountObj) && litLampCountObj is int litLampCount)
        {
            return litLampCount;
        }

        return 0;
    }

    private void SetRoomLitLampCount(int litLampCount)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { LitLampCountKey, litLampCount }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void ApplyLitLampCount(int litLampCount)
    {
        _currentLitLamps = Mathf.Clamp(litLampCount, 0, totalLamps);

        if (_currentLitLamps != _lastLoggedLitLamps)
        {
            _lastLoggedLitLamps = _currentLitLamps;
            Debug.Log($"Lamps lit: {_currentLitLamps}/{totalLamps}");
        }

        if (progressBar != null)
        {
            progressBar.value = _currentLitLamps;
        }

        if (!_ritualDoorLogged && _currentLitLamps >= lampsNeededToOpenDoor)
        {
            _ritualDoorLogged = true;
            Debug.Log($"Ritual door has opened ({_currentLitLamps}/{totalLamps} lamps lit)");
        }
    }
}
