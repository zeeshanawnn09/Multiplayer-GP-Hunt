using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public enum PlayerRole
{
    Priest,
    Ghost
}

public class PlayerControls : MonoBehaviourPunCallbacks
{
    CharacterController controller;

    [SerializeField]
    GameObject camObject;

    [SerializeField]
    Material[] materials;

    public Camera playerCam;

    public PlayerRole playerRole { get; private set; }

    [SerializeField]
    float moveSpeed = 5f;

    [SerializeField]
    float turnSpeed = 5f;

    [SerializeField]
    float cameraSpeed = 5f;

    public static GameObject localPlayerInstance;

    InputSystem_Actions inputActions;
    Vector2 moveInput;
    Vector2 lookInput;
    bool interactPressed;

    private void Awake()
    {

        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;
        inputActions.Player.Interact.performed += _ => interactPressed = true;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        Debug.Log($"Player spawned! IsMine: {photonView.IsMine}, Owner: {photonView.Owner}, ViewID: {photonView.ViewID}");
        
        // Safe null check for TestUI
        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        }
        
        Invoke("FinishInvoke", 0.2f);
        
    }

    // Update is called once per frame
    void Update()
    {
        //Process local player input
        if (photonView.IsMine)
        {
            // Fallback to direct keyboard input if the Interact action is not bound correctly.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            // Debug input values occasionally
            if (Time.frameCount % 120 == 0 && (moveInput != Vector2.zero || lookInput != Vector2.zero))
            {
                Debug.Log($"Input detected - Move: {moveInput}, Look: {lookInput}");
            }
            
            ProcessMovement();
            ProcessTurn();
            
        }
        
        // Safe null check for TestUI
        if (TestConnectionText.TestUI != null && photonView.Owner != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayOwner(photonView.Owner.ToStringFull());
        }
        
    }

    //Move
    void ProcessMovement()
    {
        Vector3 moveVector = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(moveVector * moveSpeed * Time.deltaTime);
        
    }

    //Turn
    void ProcessTurn()
    {
        float xInput = lookInput.x * turnSpeed * Time.deltaTime;
        float yInput = lookInput.y * cameraSpeed * Time.deltaTime;
        
        transform.Rotate(0, xInput, 0);
        camObject.transform.Rotate(-yInput, 0, 0);

        
    }

    //Sets character colour for each player
    [PunRPC]
    public void SetCharacterMat(int matIndex)
    {
        // Bounds check for material index
        if (matIndex >= 0 && matIndex < materials.Length)
        {
            GetComponent<MeshRenderer>().material = materials[matIndex];
            print("MatCall: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }
        else
        {
            Debug.LogWarning($"Material index {matIndex} out of bounds (0-{materials.Length - 1})");
        }
        
        if (photonView.IsMine)
        {
            photonView.RPC("SetCharacterMat", RpcTarget.OthersBuffered, matIndex);
        }
    }

    //Assigns role to player (Priest or Ghost)
    [PunRPC]
    public void AssignPlayerRole(int role)
    {
        playerRole = (PlayerRole)role;
        string localPlayerName = PhotonNetwork.LocalPlayer?.NickName ?? "Unknown";
        Debug.Log($"[RPC] AssignPlayerRole received on client '{localPlayerName}' for player '{photonView.Owner.NickName}': {playerRole} (IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID})");
        
        // Apply role-specific material (0 = Priest, 1 = Ghost)
        if (role < materials.Length)
        {
            GetComponent<MeshRenderer>().material = materials[role];
            Debug.Log($"  → Applied material {role} for role {playerRole}");
        }
        else
        {
            Debug.LogWarning($"  → Role {role} exceeds materials array length {materials.Length}");
        }
        
        // Display role on UI if this is the local player
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
        }
        else
        {
            Debug.Log($"  → Skipping UI update (not local player, this is {photonView.Owner.NickName}'s remote character)");
        }
    }

    //Store reference to local player and attach main camera to it
    void FinishInvoke()
    {
        Debug.Log($"FinishInvoke called. IsMine: {photonView.IsMine}, Owner: {photonView.Owner?.NickName}");
        
        // Safe null check for TestUI
        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        }
        
        if (photonView.IsMine)
        {
            localPlayerInstance = this.gameObject;
            Debug.Log($"Set localPlayerInstance for player {photonView.Owner.NickName}");
            
            // Safely find and attach camera
            GameObject mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam != null)
            {
                AttachCamera(mainCam);
                Debug.Log("Camera attached successfully");
            }
            else
            {
                Debug.LogWarning("MainCamera not found for player!");
            }
        }
        else
        {
            Debug.Log($"Skipping camera attachment - not my player (Owner: {photonView.Owner?.NickName})");
        }
    }

    //Attach camera to player
    public void AttachCamera(GameObject cam)
    {
        cam.transform.SetParent(camObject.transform);
        cam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public bool ConsumeInteractPressed()
    {
        if (!photonView.IsMine || !interactPressed)
        {
            return false;
        }

        interactPressed = false;
        return true;
    }

}