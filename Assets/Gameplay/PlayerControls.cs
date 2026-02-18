using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviourPunCallbacks
{
    CharacterController controller;

    [SerializeField]
    GameObject camObject;

    public float moveSpeed = 5f;
    public float turnSpeed = 1000f;

    public static GameObject localPlayerInstance;

    private void Awake()
    {
        if (photonView.IsMine)
        {
            localPlayerInstance = this.gameObject;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayView(photonView.IsMine);
    }

    // Update is called once per frame
    void Update()
    {
        //print(photonView.IsMine);
        if (photonView.IsMine)
        {
            ProcessMovement();
            ProcessTurn();
        }
        TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayOwner(photonView.Owner.ToStringFull());
    }

    void ProcessMovement()
    {
        Vector3 moveInput = transform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime + 
            transform.right * Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        controller.Move(moveInput);
        //print(moveInput);
        
    }

    void ProcessTurn()
    {
        float xInput = Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime;
        float yInput = Input.GetAxis("Mouse Y") * turnSpeed * Time.deltaTime;
        transform.Rotate(0,xInput,0);
        camObject.transform.Rotate(-yInput,0,0);
        
    }
    
}