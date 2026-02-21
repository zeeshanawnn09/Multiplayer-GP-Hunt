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
        
    }

    //Signify new initial player
    public void ChangeColour()
    {
        text.color = Color.green;
        Invoke("ChangeBack", 1f);
    }

    void ChangeBack()
    {
        text.color = Color.white;
    }

    //Display photonView.IsMine
    public void DisplayView(bool input)
    {
        iMText.text = "IsMine: " + input;
    }

    //Display current owner of GameObject
    public void DisplayOwner(string input)
    {
        ownerText.text = "Owner: " + input;
    }
}
