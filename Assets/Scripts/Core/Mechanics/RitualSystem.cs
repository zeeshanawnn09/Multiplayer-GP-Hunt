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
    [SerializeField] private GameObject[] bellObjects;

    [Header("Secondary Ritual Setup")]
    [SerializeField] private GameObject secondaryTaskObject;

    [Header("Flower Offering Setup")]
    [SerializeField] private GameObject shrineObject;

    [Header("Ritual Door")]
    [SerializeField] private GameObject ritualDoorClosedModel;
    [SerializeField] private GameObject ritualDoorOpenModel;
    [SerializeField] private Animator ritualDoorAnimator;
    [SerializeField] private string ritualDoorOpenTrigger = "Open";
    [SerializeField] private float ritualDoorOpenAnimationDurationSeconds = 0f;


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
    [SerializeField] private Image taskUnfilledBackgroundImage;
    [SerializeField] private Image taskFilledImage;
    [SerializeField] private int initialTaskValue = 0;

    private const int TotalTaskSteps = 5;

    private const string BellVisibleKey = "BellVisible";
    private const string BellInteractableKey = "BellInteractable";
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
    private bool _isBellInteractable;
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
    private bool _isRitualDoorOpen;
    private bool _hasDoorStateInitialized;
    private int _doorOpenTriggerHash;
    private bool _hasDoorOpenTrigger;
    private Coroutine _ritualDoorOpenCoroutine;

    private void Awake()
    {
        _doorOpenTriggerHash = string.IsNullOrEmpty(ritualDoorOpenTrigger) ? 0 : Animator.StringToHash(ritualDoorOpenTrigger);
        CacheRitualDoorAnimatorParameters();

        if (bellObjects == null || bellObjects.Length == 0)
        {
            if (bellObject != null)
            {
                bellObjects = new[] { bellObject };
            }
        }

        if (bellObjects != null && bellObjects.Length > 0)
        {
            totalRingsRequired = bellObjects.Length;
        }

        SetBellVisibleLocal(false);
        SetSecondaryVisibleLocal(false);
        SetShrineVisibleLocal(false);
        SetPromptVisible(false, string.Empty);
        SetFlowerOfferingPanelVisible(false);
        ApplyRitualDoorVisualState(false);

        _taskValue = initialTaskValue;
        UpdateTaskUI();
    }

    private void CacheRitualDoorAnimatorParameters()
    {
        _hasDoorOpenTrigger = false;

        if (ritualDoorAnimator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = ritualDoorAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                if (_doorOpenTriggerHash != 0 && parameter.nameHash == _doorOpenTriggerHash)
                {
                    _hasDoorOpenTrigger = true;
                }
            }
        }
    }

    private void Start()
    {
        SyncFromRoomProperties();
        _hasDoorStateInitialized = true;
        SetRitualDoorState(false, animateTransition: false);

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            InitializeRoomPropertiesIfNeeded();
            EvaluateObjectiveVisibilityFromMaster();
        }
    }

    private void Update()
    {
        UpdateRitualDoorStateFromLampProgress();

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

        if (_isBellVisible && !_isBellInteractable)
        {
            SetPromptVisible(false, string.Empty);
            return;
        }

        if (_isFlowerOfferingActive)
        {
            HandleFlowerOfferingInput();
            return;
        }

        if (_isBellVisible && _isBellInteractable)
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

    private void UpdateRitualDoorStateFromLampProgress(bool forceInstantState = false)
    {
        bool shouldBeOpen = IsLampRequirementMet();

        if (_isRitualDoorOpen)
        {
            return;
        }

        if (!_hasDoorStateInitialized)
        {
            _hasDoorStateInitialized = true;
            SetRitualDoorState(false, animateTransition: false);
            return;
        }

        if (!shouldBeOpen)
        {
            return;
        }

        SetRitualDoorState(true, animateTransition: !forceInstantState);
    }

    private void SetRitualDoorState(bool isOpen, bool animateTransition)
    {
        _isRitualDoorOpen = isOpen;

        if (!isOpen)
        {
            if (_ritualDoorOpenCoroutine != null)
            {
                StopCoroutine(_ritualDoorOpenCoroutine);
                _ritualDoorOpenCoroutine = null;
            }

            ApplyRitualDoorVisualState(false);
            return;
        }

        if (ritualDoorAnimator == null)
        {
            ApplyRitualDoorVisualState(true);
            return;
        }

        if (_hasDoorOpenTrigger && animateTransition)
        {
            ritualDoorAnimator.SetTrigger(_doorOpenTriggerHash);
        }

        if (_ritualDoorOpenCoroutine != null)
        {
            StopCoroutine(_ritualDoorOpenCoroutine);
        }

        _ritualDoorOpenCoroutine = StartCoroutine(ApplyRitualDoorOpenAfterDelay(animateTransition));
    }

    private System.Collections.IEnumerator ApplyRitualDoorOpenAfterDelay(bool animateTransition)
    {
        float waitSeconds = animateTransition ? GetRitualDoorOpenAnimationDurationSeconds() : 0f;
        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        ApplyRitualDoorVisualState(true);
        _ritualDoorOpenCoroutine = null;
    }

    private void ApplyRitualDoorVisualState(bool isOpen)
    {
        if (ritualDoorClosedModel != null)
        {
            ritualDoorClosedModel.SetActive(!isOpen);
        }

        if (ritualDoorOpenModel != null)
        {
            ritualDoorOpenModel.SetActive(isOpen);
        }
    }

    private float GetRitualDoorOpenAnimationDurationSeconds()
    {
        if (ritualDoorOpenAnimationDurationSeconds > 0f)
        {
            return ritualDoorOpenAnimationDurationSeconds;
        }

        if (ritualDoorAnimator != null && ritualDoorAnimator.runtimeAnimatorController != null)
        {
            AnimationClip[] animationClips = ritualDoorAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < animationClips.Length; i++)
            {
                AnimationClip clip = animationClips[i];
                if (clip != null && (clip.name == "DoorOpenAnim" || clip.name.IndexOf("Open", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return Mathf.Max(0.1f, clip.length);
                }
            }

            if (animationClips.Length > 0 && animationClips[0] != null)
            {
                return Mathf.Max(0.1f, animationClips[0].length);
            }
        }

        return 1f;
    }

    private bool IsAnyObjectiveVisible()
    {
        return GetCurrentTaskStep() < TotalTaskSteps;
    }

    private bool IsBellObjectiveActive()
    {
        int currentTaskStep = GetCurrentTaskStep();
        return currentTaskStep == 0 || currentTaskStep == 4;
    }

    private bool IsSecondaryObjectiveUnlocked()
    {
        int currentTaskStep = GetCurrentTaskStep();
        return currentTaskStep == 1 || currentTaskStep == 3;
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
        return GetCurrentTaskStep() == 2;
    }

    private bool IsFlowerOfferingObjectiveComplete()
    {
        return GetCurrentTaskStep() >= TotalTaskSteps;
    }

    private void EvaluateObjectiveVisibilityFromMaster()
    {
        int currentTaskStep = GetCurrentTaskStep();

        if (currentTaskStep >= TotalTaskSteps)
        {
            if (_isSecondaryVisible)
            {
                SetSecondaryVisible(false);
            }

            if (_isShrineVisible)
            {
                SetShrineVisible(false);
            }

            if (_isBellVisible)
            {
                SetBellInteractable(false);
            }

            return;
        }

        if (currentTaskStep == 0 || currentTaskStep == 4)
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
            if (shouldShowBell)
            {
                SetBellVisible(true);
                SetBellInteractable(true);
            }
            else if (_isBellVisible)
            {
                SetBellInteractable(false);
            }

            return;
        }

        SetBellVisible(true);
        SetBellInteractable(false);

        if (currentTaskStep == 1 || currentTaskStep == 3)
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

        if (currentTaskStep != 2)
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
        int currentTaskStep = GetCurrentTaskStep();
        _completedRings = Mathf.Clamp(_completedRings + 1, 0, totalRingsRequired);
        SetRoomRingCount(_completedRings);
        SetBellInteractable(false);

        bool isObjectiveComplete = !IsBellObjectiveActive();

        if (!isObjectiveComplete)
        {
            return;
        }

        SetRoomTaskValue(_taskValue + 1);

        if (currentTaskStep == 0)
        {
            SetRoomSecondaryCount(0);
            double now = PhotonNetwork.Time;
            SetRoomSecondaryUnlockTime(now + Mathf.Max(0f, secondaryRevealDelaySeconds));
            SetRoomSecondaryVisible(false);
        }
        else if (currentTaskStep == 4)
        {
            SetRoomSecondaryVisible(false);
            SetShrineVisible(false);
        }

        StopBellCooldown();
        EvaluateObjectiveVisibilityFromMaster();
    }

    private void HandleSecondaryInteractionAccepted()
    {
        int currentTaskStep = GetCurrentTaskStep();
        _secondaryInteractionsCompleted = Mathf.Clamp(_secondaryInteractionsCompleted + 1, 0, secondaryInteractionsRequired);
        SetRoomSecondaryCount(_secondaryInteractionsCompleted);
        SetSecondaryVisible(false);

        if (IsSecondaryObjectiveComplete())
        {
            SetRoomTaskValue(_taskValue + 1);
            StopSecondaryCooldown();

            if (currentTaskStep == 1)
            {
                SetShrineVisible(false);
                StartShrineCooldown();
            }
            else if (currentTaskStep == 3)
            {
                SetRoomRingCount(0);
                SetBellVisible(true);
                SetBellInteractable(false);
            }

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
        SetRoomSecondaryCount(0);
        double now = PhotonNetwork.Time;
        SetRoomSecondaryUnlockTime(now + Mathf.Max(0f, secondaryRevealDelaySeconds));
        SetRoomSecondaryVisible(false);
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

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(BellInteractableKey))
        {
            defaults[BellInteractableKey] = false;
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

        if (propertiesThatChanged.TryGetValue(BellInteractableKey, out object bellInteractableObj) && bellInteractableObj is bool bellInteractable)
        {
            SetBellInteractableLocal(bellInteractable);
        }

        if (propertiesThatChanged.TryGetValue(BellRingCountKey, out object ringCountObj) && ringCountObj is int ringCount)
        {
            _completedRings = Mathf.Clamp(ringCount, 0, totalRingsRequired);
            RefreshBellSequenceState();
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

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BellInteractableKey, out object bellInteractableObj) && bellInteractableObj is bool bellInteractable)
        {
            SetBellInteractableLocal(bellInteractable);
        }
        else
        {
            SetBellInteractableLocal(false);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BellRingCountKey, out object ringCountObj) && ringCountObj is int ringCount)
        {
            _completedRings = Mathf.Clamp(ringCount, 0, totalRingsRequired);
        }
        else
        {
            _completedRings = 0;
        }

        RefreshBellSequenceState();

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

    private void SetBellInteractable(bool isInteractable)
    {
        if (!PhotonNetwork.InRoom)
        {
            SetBellInteractableLocal(isInteractable);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable updatedProperties = new Hashtable
        {
            { BellInteractableKey, isInteractable }
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

    private int GetBellSequenceCount()
    {
        return bellObjects != null && bellObjects.Length > 0 ? bellObjects.Length : (bellObject != null ? 1 : 0);
    }

    private GameObject GetBellObject(int index)
    {
        if (bellObjects != null && bellObjects.Length > 0)
        {
            if (index >= 0 && index < bellObjects.Length)
            {
                return bellObjects[index];
            }

            return null;
        }

        return index == 0 ? bellObject : null;
    }

    private void RefreshBellSequenceState()
    {
        int bellCount = GetBellSequenceCount();
        if (bellCount <= 0)
        {
            _isBellVisible = false;
            _isBellInteractable = false;
            return;
        }

        int visibleBellCount = Mathf.Clamp(_completedRings, 0, bellCount);
        bool hasCurrentBell = _completedRings < bellCount;

        for (int i = 0; i < bellCount; i++)
        {
            GameObject currentBell = GetBellObject(i);
            if (currentBell == null)
            {
                continue;
            }

            bool shouldBeVisible = i < visibleBellCount || (hasCurrentBell && i == visibleBellCount && _isBellVisible);
            currentBell.SetActive(shouldBeVisible);

            Collider[] bellColliders = currentBell.GetComponentsInChildren<Collider>(true);
            bool shouldBeInteractable = hasCurrentBell
                && i == visibleBellCount
                && _isBellVisible
                && _isBellInteractable
                && !_isBellCoolingDown
                && IsLampRequirementMet();

            for (int colliderIndex = 0; colliderIndex < bellColliders.Length; colliderIndex++)
            {
                Collider bellCollider = bellColliders[colliderIndex];
                if (bellCollider != null)
                {
                    bellCollider.enabled = shouldBeInteractable;
                }
            }
        }
    }

    private void SetBellVisibleLocal(bool isVisible)
    {
        _isBellVisible = isVisible;

        if (!isVisible)
        {
            _holdProgress = 0f;
        }

        RefreshBellSequenceState();
    }

    private void SetBellInteractableLocal(bool isInteractable)
    {
        _isBellInteractable = isInteractable;

        if (!isInteractable)
        {
            _holdProgress = 0f;
        }

        RefreshBellSequenceState();
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
        if (taskUnfilledBackgroundImage != null)
        {
            taskUnfilledBackgroundImage.enabled = true;
        }

        if (taskFilledImage == null)
        {
            return;
        }

        taskFilledImage.type = Image.Type.Filled;
        taskFilledImage.fillMethod = Image.FillMethod.Horizontal;
        taskFilledImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        int taskProgress = Mathf.Clamp(GetCurrentTaskStep(), 0, TotalTaskSteps);
        int safeTotalSteps = Mathf.Max(1, TotalTaskSteps);
        taskFilledImage.fillAmount = Mathf.Clamp01((float)taskProgress / safeTotalSteps);
    }

    private int GetCurrentTaskStep()
    {
        return Mathf.Clamp(_taskValue - initialTaskValue, 0, TotalTaskSteps);
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
