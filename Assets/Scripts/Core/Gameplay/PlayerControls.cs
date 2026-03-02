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

    private void Awake()
    {
        //Moved to FinishInvoke due to ownership transfer after spawning
        /*
        if (photonView.IsMine)
        {
            localPlayerInstance = this.gameObject;
        }
        */

        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;

        DontDestroyOnLoad(this.gameObject);

    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        Invoke("FinishInvoke", 0.2f);
        
    }

    // Update is called once per frame
    void Update()
    {
        //Process local player input
        if (photonView.IsMine)
        {
            ProcessMovement();
            ProcessTurn();
            
        }
        TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayOwner(photonView.Owner.ToStringFull());
        
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
        GetComponent<MeshRenderer>().material = materials[matIndex];
        print("MatCall: " + PhotonNetwork.CurrentRoom.PlayerCount);
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
        print($"Player {photonView.Owner.NickName} assigned role: {playerRole}");
        
        // Apply role-specific material (0 = Priest, 1 = Ghost)
        GetComponent<MeshRenderer>().material = materials[role];
        
        // Display role on UI if this is the local player
        if (photonView.IsMine)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayRole(playerRole.ToString());
        }
    }

    //Store reference to local player and attach main camera to it
    void FinishInvoke()
    {
        TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
        if (photonView.IsMine)
        {
            localPlayerInstance = this.gameObject;
            AttachCamera(GameObject.FindWithTag("MainCamera"));
        }
    }

    //Attach camera to player
    public void AttachCamera(GameObject cam)
    {
        cam.transform.SetParent(camObject.transform);
        cam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

}