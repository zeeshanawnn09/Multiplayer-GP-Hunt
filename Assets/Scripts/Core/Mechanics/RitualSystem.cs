using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RitualSystem : MonoBehaviourPunCallbacks
{
    [Header("Bell Setup")]
    [SerializeField] private GameObject bellObject;
    [SerializeField] private GameObject interactPrompt;

    [Header("Interaction")]
    [SerializeField] private float holdDurationSeconds = 3f;

    [Header("Bell Cycle")]
    [SerializeField] private int totalRingsRequired = 5;
    [SerializeField] private float minCooldownSeconds = 10f;
    [SerializeField] private float maxCooldownSeconds = 60f;

    [Header("Task UI")]
    [SerializeField] private TMP_Text taskText;
    [SerializeField] private int initialTaskValue = 0;

    private const string BellVisibleKey = "BellVisible";
    private const string BellRingCountKey = "BellRingCount";
    private const string BellTaskValueKey = "BellTaskValue";

    private PlayerControls _localPlayerInRange;
    private float _holdProgress;
    private bool _isBellVisible;
    private bool _isCoolingDown;
    private Coroutine _cooldownCoroutine;

    private int _completedRings;
    private int _taskValue;

    private void Awake()
    {
        SetBellVisibleLocal(false);
        SetPromptVisible(false);
        _taskValue = initialTaskValue;
        UpdateTaskUI();
    }

    private void Start()
    {
        SyncFromRoomProperties();

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            InitializeRoomPropertiesIfNeeded();
            TryShowBellFromMaster();
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            EvaluateBellVisibilityFromMaster();
        }

        if (!_isBellVisible)
        {
            SetPromptVisible(false);
            _holdProgress = 0f;
            return;
        }

        if (!IsLocalPriestEligible())
        {
            SetPromptVisible(false);
            _holdProgress = 0f;
            return;
        }

        SetPromptVisible(true);

        bool isInteractHeld = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        if (!isInteractHeld)
        {
            _holdProgress = 0f;
            return;
        }

        _holdProgress += Time.deltaTime;
        if (_holdProgress < holdDurationSeconds)
        {
            return;
        }

        _holdProgress = 0f;

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RequestBellRing), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            HandleBellRingAccepted();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls.gameObject == PlayerControls.localPlayerInstance)
        {
            _localPlayerInRange = playerControls;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls == _localPlayerInRange)
        {
            _localPlayerInRange = null;
            _holdProgress = 0f;
            SetPromptVisible(false);
        }
    }

    private bool IsLocalPriestEligible()
    {
        if (_localPlayerInRange == null)
        {
            return false;
        }

        return _localPlayerInRange.HasAssignedRole && _localPlayerInRange.playerRole == PlayerRole.Priest;
    }

    private bool IsLampRequirementMet()
    {
        return LampProgressManager.Instance != null && LampProgressManager.Instance.IsRitualReady;
    }

    private void TryShowBellFromMaster()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_completedRings >= totalRingsRequired || _isCoolingDown || !IsLampRequirementMet())
        {
            SetRoomBellVisible(false);
            return;
        }

        SetRoomBellVisible(true);
    }

    private void EvaluateBellVisibilityFromMaster()
    {
        if (_completedRings >= totalRingsRequired || _isCoolingDown)
        {
            if (_isBellVisible)
            {
                SetRoomBellVisible(false);
            }

            return;
        }

        bool shouldBeVisible = IsLampRequirementMet();
        if (shouldBeVisible != _isBellVisible)
        {
            SetRoomBellVisible(shouldBeVisible);
        }
    }

    [PunRPC]
    private void RPC_RequestBellRing(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!_isBellVisible || _isCoolingDown || _completedRings >= totalRingsRequired || !IsLampRequirementMet())
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        HandleBellRingAccepted();
    }

    private void HandleBellRingAccepted()
    {
        _completedRings = Mathf.Clamp(_completedRings + 1, 0, totalRingsRequired);
        SetRoomRingCount(_completedRings);
        SetRoomBellVisible(false);

        if (_completedRings >= totalRingsRequired)
        {
            SetRoomTaskValue(initialTaskValue + 1);
            StopCooldown();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
        }

        StartCooldown();
    }

    private void StartCooldown()
    {
        StopCooldown();
        _isCoolingDown = true;
        _cooldownCoroutine = StartCoroutine(CooldownCoroutine());
    }

    private void StopCooldown()
    {
        _isCoolingDown = false;

        if (_cooldownCoroutine != null)
        {
            StopCoroutine(_cooldownCoroutine);
            _cooldownCoroutine = null;
        }
    }

    private System.Collections.IEnumerator CooldownCoroutine()
    {
        float minSeconds = Mathf.Max(0f, minCooldownSeconds);
        float maxSeconds = Mathf.Max(minSeconds, maxCooldownSeconds);
        float randomCooldown = Random.Range(minSeconds, maxSeconds);

        yield return new WaitForSeconds(randomCooldown);

        _isCoolingDown = false;
        _cooldownCoroutine = null;
        TryShowBellFromMaster();
    }

    private void InitializeRoomPropertiesIfNeeded()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable defaults = new Hashtable();

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(BellVisibleKey))
        {
            defaults[BellVisibleKey] = false;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(BellRingCountKey))
        {
            defaults[BellRingCountKey] = 0;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(BellTaskValueKey))
        {
            defaults[BellTaskValueKey] = initialTaskValue;
        }

        if (defaults.Count > 0)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(defaults);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(BellVisibleKey, out object bellVisibleObj) && bellVisibleObj is bool bellVisible)
        {
            SetBellVisibleLocal(bellVisible);
        }

        if (propertiesThatChanged.TryGetValue(BellRingCountKey, out object ringCountObj) && ringCountObj is int ringCount)
        {
            _completedRings = Mathf.Clamp(ringCount, 0, totalRingsRequired);
        }

        if (propertiesThatChanged.TryGetValue(BellTaskValueKey, out object taskValueObj) && taskValueObj is int taskValue)
        {
            _taskValue = taskValue;
            UpdateTaskUI();
        }

        if (PhotonNetwork.IsMasterClient && !_isCoolingDown)
        {
            TryShowBellFromMaster();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_completedRings >= totalRingsRequired)
        {
            return;
        }

        StopCooldown();
        TryShowBellFromMaster();
    }

    private void SyncFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom)
        {
            SetBellVisibleLocal(false);
            _completedRings = 0;
            _taskValue = initialTaskValue;
            UpdateTaskUI();
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BellVisibleKey, out object bellVisibleObj) && bellVisibleObj is bool bellVisible)
        {
            SetBellVisibleLocal(bellVisible);
        }
        else
        {
            SetBellVisibleLocal(false);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BellRingCountKey, out object ringCountObj) && ringCountObj is int ringCount)
        {
            _completedRings = Mathf.Clamp(ringCount, 0, totalRingsRequired);
        }
        else
        {
            _completedRings = 0;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BellTaskValueKey, out object taskValueObj) && taskValueObj is int taskValue)
        {
            _taskValue = taskValue;
        }
        else
        {
            _taskValue = initialTaskValue;
        }

        UpdateTaskUI();
    }

    private void SetRoomBellVisible(bool isVisible)
    {
        if (!PhotonNetwork.InRoom)
        {
            SetBellVisibleLocal(isVisible);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { BellVisibleKey, isVisible }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomRingCount(int ringCount)
    {
        if (!PhotonNetwork.InRoom)
        {
            _completedRings = ringCount;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { BellRingCountKey, ringCount }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomTaskValue(int taskValue)
    {
        if (!PhotonNetwork.InRoom)
        {
            _taskValue = taskValue;
            UpdateTaskUI();
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { BellTaskValueKey, taskValue }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetBellVisibleLocal(bool isVisible)
    {
        _isBellVisible = isVisible;

        if (bellObject != null)
        {
            bellObject.SetActive(isVisible);
        }

        if (!isVisible)
        {
            _holdProgress = 0f;
            SetPromptVisible(false);
        }
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(isVisible);
        }
    }

    private void UpdateTaskUI()
    {
        if (taskText != null)
        {
            taskText.text = _taskValue.ToString();
        }
    }

    private void OnDisable()
    {
        _localPlayerInRange = null;
        _holdProgress = 0f;
        StopCooldown();
        SetPromptVisible(false);
    }
}
