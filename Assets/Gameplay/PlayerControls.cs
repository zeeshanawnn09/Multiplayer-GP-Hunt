using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviourPunCallbacks
{
    CharacterController controller;

    [SerializeField]
    GameObject camObject;

    public float speed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            ProcessMovement();
        }
    }

    void ProcessMovement()
    {
        Vector3 moveInput = transform.forward * Input.GetAxis("Vertical") * speed * Time.deltaTime;
        controller.Move(moveInput);
        
    }

    
}