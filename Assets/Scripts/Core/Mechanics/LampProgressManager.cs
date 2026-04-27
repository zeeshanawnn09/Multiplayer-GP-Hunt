using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LampProgressManager : MonoBehaviourPunCallbacks
{
    public static LampProgressManager Instance { get; private set; }
    public int CurrentLitLamps => _currentLitLamps;
    public int LampsNeededToOpenDoor => lampsNeededToOpenDoor;
    public bool IsRitualReady => _currentLitLamps >= lampsNeededToOpenDoor;

    [SerializeField] private int totalLamps = 15;
    [SerializeField] private int lampsNeededToOpenDoor = 10;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image lampUnlitBackgroundImage;
    [SerializeField] private Image lampLitFillImage;
    [SerializeField] private GameObject ritualReadyPrompt;
    [SerializeField] private float ritualReadyPromptDurationSeconds = 10f;

    private const string LitLampCountKey = "LitLampCount";

    private int _currentLitLamps;
    private bool _ritualDoorLogged;
    private int _lastLoggedLitLamps = -1;
    private Coroutine _ritualPromptCoroutine;

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
        SyncTotalLampCountFromScene();
        ConfigureProgressBar();
        SetRitualPromptVisible(false);

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

    private void SyncTotalLampCountFromScene()
    {
        LitLampBehavior[] lampsInScene = FindObjectsByType<LitLampBehavior>(FindObjectsSortMode.None);
        if (lampsInScene.Length <= 0 || lampsInScene.Length == totalLamps)
        {
            return;
        }

        totalLamps = lampsInScene.Length;
        Debug.Log($"LampProgressManager: totalLamps synced to scene lamp count ({totalLamps}).");
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

    public void NotifyLampStateChangedByMaster(bool isLit)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int updatedCount = GetLitLampCountFromScene();

        if (updatedCount == GetRoomLitLampCount())
        {
            return;
        }

        SetRoomLitLampCount(updatedCount);
        ApplyLitLampCount(updatedCount);
    }

    public void NotifyLampStateChangedLocalFallback(bool isLit)
    {
        if (PhotonNetwork.InRoom)
        {
            // In-room progress must come from room properties to stay in sync.
            return;
        }

        int delta = isLit ? 1 : -1;
        ApplyLitLampCount(Mathf.Clamp(_currentLitLamps + delta, 0, totalLamps));
    }

    private void ConfigureProgressBar()
    {
        if (progressBar == null)
        {
            ConfigureLampImageProgress();
            return;
        }

        progressBar.minValue = 0f;
        progressBar.maxValue = totalLamps;
        progressBar.wholeNumbers = true;

        ConfigureLampImageProgress();
    }

    private void ConfigureLampImageProgress()
    {
        if (lampUnlitBackgroundImage != null)
        {
            lampUnlitBackgroundImage.enabled = true;
        }

        if (lampLitFillImage == null)
        {
            return;
        }

        lampLitFillImage.type = Image.Type.Filled;
        lampLitFillImage.fillMethod = Image.FillMethod.Horizontal;
        lampLitFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        lampLitFillImage.fillAmount = 0f;
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

    private int GetLitLampCountFromScene()
    {
        LitLampBehavior[] lampsInScene = FindObjectsByType<LitLampBehavior>(FindObjectsSortMode.None);
        int litLampCount = 0;

        for (int i = 0; i < lampsInScene.Length; i++)
        {
            LitLampBehavior lamp = lampsInScene[i];
            if (lamp != null && lamp.IsLit)
            {
                litLampCount++;
            }
        }

        return Mathf.Clamp(litLampCount, 0, totalLamps);
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
        int previousLitLampCount = _currentLitLamps;
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

        if (lampLitFillImage != null)
        {
            lampLitFillImage.fillAmount = GetNormalizedProgress();
        }

        if (!_ritualDoorLogged && _currentLitLamps >= lampsNeededToOpenDoor)
        {
            _ritualDoorLogged = true;
            Debug.Log($"Ritual door has opened ({_currentLitLamps}/{totalLamps} lamps lit)");
        }

        bool crossedThreshold = previousLitLampCount < lampsNeededToOpenDoor && _currentLitLamps >= lampsNeededToOpenDoor;
        if (crossedThreshold)
        {
            ShowRitualReadyPromptTemporarily();
        }
    }

    private float GetNormalizedProgress()
    {
        int safeTotalLamps = Mathf.Max(1, totalLamps);
        return Mathf.Clamp01((float)_currentLitLamps / safeTotalLamps);
    }

    private void ShowRitualReadyPromptTemporarily()
    {
        if (ritualReadyPrompt == null)
        {
            return;
        }

        if (_ritualPromptCoroutine != null)
        {
            StopCoroutine(_ritualPromptCoroutine);
        }

        _ritualPromptCoroutine = StartCoroutine(HideRitualPromptAfterDelay());
    }

    private System.Collections.IEnumerator HideRitualPromptAfterDelay()
    {
        SetRitualPromptVisible(true);

        float clampedDuration = Mathf.Max(0f, ritualReadyPromptDurationSeconds);
        if (clampedDuration > 0f)
        {
            yield return new WaitForSeconds(clampedDuration);
        }

        SetRitualPromptVisible(false);
        _ritualPromptCoroutine = null;
    }

    private void SetRitualPromptVisible(bool isVisible)
    {
        if (ritualReadyPrompt != null)
        {
            ritualReadyPrompt.SetActive(isVisible);
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_ritualPromptCoroutine != null)
        {
            StopCoroutine(_ritualPromptCoroutine);
            _ritualPromptCoroutine = null;
        }

        SetRitualPromptVisible(false);
    }
}
