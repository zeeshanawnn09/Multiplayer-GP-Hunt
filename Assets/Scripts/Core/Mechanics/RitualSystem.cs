using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RitualSystem : MonoBehaviourPunCallbacks
{
    [Header("Bell Setup")]
    [SerializeField] private GameObject bellObject;

    [Header("Secondary Ritual Setup")]
    [SerializeField] private GameObject secondaryTaskObject;

    [Header("Flower Offering Setup")]
    [SerializeField] private GameObject shrineObject;

    [Header("Interaction UI")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private TMP_Text interactPromptText;

    [Header("Flower Offering UI")]
    [SerializeField] private GameObject flowerOfferingPanel;
    [SerializeField] private Slider flowerOfferingProgressSlider;
    [SerializeField] private TMP_Text flowerOfferingTimerText;

    [Header("Interaction")]
    [SerializeField] private float holdDurationSeconds = 3f;

    [Header("Bell Cycle")]
    [SerializeField] private int totalRingsRequired = 5;
    [SerializeField] private float minCooldownSeconds = 10f;
    [SerializeField] private float maxCooldownSeconds = 60f;

    [Header("Secondary Cycle")]
    [SerializeField] private int secondaryInteractionsRequired = 3;
    [SerializeField] private float secondaryMinCooldownSeconds = 5f;
    [SerializeField] private float secondaryMaxCooldownSeconds = 15f;
    [SerializeField] private float secondaryRevealDelaySeconds = 5f;

    [Header("Flower Offering Cycle")]
    [SerializeField] private int minFlowersRequired = 5;
    [SerializeField] private int maxFlowersRequired = 10;
    [SerializeField] private float shrineMinCooldownSeconds = 5f;
    [SerializeField] private float shrineMaxCooldownSeconds = 10f;
    [SerializeField] private float offeringDurationSeconds = 10f;
    [SerializeField] private int smashesRequired = 20;

    [Header("Task UI")]
    [SerializeField] private TMP_Text taskText;
    [SerializeField] private int initialTaskValue = 0;

    private const string BellVisibleKey = "BellVisible";
    private const string BellRingCountKey = "BellRingCount";
    private const string SecondaryVisibleKey = "SecondaryVisible";
    private const string SecondaryInteractionCountKey = "SecondaryInteractionCount";
    private const string SecondaryUnlockTimeKey = "SecondaryUnlockTime";
    private const string ShrineVisibleKey = "ShrineVisible";
    private const string FlowerRequiredKey = "FlowerOfferingRequired";
    private const string FlowerOfferingActiveKey = "FlowerOfferingActive";
    private const string FlowerOfferingSmashCountKey = "FlowerOfferingSmashCount";
    private const string FlowerOfferingEndTimeKey = "FlowerOfferingEndTime";
    private const string RitualTaskValueKey = "RitualTaskValue";

    private PlayerControls _localPlayerInRange;
    private float _holdProgress;

    private bool _isBellVisible;
    private bool _isSecondaryVisible;
    private bool _isShrineVisible;
    private bool _isFlowerOfferingActive;

    private bool _isBellCoolingDown;
    private bool _isSecondaryCoolingDown;
    private bool _isShrineCoolingDown;

    private Coroutine _bellCooldownCoroutine;
    private Coroutine _secondaryCooldownCoroutine;
    private Coroutine _shrineCooldownCoroutine;

    private int _completedRings;
    private int _secondaryInteractionsCompleted;
    private double _secondaryUnlockTime;
    private int _requiredFlowerCount;
    private int _offeringSmashCount;
    private double _offeringEndTime;
    private int _taskValue;

    private void Awake()
    {
        SetBellVisibleLocal(false);
        SetSecondaryVisibleLocal(false);
        SetShrineVisibleLocal(false);
        SetPromptVisible(false, string.Empty);
        SetFlowerOfferingPanelVisible(false);

        _taskValue = initialTaskValue;
        UpdateTaskUI();
    }

    private void Start()
    {
        SyncFromRoomProperties();

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            InitializeRoomPropertiesIfNeeded();
            EvaluateObjectiveVisibilityFromMaster();
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            EvaluateOfferingTimeoutFromMaster();
            EvaluateObjectiveVisibilityFromMaster();
        }

        if (!IsAnyObjectiveVisible() || !IsLocalPriestEligible())
        {
            _holdProgress = 0f;
            SetPromptVisible(false, string.Empty);
            SetFlowerOfferingPanelVisible(false);
            return;
        }

        if (_isFlowerOfferingActive)
        {
            HandleFlowerOfferingInput();
            return;
        }

        if (_isBellVisible)
        {
            SetPromptVisible(true, $"Hold E to ring bell ({_completedRings}/{totalRingsRequired})");
            HandleBellInput();
            return;
        }

        if (_isSecondaryVisible)
        {
            SetPromptVisible(true, $"Press E to interact ({_secondaryInteractionsCompleted}/{secondaryInteractionsRequired})");
            HandleSecondaryInput();
            return;
        }

        if (_isShrineVisible)
        {
            int currentFlowers = GetSharedFlowerCount();
            string prompt = $"Need {_requiredFlowerCount} flowers (Current: {currentFlowers}) - Press E to start ritual";
            SetPromptVisible(true, prompt);
            HandleShrineStartInput();
        }
    }

    private void HandleBellInput()
    {
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

    private void HandleSecondaryInput()
    {
        if (!_localPlayerInRange.ConsumeInteractPressed())
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RequestSecondaryInteraction), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            HandleSecondaryInteractionAccepted();
        }
    }

    private void HandleShrineStartInput()
    {
        if (!_localPlayerInRange.ConsumeInteractPressed())
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RequestStartFlowerOffering), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            StartFlowerOfferingSession();
        }
    }

    private void HandleFlowerOfferingInput()
    {
        UpdateFlowerOfferingUi();

        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC(nameof(RPC_RequestFlowerSmash), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            HandleFlowerSmashAccepted();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls == null || playerControls.gameObject != PlayerControls.localPlayerInstance)
        {
            return;
        }

        if (playerControls.IsPriest)
        {
            _localPlayerInRange = playerControls;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls != null && playerControls == _localPlayerInRange)
        {
            _localPlayerInRange = null;
            _holdProgress = 0f;
            SetPromptVisible(false, string.Empty);
            SetFlowerOfferingPanelVisible(false);
        }
    }

    private bool IsLocalPriestEligible()
    {
        return _localPlayerInRange != null && _localPlayerInRange.IsPriest;
    }

    private bool IsLampRequirementMet()
    {
        return LampProgressManager.Instance != null && LampProgressManager.Instance.IsRitualReady;
    }

    private bool IsAnyObjectiveVisible()
    {
        return _isBellVisible || _isSecondaryVisible || _isShrineVisible || _isFlowerOfferingActive;
    }

    private bool IsBellObjectiveActive()
    {
        return _completedRings < totalRingsRequired;
    }

    private bool IsSecondaryObjectiveUnlocked()
    {
        return _completedRings >= totalRingsRequired;
    }

    private bool IsSecondaryObjectiveComplete()
    {
        return _secondaryInteractionsCompleted >= secondaryInteractionsRequired;
    }

    private bool IsSecondaryRevealDelayComplete()
    {
        if (_secondaryUnlockTime <= 0d)
        {
            return true;
        }

        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble;
        return now >= _secondaryUnlockTime;
    }

    private bool IsFlowerOfferingObjectiveUnlocked()
    {
        return IsSecondaryObjectiveComplete();
    }

    private bool IsFlowerOfferingObjectiveComplete()
    {
        return _taskValue >= initialTaskValue + 3;
    }

    private void EvaluateObjectiveVisibilityFromMaster()
    {
        if (IsBellObjectiveActive())
        {
            if (_isSecondaryVisible)
            {
                SetSecondaryVisible(false);
            }

            if (_isShrineVisible)
            {
                SetShrineVisible(false);
            }

            bool shouldShowBell = !_isBellCoolingDown && IsLampRequirementMet();
            if (shouldShowBell != _isBellVisible)
            {
                SetBellVisible(shouldShowBell);
            }

            return;
        }

        if (_isBellVisible)
        {
            SetBellVisible(false);
        }

        if (!IsSecondaryObjectiveComplete())
        {
            if (_isShrineVisible)
            {
                SetShrineVisible(false);
            }

            bool shouldShowSecondary = IsSecondaryObjectiveUnlocked()
                && !_isSecondaryCoolingDown
                && IsSecondaryRevealDelayComplete()
                && IsLampRequirementMet();

            if (shouldShowSecondary != _isSecondaryVisible)
            {
                SetSecondaryVisible(shouldShowSecondary);
            }

            return;
        }

        if (_isSecondaryVisible)
        {
            SetSecondaryVisible(false);
        }

        if (!IsFlowerOfferingObjectiveUnlocked() || IsFlowerOfferingObjectiveComplete())
        {
            if (_isShrineVisible)
            {
                SetShrineVisible(false);
            }

            return;
        }

        if (_isFlowerOfferingActive)
        {
            if (!_isShrineVisible)
            {
                SetShrineVisible(true);
            }

            return;
        }

        bool shouldShowShrine = !_isShrineCoolingDown && IsLampRequirementMet();
        if (shouldShowShrine != _isShrineVisible)
        {
            SetShrineVisible(shouldShowShrine);
        }
    }

    [PunRPC]
    private void RPC_RequestBellRing(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!_isBellVisible || _isBellCoolingDown || !IsBellObjectiveActive() || !IsLampRequirementMet())
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        HandleBellRingAccepted();
    }

    [PunRPC]
    private void RPC_RequestSecondaryInteraction(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!_isSecondaryVisible || _isSecondaryCoolingDown || !IsSecondaryObjectiveUnlocked() || IsSecondaryObjectiveComplete() || !IsLampRequirementMet())
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        HandleSecondaryInteractionAccepted();
    }

    [PunRPC]
    private void RPC_RequestStartFlowerOffering(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!_isShrineVisible || _isFlowerOfferingActive || !IsFlowerOfferingObjectiveUnlocked() || IsFlowerOfferingObjectiveComplete() || !IsLampRequirementMet())
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        if (!IsPriestActor(actorNumber))
        {
            return;
        }

        int requiredFlowers = Mathf.Max(1, _requiredFlowerCount);
        int currentFlowers = GetSharedFlowerCount();

        if (currentFlowers < requiredFlowers)
        {
            return;
        }

        SetSharedFlowerCount(currentFlowers - requiredFlowers);
        StartFlowerOfferingSession();
    }

    [PunRPC]
    private void RPC_RequestFlowerSmash(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!_isFlowerOfferingActive)
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        if (!IsPriestActor(actorNumber))
        {
            return;
        }

        if (PhotonNetwork.Time >= _offeringEndTime)
        {
            HandleFlowerOfferingFailed();
            return;
        }

        HandleFlowerSmashAccepted();
    }

    private void HandleBellRingAccepted()
    {
        _completedRings = Mathf.Clamp(_completedRings + 1, 0, totalRingsRequired);
        SetRoomRingCount(_completedRings);
        SetBellVisible(false);

        if (!IsBellObjectiveActive())
        {
            SetRoomTaskValue(initialTaskValue + 1);
            SetRoomSecondaryCount(0);
            double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble;
            SetRoomSecondaryUnlockTime(now + Mathf.Max(0f, secondaryRevealDelaySeconds));
            SetRoomSecondaryVisible(false);
            StopBellCooldown();
            EvaluateObjectiveVisibilityFromMaster();
            return;
        }

        StartBellCooldown();
    }

    private void HandleSecondaryInteractionAccepted()
    {
        _secondaryInteractionsCompleted = Mathf.Clamp(_secondaryInteractionsCompleted + 1, 0, secondaryInteractionsRequired);
        SetRoomSecondaryCount(_secondaryInteractionsCompleted);
        SetSecondaryVisible(false);

        if (IsSecondaryObjectiveComplete())
        {
            SetRoomTaskValue(_taskValue + 1);
            StopSecondaryCooldown();
            SetShrineVisible(false);
            StartShrineCooldown();
            EvaluateObjectiveVisibilityFromMaster();
            return;
        }

        StartSecondaryCooldown();
    }

    private void HandleFlowerSmashAccepted()
    {
        _offeringSmashCount = Mathf.Clamp(_offeringSmashCount + 1, 0, Mathf.Max(1, smashesRequired));
        SetRoomOfferingSmashCount(_offeringSmashCount);

        if (_offeringSmashCount >= Mathf.Max(1, smashesRequired))
        {
            HandleFlowerOfferingCompleted();
        }
    }

    private void StartFlowerOfferingSession()
    {
        StopShrineCooldown();

        SetShrineVisible(true);
        SetRoomOfferingSmashCount(0);
        SetRoomOfferingEndTime(PhotonNetwork.Time + Mathf.Max(1f, offeringDurationSeconds));
        SetRoomOfferingActive(true);
    }

    private void HandleFlowerOfferingCompleted()
    {
        SetRoomOfferingActive(false);
        SetShrineVisible(false);
        SetRoomTaskValue(_taskValue + 1);
        StopShrineCooldown();
        EvaluateObjectiveVisibilityFromMaster();
    }

    private void HandleFlowerOfferingFailed()
    {
        SetRoomOfferingActive(false);
        SetRoomOfferingSmashCount(0);
        SetShrineVisible(false);
        StartShrineCooldown();
    }

    private void EvaluateOfferingTimeoutFromMaster()
    {
        if (!_isFlowerOfferingActive)
        {
            return;
        }

        if (PhotonNetwork.Time >= _offeringEndTime)
        {
            HandleFlowerOfferingFailed();
        }
    }

    private void StartBellCooldown()
    {
        StopBellCooldown();
        _isBellCoolingDown = true;
        _bellCooldownCoroutine = StartCoroutine(BellCooldownCoroutine());
    }

    private void StopBellCooldown()
    {
        _isBellCoolingDown = false;

        if (_bellCooldownCoroutine != null)
        {
            StopCoroutine(_bellCooldownCoroutine);
            _bellCooldownCoroutine = null;
        }
    }

    private void StartSecondaryCooldown()
    {
        StopSecondaryCooldown();
        _isSecondaryCoolingDown = true;
        _secondaryCooldownCoroutine = StartCoroutine(SecondaryCooldownCoroutine());
    }

    private void StopSecondaryCooldown()
    {
        _isSecondaryCoolingDown = false;

        if (_secondaryCooldownCoroutine != null)
        {
            StopCoroutine(_secondaryCooldownCoroutine);
            _secondaryCooldownCoroutine = null;
        }
    }

    private void StartShrineCooldown()
    {
        StopShrineCooldown();
        _isShrineCoolingDown = true;
        _shrineCooldownCoroutine = StartCoroutine(ShrineCooldownCoroutine());
    }

    private void StopShrineCooldown()
    {
        _isShrineCoolingDown = false;

        if (_shrineCooldownCoroutine != null)
        {
            StopCoroutine(_shrineCooldownCoroutine);
            _shrineCooldownCoroutine = null;
        }
    }

    private System.Collections.IEnumerator BellCooldownCoroutine()
    {
        float minSeconds = Mathf.Max(0f, minCooldownSeconds);
        float maxSeconds = Mathf.Max(minSeconds, maxCooldownSeconds);
        float randomCooldown = Random.Range(minSeconds, maxSeconds);

        yield return new WaitForSeconds(randomCooldown);

        _isBellCoolingDown = false;
        _bellCooldownCoroutine = null;
        EvaluateObjectiveVisibilityFromMaster();
    }

    private System.Collections.IEnumerator SecondaryCooldownCoroutine()
    {
        float minSeconds = Mathf.Max(0f, secondaryMinCooldownSeconds);
        float maxSeconds = Mathf.Max(minSeconds, secondaryMaxCooldownSeconds);
        float randomCooldown = Random.Range(minSeconds, maxSeconds);

        yield return new WaitForSeconds(randomCooldown);

        _isSecondaryCoolingDown = false;
        _secondaryCooldownCoroutine = null;
        EvaluateObjectiveVisibilityFromMaster();
    }

    private System.Collections.IEnumerator ShrineCooldownCoroutine()
    {
        float minSeconds = Mathf.Max(0f, shrineMinCooldownSeconds);
        float maxSeconds = Mathf.Max(minSeconds, shrineMaxCooldownSeconds);
        float randomCooldown = Random.Range(minSeconds, maxSeconds);

        yield return new WaitForSeconds(randomCooldown);

        _isShrineCoolingDown = false;
        _shrineCooldownCoroutine = null;

        int minRequired = Mathf.Max(1, minFlowersRequired);
        int maxRequired = Mathf.Max(minRequired, maxFlowersRequired);
        SetRoomRequiredFlowerCount(Random.Range(minRequired, maxRequired + 1));

        EvaluateObjectiveVisibilityFromMaster();
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

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SecondaryVisibleKey))
        {
            defaults[SecondaryVisibleKey] = false;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SecondaryInteractionCountKey))
        {
            defaults[SecondaryInteractionCountKey] = 0;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SecondaryUnlockTimeKey))
        {
            defaults[SecondaryUnlockTimeKey] = 0d;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ShrineVisibleKey))
        {
            defaults[ShrineVisibleKey] = false;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FlowerRequiredKey))
        {
            defaults[FlowerRequiredKey] = Mathf.Max(1, minFlowersRequired);
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FlowerOfferingActiveKey))
        {
            defaults[FlowerOfferingActiveKey] = false;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FlowerOfferingSmashCountKey))
        {
            defaults[FlowerOfferingSmashCountKey] = 0;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FlowerOfferingEndTimeKey))
        {
            defaults[FlowerOfferingEndTimeKey] = 0d;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RitualTaskValueKey))
        {
            defaults[RitualTaskValueKey] = initialTaskValue;
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

        if (propertiesThatChanged.TryGetValue(SecondaryVisibleKey, out object secondaryVisibleObj) && secondaryVisibleObj is bool secondaryVisible)
        {
            SetSecondaryVisibleLocal(secondaryVisible);
        }

        if (propertiesThatChanged.TryGetValue(SecondaryInteractionCountKey, out object secondaryCountObj) && secondaryCountObj is int secondaryCount)
        {
            _secondaryInteractionsCompleted = Mathf.Clamp(secondaryCount, 0, secondaryInteractionsRequired);
        }

        if (propertiesThatChanged.TryGetValue(SecondaryUnlockTimeKey, out object secondaryUnlockObj) && secondaryUnlockObj is double secondaryUnlockTime)
        {
            _secondaryUnlockTime = secondaryUnlockTime;
        }

        if (propertiesThatChanged.TryGetValue(ShrineVisibleKey, out object shrineVisibleObj) && shrineVisibleObj is bool shrineVisible)
        {
            SetShrineVisibleLocal(shrineVisible);
        }

        if (propertiesThatChanged.TryGetValue(FlowerRequiredKey, out object requiredFlowersObj) && requiredFlowersObj is int requiredFlowers)
        {
            _requiredFlowerCount = Mathf.Max(1, requiredFlowers);
        }

        if (propertiesThatChanged.TryGetValue(FlowerOfferingActiveKey, out object offeringActiveObj) && offeringActiveObj is bool offeringActive)
        {
            _isFlowerOfferingActive = offeringActive;
            if (!offeringActive)
            {
                SetFlowerOfferingPanelVisible(false);
            }
        }

        if (propertiesThatChanged.TryGetValue(FlowerOfferingSmashCountKey, out object smashCountObj) && smashCountObj is int smashCount)
        {
            _offeringSmashCount = Mathf.Clamp(smashCount, 0, Mathf.Max(1, smashesRequired));
        }

        if (propertiesThatChanged.TryGetValue(FlowerOfferingEndTimeKey, out object offeringEndObj) && offeringEndObj is double offeringEndTime)
        {
            _offeringEndTime = offeringEndTime;
        }

        if (propertiesThatChanged.TryGetValue(RitualTaskValueKey, out object taskValueObj) && taskValueObj is int taskValue)
        {
            _taskValue = taskValue;
            UpdateTaskUI();
        }

        UpdateFlowerOfferingUi();

        if (PhotonNetwork.IsMasterClient)
        {
            EvaluateObjectiveVisibilityFromMaster();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        StopBellCooldown();
        StopSecondaryCooldown();
        StopShrineCooldown();
        EvaluateObjectiveVisibilityFromMaster();
    }

    private void SyncFromRoomProperties()
    {
        if (!PhotonNetwork.InRoom)
        {
            SetBellVisibleLocal(false);
            SetSecondaryVisibleLocal(false);
            SetShrineVisibleLocal(false);
            _completedRings = 0;
            _secondaryInteractionsCompleted = 0;
            _requiredFlowerCount = Mathf.Max(1, minFlowersRequired);
            _offeringSmashCount = 0;
            _offeringEndTime = 0d;
            _isFlowerOfferingActive = false;
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

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SecondaryVisibleKey, out object secondaryVisibleObj) && secondaryVisibleObj is bool secondaryVisible)
        {
            SetSecondaryVisibleLocal(secondaryVisible);
        }
        else
        {
            SetSecondaryVisibleLocal(false);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SecondaryInteractionCountKey, out object secondaryCountObj) && secondaryCountObj is int secondaryCount)
        {
            _secondaryInteractionsCompleted = Mathf.Clamp(secondaryCount, 0, secondaryInteractionsRequired);
        }
        else
        {
            _secondaryInteractionsCompleted = 0;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SecondaryUnlockTimeKey, out object secondaryUnlockObj) && secondaryUnlockObj is double secondaryUnlockTime)
        {
            _secondaryUnlockTime = secondaryUnlockTime;
        }
        else
        {
            _secondaryUnlockTime = 0d;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ShrineVisibleKey, out object shrineVisibleObj) && shrineVisibleObj is bool shrineVisible)
        {
            SetShrineVisibleLocal(shrineVisible);
        }
        else
        {
            SetShrineVisibleLocal(false);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(FlowerRequiredKey, out object requiredFlowersObj) && requiredFlowersObj is int requiredFlowers)
        {
            _requiredFlowerCount = Mathf.Max(1, requiredFlowers);
        }
        else
        {
            _requiredFlowerCount = Mathf.Max(1, minFlowersRequired);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(FlowerOfferingActiveKey, out object offeringActiveObj) && offeringActiveObj is bool offeringActive)
        {
            _isFlowerOfferingActive = offeringActive;
        }
        else
        {
            _isFlowerOfferingActive = false;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(FlowerOfferingSmashCountKey, out object smashCountObj) && smashCountObj is int smashCount)
        {
            _offeringSmashCount = Mathf.Clamp(smashCount, 0, Mathf.Max(1, smashesRequired));
        }
        else
        {
            _offeringSmashCount = 0;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(FlowerOfferingEndTimeKey, out object offeringEndObj) && offeringEndObj is double offeringEndTime)
        {
            _offeringEndTime = offeringEndTime;
        }
        else
        {
            _offeringEndTime = 0d;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RitualTaskValueKey, out object taskValueObj) && taskValueObj is int taskValue)
        {
            _taskValue = taskValue;
        }
        else
        {
            _taskValue = initialTaskValue;
        }

        UpdateTaskUI();
        UpdateFlowerOfferingUi();
    }

    private void SetBellVisible(bool isVisible)
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

    private void SetSecondaryVisible(bool isVisible)
    {
        if (!PhotonNetwork.InRoom)
        {
            SetSecondaryVisibleLocal(isVisible);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { SecondaryVisibleKey, isVisible }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomSecondaryVisible(bool isVisible)
    {
        SetSecondaryVisible(isVisible);
    }

    private void SetRoomSecondaryCount(int count)
    {
        if (!PhotonNetwork.InRoom)
        {
            _secondaryInteractionsCompleted = count;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { SecondaryInteractionCountKey, count }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomSecondaryUnlockTime(double unlockTime)
    {
        if (!PhotonNetwork.InRoom)
        {
            _secondaryUnlockTime = unlockTime;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { SecondaryUnlockTimeKey, unlockTime }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetShrineVisible(bool isVisible)
    {
        if (!PhotonNetwork.InRoom)
        {
            SetShrineVisibleLocal(isVisible);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { ShrineVisibleKey, isVisible }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomRequiredFlowerCount(int count)
    {
        if (!PhotonNetwork.InRoom)
        {
            _requiredFlowerCount = count;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { FlowerRequiredKey, count }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomOfferingActive(bool isActive)
    {
        if (!PhotonNetwork.InRoom)
        {
            _isFlowerOfferingActive = isActive;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { FlowerOfferingActiveKey, isActive }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomOfferingSmashCount(int smashCount)
    {
        if (!PhotonNetwork.InRoom)
        {
            _offeringSmashCount = smashCount;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { FlowerOfferingSmashCountKey, smashCount }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private void SetRoomOfferingEndTime(double endTime)
    {
        if (!PhotonNetwork.InRoom)
        {
            _offeringEndTime = endTime;
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { FlowerOfferingEndTimeKey, endTime }
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
            { RitualTaskValueKey, taskValue }
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
        }
    }

    private void SetSecondaryVisibleLocal(bool isVisible)
    {
        _isSecondaryVisible = isVisible;

        if (secondaryTaskObject != null)
        {
            secondaryTaskObject.SetActive(isVisible);
        }
    }

    private void SetShrineVisibleLocal(bool isVisible)
    {
        _isShrineVisible = isVisible;

        if (shrineObject != null)
        {
            shrineObject.SetActive(isVisible);
        }
    }

    private void SetPromptVisible(bool isVisible, string promptText)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(isVisible);
        }

        if (interactPromptText != null)
        {
            interactPromptText.text = isVisible ? promptText : string.Empty;
        }
    }

    private void SetFlowerOfferingPanelVisible(bool isVisible)
    {
        if (flowerOfferingPanel != null)
        {
            flowerOfferingPanel.SetActive(isVisible);
        }
    }

    private void UpdateFlowerOfferingUi()
    {
        bool shouldShow = _isFlowerOfferingActive && IsLocalPriestEligible();
        SetFlowerOfferingPanelVisible(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        int requiredSmashes = Mathf.Max(1, smashesRequired);
        if (flowerOfferingProgressSlider != null)
        {
            flowerOfferingProgressSlider.minValue = 0f;
            flowerOfferingProgressSlider.maxValue = requiredSmashes;
            flowerOfferingProgressSlider.value = Mathf.Clamp(_offeringSmashCount, 0, requiredSmashes);
        }

        if (flowerOfferingTimerText != null)
        {
            float remaining = Mathf.Max(0f, (float)(_offeringEndTime - PhotonNetwork.Time));
            flowerOfferingTimerText.text = Mathf.CeilToInt(remaining).ToString();
        }

        SetPromptVisible(true, "Smash F");
    }

    private void UpdateTaskUI()
    {
        if (taskText != null)
        {
            taskText.text = _taskValue.ToString();
        }
    }

    private int GetSharedFlowerCount()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return 0;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PlayerControls.FlowerCountRoomPropertyKey, out object value) && value is int flowerCount)
        {
            return Mathf.Max(0, flowerCount);
        }

        return 0;
    }

    private void SetSharedFlowerCount(int flowerCount)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { PlayerControls.FlowerCountRoomPropertyKey, Mathf.Max(0, flowerCount) }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(updatedProperties);
    }

    private bool IsPriestActor(int actorNumber)
    {
        PlayerControls[] players = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        foreach (PlayerControls player in players)
        {
            if (player == null || player.photonView == null)
            {
                continue;
            }

            if (player.photonView.OwnerActorNr == actorNumber)
            {
                return player.IsPriest;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        _localPlayerInRange = null;
        _holdProgress = 0f;

        StopBellCooldown();
        StopSecondaryCooldown();
        StopShrineCooldown();

        SetPromptVisible(false, string.Empty);
        SetFlowerOfferingPanelVisible(false);
    }
}
