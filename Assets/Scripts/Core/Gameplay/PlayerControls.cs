using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public enum PlayerRole
{
    Priest,
    Ghost
}

public class PlayerControls : MonoBehaviourPunCallbacks, IPunObservable
{
    public const string FlowerCountRoomPropertyKey = "FlowerCount";
    public const int MaxSoulCount = 15;

    CharacterController controller;

    [SerializeField]
    GameObject camObject;

    [Header("Character Visuals")]
    [SerializeField]
    private GameObject priestVisualRoot;

    [SerializeField]
    private GameObject ghostVisualRoot;

    [SerializeField]
    private bool autoFindRoleVisualRoots = true;

    [Header("Animation")]
    [SerializeField]
    private string animatorSpeedParameter = "Speed";

    [SerializeField]
    private float animationSmoothing = 12f;

    [SerializeField]
    private float idleSpeedThreshold = 0.05f;

    [SerializeField]
    private float remoteAnimationSmoothing = 18f;

    [SerializeField]
    private string animatorAttackTriggerParameter = "Attack";

    [SerializeField]
    private Animator priestAnimator;

    [SerializeField]
    private Animator ghostAnimator;

    [SerializeField]
    private bool autoFindRoleAnimators = true;

    public Camera playerCam;

    public PlayerRole playerRole { get; private set; }
    public bool HasAssignedRole { get; private set; }
    public bool IsPriest => HasAssignedRole && playerRole == PlayerRole.Priest;
    public bool IsGhost => HasAssignedRole && playerRole == PlayerRole.Ghost;

    private const string PriestLayerName = "PriestPlayer";
    private const string GhostLayerName = "GhostPlayer";

    [SerializeField]
    float moveSpeed = 5f;

    [SerializeField]
    float turnSpeed = 5f;

    [SerializeField]
    float cameraSpeed = 5f;

    public static GameObject localPlayerInstance;

    [SerializeField]
    private float flowerPickupRayDistance = 5f;

    [SerializeField]
    private float soulPickupRayDistance = 5f;

    [SerializeField]
    private float soulDropRayDistance = 6f;

    [Header("Ghost Vines")]
    [SerializeField]
    private int initialGhostVineCount = 3;

    [SerializeField]
    private string ghostVinePrefabName = "Trap_Wines";

    [SerializeField]
    private float ghostVinePlacementRayDistance = 10f;

    [SerializeField]
    private float ghostVineVerticalOffset = 0.05f;

    [Header("Ghost Blood Pool")]
    [SerializeField]
    private int initialGhostBloodPoolCount = 3;

    [SerializeField]
    private string ghostBloodPoolPrefabName = "BloodPool";

    [SerializeField]
    private float ghostBloodPoolPlacementRayDistance = 10f;

    [SerializeField]
    private float ghostBloodPoolVerticalOffset = 0.05f;

    [SerializeField]
    private float ashPotPickupRayDistance = 5f;

    [Header("Death Spectate")]
    [SerializeField]
    private float spectateRetargetIntervalSeconds = 0.5f;

    [Header("Debug")]
    [SerializeField]
    private bool logImmobilizeTimer = true;

    [Header("Combat")]
    [SerializeField]
    private int projectileDamage = 10;

    [SerializeField]
    private float fireCooldownSeconds = 0.2f;

    [SerializeField]
    private int priestBurstShotLimit = 5;

    [SerializeField]
    private float priestBurstCooldownSeconds = 10f;

    [SerializeField]
    private int ghostBurstShotLimit = 10;

    [SerializeField]
    private float ghostBurstCooldownSeconds = 5f;

    [SerializeField]
    private float priestProjectileRange = 12f;

    [SerializeField]
    private float ghostProjectileRange = 28f;

    [SerializeField]
    private float projectileVisualSpeed = 40f;

    [SerializeField]
    private int initialProjectilePoolSize = 12;

    [SerializeField]
    private int maxProjectilePoolSize = 24;

    [SerializeField]
    private GameObject projectileVisualPrefab;

    private int flowerCount;
    private int soulCount;
    private int ghostVineCount;
    private int ghostBloodPoolCount;
    private int activeDroppedSoulViewId = -1;
    private bool hasAshPot;
    private int carriedAshPotSourcePlayerViewId = -1;
    private bool isDead;
    private bool isImmobilized;
    private Coroutine immobilizeRoutine;
    private Coroutine spectateFollowRoutine;
    private PlayerControls spectateTarget;
    private float nextFireTime;
    private int shotsFiredInCurrentBurst;
    private float burstCooldownEndTime;
    private Transform projectilePoolRoot;
    private int animatorSpeedHash;
    private int animatorAttackTriggerHash;
    private float targetAnimationSpeed;
    private float networkedAnimationSpeed;
    private float smoothedAnimationSpeed;
    private Vector3 previousPosition;
    private readonly Queue<PooledProjectile> availableProjectiles = new Queue<PooledProjectile>();
    private readonly List<PooledProjectile> pooledProjectiles = new List<PooledProjectile>();

    InputSystem_Actions inputActions;
    Vector2 moveInput;
    Vector2 lookInput;
    bool interactPressed;

