using TMPro;
using UnityEngine;
using Photon.Pun;

public class TestConnectionText : MonoBehaviourPunCallbacks
{
    public TMP_Text text;
    public TMP_Text iMText;
    public TMP_Text ownerText;

    public static GameObject TestUI;

    private void Start()
    {
        TestUI = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Connection status: " + PhotonNetwork.NetworkClientState + " Room: " + PhotonNetwork.CurrentRoom.Name;
        //iMText.text = "IsMine: " + photonView.IsMine;
        
    }

    public void ChangeColour()
    {
        text.color = Color.green;
        Invoke("ChangeBack", 1f);
    }

    void ChangeBack()
    {
        text.color = Color.white;
    }

    public void DisplayView(bool input)
    {
        iMText.text = "IsMine: " + input;
    }

    public void DisplayOwner(string input)
    {
        ownerText.text = "Owner: " + input;
    }
}
