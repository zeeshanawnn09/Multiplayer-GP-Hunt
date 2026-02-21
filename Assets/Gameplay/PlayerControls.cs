using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviourPunCallbacks
{
    CharacterController controller;

    [SerializeField]
    GameObject camObject;

    [SerializeField]
    Material[] materials;

    public Camera playerCam;

    public float moveSpeed = 5f;
    public float turnSpeed = 1000f;

    public static GameObject localPlayerInstance;

    private void Awake()
    {
        //Moved to FinishInvoke due to ownership transfer after spawning
        /*
        if (photonView.IsMine)
        {
            localPlayerInstance = this.gameObject;
        }
        */

        DontDestroyOnLoad(this.gameObject);

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
        Vector3 moveInput = transform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime + 
            transform.right * Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        controller.Move(moveInput);
        
    }

    //Turn
    void ProcessTurn()
    {
        float xInput = Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime;
        float yInput = Input.GetAxis("Mouse Y") * turnSpeed * Time.deltaTime;
        transform.Rotate(0,xInput,0);
        camObject.transform.Rotate(-yInput,0,0);
        
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