    private void Awake()
    {
        ResolveRoleVisualRoots();
        ResolveRoleAnimators();

        animatorSpeedHash = Animator.StringToHash(animatorSpeedParameter);
        animatorAttackTriggerHash = Animator.StringToHash(animatorAttackTriggerParameter);

        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;
        inputActions.Player.Interact.performed += _ =>
        {
            if (!photonView.IsMine)
            {
                return;
            }

            interactPressed = true;
            TryPickupInteractables();
        };

        DontDestroyOnLoad(this.gameObject);
    }

    private new void OnEnable()
    {
        base.OnEnable();
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }

    private new void OnDisable()
    {
        base.OnDisable();
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        previousPosition = transform.position;
        EnsureProjectilePool();
        ApplyRoleVisuals();

        Debug.Log($"Player spawned! IsMine: {photonView.IsMine}, Owner: {photonView.Owner}, ViewID: {photonView.ViewID}");

        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        }

        if (photonView.IsMine)
        {
            SyncFlowerCountFromRoom();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SetupCrosshair();
            RefreshFlowerCountUI();
        }

        Invoke("FinishInvoke", 0.2f);
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryFireProjectile();
            }

            HandleSoulDropInput();
            HandleVineDropInput();
            HandleBloodPoolDropInput();

            if (Time.frameCount % 120 == 0 && (moveInput != Vector2.zero || lookInput != Vector2.zero))
            {
                Debug.Log($"Input detected - Move: {moveInput}, Look: {lookInput}");
            }

            ProcessMovement();
            ProcessTurn();
            UpdateBurstCooldownUI();
        }

        UpdateAnimationSpeed();

        if (TestConnectionText.TestUI != null && photonView.Owner != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayOwner(photonView.Owner.ToStringFull());
        }
    }

    private void UpdateBurstCooldownUI()
    {
        if (!photonView.IsMine || TestConnectionText.TestUI == null)
        {
            return;
        }

        TestConnectionText connectionText = TestConnectionText.TestUI.GetComponent<TestConnectionText>();
        if (connectionText == null)
        {
            return;
        }

        float remaining = Mathf.Max(0f, burstCooldownEndTime - Time.time);
        if (remaining > 0f)
        {
            int display = Mathf.CeilToInt(remaining);
            connectionText.DisplayBurstCooldown(display);
        }
        else
        {
            connectionText.ClearBurstCooldown();
        }
    }

    private void UpdateAnimationSpeed()
    {
        Animator activeAnimator = GetActiveRoleAnimator();
        if (activeAnimator == null)
        {
            return;
        }

        float normalizedSpeed;

        if (photonView.IsMine)
        {
            if (isDead || isImmobilized)
            {
                normalizedSpeed = 0f;
            }
            else
            {
                normalizedSpeed = Mathf.Clamp01(moveInput.magnitude);
            }
        }
        else
        {
            normalizedSpeed = Mathf.Clamp01(networkedAnimationSpeed);
        }

        if (normalizedSpeed < idleSpeedThreshold)
        {
            normalizedSpeed = 0f;
        }

        targetAnimationSpeed = normalizedSpeed;

        float smoothing = photonView.IsMine ? animationSmoothing : remoteAnimationSmoothing;

        smoothedAnimationSpeed = Mathf.MoveTowards(
            smoothedAnimationSpeed,
            targetAnimationSpeed,
            Mathf.Max(0.01f, smoothing) * Time.deltaTime);

        activeAnimator.SetFloat(animatorSpeedHash, smoothedAnimationSpeed);
        previousPosition = transform.position;
    }

    private Animator GetActiveRoleAnimator()
    {
        if (!HasAssignedRole)
        {
            return null;
        }

        if (IsPriest)
        {
            return priestAnimator;
        }

        if (IsGhost)
        {
            return ghostAnimator;
        }

        return null;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(targetAnimationSpeed);
            return;
        }

        if (stream.Count > 0)
        {
            networkedAnimationSpeed = (float)stream.ReceiveNext();
        }
    }

    private void PlayAttackAnimation()
    {
        Animator activeAnimator = GetActiveRoleAnimator();
        if (activeAnimator == null || animatorAttackTriggerHash == 0)
        {
            return;
        }

        activeAnimator.ResetTrigger(animatorAttackTriggerHash);
        activeAnimator.SetTrigger(animatorAttackTriggerHash);
    }

    [PunRPC]
    private void RPC_PlayAttackAnimation()
    {
        PlayAttackAnimation();
    }

    private void HandleSoulDropInput()
    {
        if (!HasAssignedRole || !IsGhost || isDead || soulCount <= 0 || HasActiveDroppedSoul || Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.gKey.wasPressedThisFrame)
        {
            return;
        }

        Vector3 dropPosition = GetSoulDropPosition();
        photonView.RPC(nameof(RPC_RequestDropSoul), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, dropPosition);
    }

    private Vector3 GetSoulDropPosition()
    {
        Camera aimCamera = GetAimCamera();
        Vector3 fallbackPosition = transform.position + transform.forward * 1.2f + Vector3.up * 0.25f;

        if (aimCamera == null)
        {
            return fallbackPosition;
        }

        Ray aimRay = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(aimRay, out RaycastHit aimHit, soulDropRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return aimHit.point + Vector3.up * 0.15f;
        }

        Vector3 projectedPosition = aimCamera.transform.position + aimCamera.transform.forward * 2f;
        Ray downRay = new Ray(projectedPosition + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(downRay, out RaycastHit floorHit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return floorHit.point + Vector3.up * 0.15f;
        }

        return fallbackPosition;
    }

    private void HandleVineDropInput()
    {
        if (!HasAssignedRole || !IsGhost || isDead || Keyboard.current == null || ghostVineCount <= 0)
        {
            return;
        }

        if (!Keyboard.current.vKey.wasPressedThisFrame)
        {
            return;
        }

        Vector3 spawnPosition = GetGhostPlacementPosition(ghostVinePlacementRayDistance, ghostVineVerticalOffset);
        Quaternion spawnRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up);

        bool placed = false;
        if (PhotonNetwork.InRoom)
        {
            if (string.IsNullOrWhiteSpace(ghostVinePrefabName))
            {
                Debug.LogWarning("Ghost vine prefab name is empty. Assign a Resources prefab name in PlayerControls.");
                return;
            }

            PhotonNetwork.Instantiate(ghostVinePrefabName, spawnPosition, spawnRotation);
            placed = true;
        }
        else
        {
            GameObject offlinePrefab = Resources.Load<GameObject>(ghostVinePrefabName);
            if (offlinePrefab == null)
            {
                Debug.LogWarning($"Ghost vine prefab '{ghostVinePrefabName}' was not found in Resources for offline placement.");
                return;
            }

            Instantiate(offlinePrefab, spawnPosition, spawnRotation);
            placed = true;
        }

        if (!placed)
        {
            return;
        }

        ghostVineCount = Mathf.Max(0, ghostVineCount - 1);
        Debug.Log($"Ghost vine placed. Remaining vines: {ghostVineCount}");
        RefreshFlowerCountUI();
    }

    private void HandleBloodPoolDropInput()
    {
        if (!HasAssignedRole || !IsGhost || isDead || Keyboard.current == null || ghostBloodPoolCount <= 0)
        {
            return;
        }

        if (!Keyboard.current.bKey.wasPressedThisFrame)
        {
            return;
        }

        Vector3 spawnPosition = GetGhostPlacementPosition(ghostBloodPoolPlacementRayDistance, ghostBloodPoolVerticalOffset);
        Quaternion spawnRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up);

        bool placed = false;
        if (PhotonNetwork.InRoom)
        {
            if (string.IsNullOrWhiteSpace(ghostBloodPoolPrefabName))
            {
                Debug.LogWarning("Ghost blood pool prefab name is empty. Assign a Resources prefab name in PlayerControls.");
                return;
            }

            PhotonNetwork.Instantiate(ghostBloodPoolPrefabName, spawnPosition, spawnRotation);
            placed = true;
        }
        else
        {
            GameObject offlinePrefab = Resources.Load<GameObject>(ghostBloodPoolPrefabName);
            if (offlinePrefab == null)
            {
                Debug.LogWarning($"Ghost blood pool prefab '{ghostBloodPoolPrefabName}' was not found in Resources for offline placement.");
                return;
            }

            Instantiate(offlinePrefab, spawnPosition, spawnRotation);
            placed = true;
        }

        if (!placed)
        {
            return;
        }

        ghostBloodPoolCount = Mathf.Max(0, ghostBloodPoolCount - 1);
        Debug.Log($"Ghost blood pool placed. Remaining pools: {ghostBloodPoolCount}");
        RefreshFlowerCountUI();
    }

    private Vector3 GetGhostPlacementPosition(float rayDistance, float verticalOffset)
    {
        Camera aimCamera = GetAimCamera();
        Vector3 fallbackPosition = transform.position + transform.forward * 1.2f;
        fallbackPosition.y = transform.position.y + verticalOffset;

        if (aimCamera == null)
        {
            return fallbackPosition;
        }

        Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(aimRay, rayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                PlayerControls hitPlayer = hit.collider.GetComponentInParent<PlayerControls>();
                if (hitPlayer != null && hitPlayer == this)
                {
                    continue;
                }

                return hit.point + hit.normal * verticalOffset;
            }
        }

        Vector3 projectedPosition = aimCamera.transform.position + aimCamera.transform.forward * 2f;
        Ray downRay = new Ray(projectedPosition + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(downRay, out RaycastHit floorHit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return floorHit.point + Vector3.up * verticalOffset;
        }

        return fallbackPosition;
    }

    [PunRPC]
    private void RPC_RequestDropSoul(int requestingActorNumber, Vector3 dropPosition)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!TryGetPlayerByActorNumber(requestingActorNumber, out PlayerControls requestingPlayer))
        {
            return;
        }

        if (!requestingPlayer.IsGhost || requestingPlayer.SoulCount <= 0)
        {
            return;
        }

        AISoulSystem soulSystem = AISoulSystem.Instance;
        if (soulSystem == null || !soulSystem.TrySpawnDroppedSoul(dropPosition, requestingActorNumber, out int spawnedSoulViewId))
        {
            return;
        }

        requestingPlayer.RPC_SetDroppedSoulState(true, spawnedSoulViewId);
        requestingPlayer.photonView.RPC(nameof(RPC_SetDroppedSoulState), RpcTarget.AllBuffered, true, spawnedSoulViewId);

        int nextSoulCount = Mathf.Max(0, requestingPlayer.SoulCount - 1);
        requestingPlayer.photonView.RPC(nameof(RPC_SetSoulCount), RpcTarget.AllBuffered, nextSoulCount);
    }

    private bool TryGetPlayerByActorNumber(int actorNumber, out PlayerControls playerControl)
    {
        playerControl = null;

        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        for (int i = 0; i < playerControls.Length; i++)
        {
            PlayerControls candidate = playerControls[i];
            if (candidate == null || candidate.photonView == null)
            {
                continue;
            }

            if (candidate.photonView.OwnerActorNr != actorNumber)
            {
                continue;
            }

            playerControl = candidate;
            return true;
        }

        return false;
    }

    void ProcessMovement()
    {
        if (isImmobilized || isDead || controller == null || !controller.enabled)
        {
            return;
        }

        Vector3 moveVector = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(moveVector * moveSpeed * Time.deltaTime);
    }

    void ProcessTurn()
    {
        if (isDead)
        {
            return;
        }

        float xInput = lookInput.x * turnSpeed * Time.deltaTime;
        float yInput = lookInput.y * cameraSpeed * Time.deltaTime;

        transform.Rotate(0, xInput, 0);
        camObject.transform.Rotate(-yInput, 0, 0);
    }

    private void SetupCrosshair()
    {
        CrosshairUI crosshair = FindFirstObjectByType<CrosshairUI>();

        if (crosshair != null)
        {
            crosshair.ShowCrosshair();
            Debug.Log("Crosshair setup complete for local player");
        }
        else
        {
            Debug.LogWarning("CrosshairUI component not found in scene! Please add the CrosshairUI script to your Canvas Image element.");
        }
    }

    [PunRPC]
    public void SetCharacterMat(int matIndex)
    {
        Debug.Log("SetCharacterMat is deprecated. Player visuals now come from role model child objects.");
    }

    [PunRPC]
    public void AssignPlayerRole(int role)
    {
        playerRole = (PlayerRole)role;
        HasAssignedRole = true;
        ghostVineCount = IsGhost ? Mathf.Max(0, initialGhostVineCount) : 0;
        ghostBloodPoolCount = IsGhost ? Mathf.Max(0, initialGhostBloodPoolCount) : 0;
        ResetBurstFireCycle();
        SyncFlowerCountFromRoom();
        string localPlayerName = PhotonNetwork.LocalPlayer?.NickName ?? "Unknown";
        Debug.Log($"[RPC] AssignPlayerRole received on client '{localPlayerName}' for player '{photonView.Owner.NickName}': {playerRole} (IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID})");

        ResolveRoleVisualRoots();
        ApplyRoleVisuals();

        ApplyRoleLayer();

        if (photonView.IsMine)
        {
            if (TestConnectionText.TestUI != null)
            {
                TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayRole(playerRole.ToString());
                Debug.Log($"  → ✓ SUCCESS: Updated UI for LOCAL player with role: {playerRole}");
            }
            else
            {
                Debug.LogWarning("  → TestUI is null, cannot display role on UI");
            }

            RefreshFlowerCountUI();
        }
        else
        {
            Debug.Log($"  → Skipping UI update (not local player, this is {photonView.Owner.NickName}'s remote character)");
        }
    }

    [PunRPC]
    public void RPC_SetSpawnPosition(Vector3 spawnPosition)
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (controller != null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = wasEnabled;
        }
        else
        {
            transform.position = spawnPosition;
        }
    }

    void FinishInvoke()
    {
        Debug.Log($"FinishInvoke called. IsMine: {photonView.IsMine}, Owner: {photonView.Owner?.NickName}, OwnerActorNr: {photonView.OwnerActorNr}, ViewID: {photonView.ViewID}");

        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        }

        if (photonView.Owner == null)
        {
            Debug.LogWarning($"Skipping camera attachment because PhotonView owner is null on '{name}'. This often means a scene copy exists. Remove any Player prefab from the scene and only spawn via PhotonNetwork.Instantiate.");
            return;
        }

        bool isLocalOwned = PhotonNetwork.LocalPlayer != null && photonView.OwnerActorNr == PhotonNetwork.LocalPlayer.ActorNumber;

        if (isLocalOwned)
        {
            localPlayerInstance = this.gameObject;
            Debug.Log($"Set localPlayerInstance for player {photonView.Owner.NickName}");

            StartCoroutine(AttachMainCameraWithRetry());
        }
        else
        {
            Debug.Log($"Skipping camera attachment - not my player (Owner: {photonView.Owner?.NickName})");
        }
    }

    private System.Collections.IEnumerator AttachMainCameraWithRetry()
    {
        const float timeoutSeconds = 2f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            Camera resolvedCamera = ResolveAttachableCamera();
            if (resolvedCamera != null)
            {
                AttachCamera(resolvedCamera.gameObject);
                Debug.Log($"Camera attached successfully for local player '{photonView.Owner?.NickName}'.");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera fallbackCamera = CreateFallbackMainCamera();
        if (fallbackCamera != null)
        {
            AttachCamera(fallbackCamera.gameObject);
            Debug.LogWarning("No scene camera found for local player. Created runtime fallback camera and attached it.");
            yield break;
        }

        Debug.LogWarning("MainCamera not found for local player after retry timeout and fallback creation failed.");
    }

    private Camera ResolveAttachableCamera()
    {
        if (playerCam != null)
        {
            return playerCam;
        }

        GameObject taggedCameraObject = GameObject.FindWithTag("MainCamera");
        if (taggedCameraObject != null)
        {
            Camera taggedCamera = taggedCameraObject.GetComponent<Camera>();
            if (taggedCamera == null)
            {
                taggedCamera = taggedCameraObject.GetComponentInChildren<Camera>(true);
            }

            if (taggedCamera != null)
            {
                return taggedCamera;
            }
        }

        if (Camera.main != null)
        {
            return Camera.main;
        }

        Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (sceneCameras != null && sceneCameras.Length > 0)
        {
            return sceneCameras[0];
        }

        return null;
    }

    private Camera CreateFallbackMainCamera()
    {
        GameObject fallbackCameraObject = new GameObject("RuntimeMainCamera");
        fallbackCameraObject.tag = "MainCamera";

        Camera fallbackCamera = fallbackCameraObject.AddComponent<Camera>();
        fallbackCameraObject.AddComponent<AudioListener>();
        return fallbackCamera;
    }

    public void AttachCamera(GameObject cam)
    {
        if (camObject == null)
        {
            Debug.LogWarning($"Cannot attach camera for '{name}' because camObject is not assigned on PlayerControls.");
            return;
        }

        cam.transform.SetParent(camObject.transform);
        cam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        playerCam = cam.GetComponent<Camera>();

        if (playerCam == null)
        {
            playerCam = cam.GetComponentInChildren<Camera>(true);
        }
    }

    private void TryPickupInteractables()
    {
        Camera aimCamera = GetAimCamera();
        if (aimCamera == null)
        {
            Debug.LogWarning("[TryPickupInteractables] Camera is null");
            return;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        Debug.Log($"[TryPickupInteractables] Raycasting from camera. Position: {ray.origin}, Direction: {ray.direction}");

        if (TryGetPickupHit(ray, ashPotPickupRayDistance, out RaycastHit ashPotHit))
        {
            Debug.Log($"[TryPickupInteractables] Raycast hit: {ashPotHit.collider.gameObject.name} at distance {ashPotHit.distance:F2}m");
            AshPotPickup ashPotPickup = ashPotHit.collider.GetComponentInParent<AshPotPickup>();
            if (ashPotPickup != null)
            {
                Debug.Log("[TryPickupInteractables] Found AshPotPickup component, requesting pickup");
                ashPotPickup.RequestPickup();
                return;
            }

            Debug.Log($"[TryPickupInteractables] Hit object {ashPotHit.collider.gameObject.name} but no AshPotPickup component");
        }

        if (IsGhost && TryGetPickupHit(ray, soulPickupRayDistance, out RaycastHit soulHit))
        {
            Debug.Log($"[TryPickupInteractables] Raycast hit soul: {soulHit.collider.gameObject.name}");
            SoulPickup soulPickup = soulHit.collider.GetComponentInParent<SoulPickup>();
            if (soulPickup != null)
            {
                Debug.Log("[TryPickupInteractables] Found SoulPickup component, requesting pickup");
                soulPickup.RequestPickup();
                return;
            }
        }

        if (TryGetPickupHit(ray, flowerPickupRayDistance, out RaycastHit flowerHit))
        {
            Debug.Log($"[TryPickupInteractables] Raycast hit flower: {flowerHit.collider.gameObject.name}");
            FlowerPickup flowerPickup = flowerHit.collider.GetComponentInParent<FlowerPickup>();
            if (flowerPickup != null)
            {
                Debug.Log("[TryPickupInteractables] Found FlowerPickup component, requesting pickup");
                flowerPickup.RequestPickup();
            }
        }
    }

    private bool TryGetPickupHit(Ray ray, float distance, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            validHit = default;
            return false;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.collider == null)
            {
                continue;
            }

            PlayerControls hitPlayer = candidate.collider.GetComponentInParent<PlayerControls>();
            if (hitPlayer != null && hitPlayer == this)
            {
                continue;
            }

            validHit = candidate;
            return true;
        }

        validHit = default;
        return false;
    }

    private void TryFireProjectile()
    {
        if (!HasAssignedRole || isDead || Time.time < nextFireTime)
        {
            return;
        }

        if (Time.time < burstCooldownEndTime)
        {
            return;
        }

        int burstShotLimit = GetCurrentRoleBurstShotLimit();
        if (shotsFiredInCurrentBurst >= burstShotLimit)
        {
            shotsFiredInCurrentBurst = 0;
        }

        Camera aimCamera = GetAimCamera();
        if (aimCamera == null)
        {
            return;
        }

        float range = IsGhost ? ghostProjectileRange : priestProjectileRange;
        float clampedRange = Mathf.Max(0.5f, range);

        Ray aimRay = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        Vector3 startPosition = aimCamera.transform.position + aimCamera.transform.forward * 0.35f;
        Vector3 targetPosition = startPosition + aimCamera.transform.forward * clampedRange;

        if (TryGetFirstValidHit(aimRay, clampedRange, out RaycastHit hit))
        {
            targetPosition = hit.point;

            SoulAI targetSoul = hit.collider.GetComponentInParent<SoulAI>();
            HealthSystem targetHealth = hit.collider.GetComponentInParent<HealthSystem>();
            PhotonView hitView = targetHealth != null ? targetHealth.GetComponent<PhotonView>() : null;
            PlayerControls targetPlayer = hit.collider.GetComponentInParent<PlayerControls>();
            bool validRoleTarget = targetPlayer != null
                && targetPlayer.HasAssignedRole
                && ((IsPriest && targetPlayer.IsGhost) || (IsGhost && targetPlayer.IsPriest));

            if (IsPriest && targetSoul != null)
            {
                PhotonView soulView = targetSoul.GetComponent<PhotonView>();
                if (soulView != null)
                {
                    soulView.RPC(nameof(SoulAI.RPC_ApplyDamage), RpcTarget.MasterClient, projectileDamage);
                }
            }
            else if (validRoleTarget && targetHealth != null && hitView != null && hitView.ViewID != photonView.ViewID && hitView.Owner != null)
            {
                hitView.RPC("RPC_ApplyDamage", hitView.Owner, projectileDamage);
            }
        }

        PlayAttackAnimation();
        photonView.RPC(nameof(RPC_PlayAttackAnimation), RpcTarget.All);
        photonView.RPC(nameof(RPC_SpawnProjectileVisual), RpcTarget.All, startPosition, targetPosition, projectileVisualSpeed);
        nextFireTime = Time.time + Mathf.Max(0.05f, fireCooldownSeconds);

        shotsFiredInCurrentBurst++;
        if (shotsFiredInCurrentBurst >= burstShotLimit)
        {
            burstCooldownEndTime = Time.time + GetCurrentRoleBurstCooldownSeconds();
        }
    }

    private int GetCurrentRoleBurstShotLimit()
    {
        if (IsGhost)
        {
            return Mathf.Max(1, ghostBurstShotLimit);
        }

        return Mathf.Max(1, priestBurstShotLimit);
    }

    private float GetCurrentRoleBurstCooldownSeconds()
    {
        if (IsGhost)
        {
            return Mathf.Max(0f, ghostBurstCooldownSeconds);
        }

        return Mathf.Max(0f, priestBurstCooldownSeconds);
    }

    private void ResetBurstFireCycle()
    {
        shotsFiredInCurrentBurst = 0;
        burstCooldownEndTime = 0f;
        nextFireTime = 0f;
    }

    private bool TryGetFirstValidHit(Ray ray, float distance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            PhotonView hitView = hits[i].collider.GetComponentInParent<PhotonView>();
            if (hitView != null && hitView.ViewID == photonView.ViewID)
            {
                continue;
            }

            hit = hits[i];
            return true;
        }

        hit = default;
        return false;
    }

    [PunRPC]
    private void RPC_SpawnProjectileVisual(Vector3 startPosition, Vector3 targetPosition, float travelSpeed)
    {
        EnsureProjectilePool();

        PooledProjectile projectile = GetPooledProjectile();
        if (projectile == null)
        {
            return;
        }

        projectile.Launch(startPosition, targetPosition, travelSpeed, ReturnProjectileToPool);
    }

    private void EnsureProjectilePool()
    {
        if (projectilePoolRoot == null)
        {
            GameObject poolRootObject = new GameObject("ProjectilePool");
            projectilePoolRoot = poolRootObject.transform;
            projectilePoolRoot.SetParent(transform, false);
        }

        int targetSize = Mathf.Max(1, initialProjectilePoolSize);
        while (pooledProjectiles.Count < targetSize)
        {
            CreatePooledProjectile();
        }
    }

    private PooledProjectile GetPooledProjectile()
    {
        while (availableProjectiles.Count > 0)
        {
            PooledProjectile candidate = availableProjectiles.Dequeue();
            if (candidate != null)
            {
                return candidate;
            }
        }

        if (pooledProjectiles.Count < Mathf.Max(initialProjectilePoolSize, maxProjectilePoolSize))
        {
            return CreatePooledProjectile();
        }

        return null;
    }

    private PooledProjectile CreatePooledProjectile()
    {
        GameObject projectileObject;

        if (projectileVisualPrefab != null)
        {
            projectileObject = Instantiate(projectileVisualPrefab, projectilePoolRoot);
        }
        else
        {
            projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.transform.SetParent(projectilePoolRoot, false);
            projectileObject.transform.localScale = Vector3.one * 0.12f;

            Collider colliderComponent = projectileObject.GetComponent<Collider>();
            if (colliderComponent != null)
            {
                Destroy(colliderComponent);
            }
        }

        projectileObject.name = "PooledProjectile";
        projectileObject.SetActive(false);

        PooledProjectile pooledProjectile = projectileObject.GetComponent<PooledProjectile>();
        if (pooledProjectile == null)
        {
            pooledProjectile = projectileObject.AddComponent<PooledProjectile>();
        }

        pooledProjectiles.Add(pooledProjectile);
        availableProjectiles.Enqueue(pooledProjectile);
        return pooledProjectile;
    }

    private void ReturnProjectileToPool(PooledProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        projectile.transform.SetParent(projectilePoolRoot, false);
        projectile.gameObject.SetActive(false);
        availableProjectiles.Enqueue(projectile);
    }

    private Camera GetAimCamera()
    {
        if (playerCam != null)
        {
            return playerCam;
        }

        if (camObject != null)
        {
            playerCam = camObject.GetComponentInChildren<Camera>(true);
            if (playerCam != null)
            {
                return playerCam;
            }
        }

        return Camera.main;
    }

    private void RefreshFlowerCountUI()
    {
        if (!photonView.IsMine || TestConnectionText.TestUI == null)
        {
            return;
        }

        SyncFlowerCountFromRoom();

        TestConnectionText connectionText = TestConnectionText.TestUI.GetComponent<TestConnectionText>();
        if (connectionText == null)
        {
            return;
        }

        if (IsPriest)
        {
            connectionText.DisplayFlowerCount(flowerCount);
            connectionText.ClearSoulCount();
            connectionText.ClearVineCount();
            connectionText.ClearBloodPoolCount();
        }
        else if (IsGhost)
        {
            connectionText.DisplaySoulCount(soulCount, MaxSoulCount);
            connectionText.ClearFlowerCount();
            connectionText.DisplayVineCount(ghostVineCount);
            connectionText.DisplayBloodPoolCount(ghostBloodPoolCount);
        }
        else
        {
            connectionText.ClearFlowerCount();
            connectionText.ClearSoulCount();
            connectionText.ClearVineCount();
            connectionText.ClearBloodPoolCount();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey(FlowerCountRoomPropertyKey))
        {
            SyncFlowerCountFromRoom();
            RefreshFlowerCountUI();
        }
    }

    private void SyncFlowerCountFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(FlowerCountRoomPropertyKey, out object value) && value is int currentCount)
        {
            flowerCount = currentCount;
        }
    }

    [PunRPC]
    public void RPC_SetSoulCount(int value)
    {
        soulCount = Mathf.Clamp(value, 0, MaxSoulCount);

        if (photonView.IsMine)
        {
            RefreshFlowerCountUI();
        }
    }

    [PunRPC]
    public void RPC_SetDroppedSoulState(bool hasDroppedSoul, int droppedSoulViewId)
    {
        activeDroppedSoulViewId = hasDroppedSoul ? droppedSoulViewId : -1;
    }

    public int SoulCount => soulCount;
    public bool HasActiveDroppedSoul => activeDroppedSoulViewId > 0;

    public void TeleportTo(Vector3 worldPosition)
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            transform.position = worldPosition;
            controller.enabled = true;
        }
        else
        {
            transform.position = worldPosition;
        }
    }

    private void ApplyRoleLayer()
    {
        string targetLayerName = playerRole == PlayerRole.Priest ? PriestLayerName : GhostLayerName;
        int targetLayer = LayerMask.NameToLayer(targetLayerName);

        if (targetLayer < 0)
        {
            Debug.LogWarning($"Layer '{targetLayerName}' is missing. Create it in Unity before using the ritual wall.");
            return;
        }

        SetLayerRecursively(gameObject, targetLayer);
    }

    private void SetLayerRecursively(GameObject targetObject, int layer)
    {
        targetObject.layer = layer;

        foreach (Transform child in targetObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public bool ConsumeInteractPressed()
    {
        if (!photonView.IsMine || isDead || !interactPressed)
        {
            return false;
        }

        interactPressed = false;
        return true;
    }

    public void Immobilize(float duration)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
        {
            return;
        }

        if (immobilizeRoutine != null)
        {
            StopCoroutine(immobilizeRoutine);
        }

        immobilizeRoutine = StartCoroutine(ImmobilizeRoutine(duration));
    }

    public bool HasAshPot => hasAshPot;
    public int CarriedAshPotSourcePlayerViewId => carriedAshPotSourcePlayerViewId;

    [PunRPC]
    public void RPC_SetAshPotCarriedState(bool value)
    {
        hasAshPot = value;

        if (!value)
        {
            carriedAshPotSourcePlayerViewId = -1;
        }
    }

    [PunRPC]
    public void RPC_SetAshPotCarriedData(bool value, int sourcePlayerViewId)
    {
        hasAshPot = value;
        carriedAshPotSourcePlayerViewId = value ? sourcePlayerViewId : -1;
    }

    [PunRPC]
    public void RPC_SetDeadState(bool value)
    {
        isDead = value;

        if (photonView.IsMine && IsPriest)
        {
            if (isDead)
            {
                StartSpectateFollow();
            }
            else
            {
                StopSpectateFollow();
            }
        }

        if (!isDead)
        {
            return;
        }

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        interactPressed = false;
    }

    private void ApplyRoleVisuals()
    {
        if (priestVisualRoot != null)
        {
            priestVisualRoot.SetActive(IsPriest);
        }

        if (ghostVisualRoot != null)
        {
            ghostVisualRoot.SetActive(IsGhost);
        }
    }

    private void ResolveRoleVisualRoots()
    {
        if (!autoFindRoleVisualRoots)
        {
            return;
        }

        if (priestVisualRoot == null)
        {
            priestVisualRoot = FindChildByNames("PriestModel", "Priest", "PriestVisual", "PriestRoot");
        }

        if (ghostVisualRoot == null)
        {
            ghostVisualRoot = FindChildByNames("GhostModel", "Ghost", "GhostVisual", "GhostRoot");
        }
    }

    private void ResolveRoleAnimators()
    {
        if (!autoFindRoleAnimators)
        {
            return;
        }

        if (priestAnimator == null && priestVisualRoot != null)
        {
            priestAnimator = priestVisualRoot.GetComponentInChildren<Animator>(true);
        }

        if (ghostAnimator == null && ghostVisualRoot != null)
        {
            ghostAnimator = ghostVisualRoot.GetComponentInChildren<Animator>(true);
        }

        if (priestAnimator == null)
        {
            priestAnimator = GetAnimatorInChildByNames("PriestModel", "Priest", "PriestVisual", "PriestRoot");
        }

        if (ghostAnimator == null)
        {
            ghostAnimator = GetAnimatorInChildByNames("GhostModel", "Ghost", "GhostVisual", "GhostRoot");
        }

        if (priestAnimator == null && ghostAnimator == null)
        {
            Debug.LogWarning("PlayerControls could not find role Animators. Assign priestAnimator and ghostAnimator in the Inspector.");
        }
    }

    private Animator GetAnimatorInChildByNames(params string[] names)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child == null || child == transform)
            {
                continue;
            }

            for (int j = 0; j < names.Length; j++)
            {
                if (string.Equals(child.name, names[j], StringComparison.OrdinalIgnoreCase))
                {
                    return child.GetComponentInChildren<Animator>(true);
                }
            }
        }

        return null;
    }

    private GameObject FindChildByNames(params string[] names)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child == null || child == transform)
            {
                continue;
            }

            for (int j = 0; j < names.Length; j++)
            {
                if (string.Equals(child.name, names[j], StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private void StartSpectateFollow()
    {
        if (spectateFollowRoutine != null)
        {
            StopCoroutine(spectateFollowRoutine);
        }

        spectateFollowRoutine = StartCoroutine(SpectateFollowRoutine());
    }

    private void StopSpectateFollow()
    {
        if (spectateFollowRoutine != null)
        {
            StopCoroutine(spectateFollowRoutine);
            spectateFollowRoutine = null;
        }

        spectateTarget = null;
        AttachCameraToSelf();
    }

    private System.Collections.IEnumerator SpectateFollowRoutine()
    {
        while (isDead)
        {
            PlayerControls nextTarget = FindLivingPriestToSpectate();
            if (nextTarget != spectateTarget)
            {
                spectateTarget = nextTarget;

                if (spectateTarget != null)
                {
                    AttachCameraToTarget(spectateTarget);
                }
                else
                {
                    AttachCameraToSelf();
                }
            }

            float wait = Mathf.Max(0.1f, spectateRetargetIntervalSeconds);
            yield return new WaitForSeconds(wait);
        }

        spectateFollowRoutine = null;
    }

    private PlayerControls FindLivingPriestToSpectate()
    {
        PlayerControls[] allPlayers = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        PlayerControls closestPriest = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerControls candidate = allPlayers[i];
            if (candidate == null || candidate == this)
            {
                continue;
            }

            if (!candidate.HasAssignedRole || !candidate.IsPriest || candidate.isDead)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestPriest = candidate;
            }
        }

        return closestPriest;
    }

    private void AttachCameraToTarget(PlayerControls target)
    {
        if (target == null || playerCam == null)
        {
            return;
        }

        Transform followAnchor = target.camObject != null ? target.camObject.transform : target.transform;
        playerCam.transform.SetParent(followAnchor);
        playerCam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void AttachCameraToSelf()
    {
        if (playerCam == null || camObject == null)
        {
            return;
        }

        playerCam.transform.SetParent(camObject.transform);
        playerCam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private System.Collections.IEnumerator ImmobilizeRoutine(float duration)
    {
        isImmobilized = true;

        float remainingSeconds = Mathf.Max(0f, duration);
        while (remainingSeconds > 0f)
        {
            if (logImmobilizeTimer)
            {
                string ownerName = photonView != null && photonView.Owner != null ? photonView.Owner.NickName : "Unknown";
                Debug.Log($"[ImmobilizeTimer] Player={ownerName}, Remaining={remainingSeconds:F1}s");
            }

            float step = Mathf.Min(1f, remainingSeconds);
            yield return new WaitForSeconds(step);
            remainingSeconds -= step;
        }

        isImmobilized = false;
        immobilizeRoutine = null;

        if (logImmobilizeTimer)
        {
            string ownerName = photonView != null && photonView.Owner != null ? photonView.Owner.NickName : "Unknown";
            Debug.Log($"[ImmobilizeTimer] Player={ownerName}, Remaining=0.0s (released)");
        }
    }
}
