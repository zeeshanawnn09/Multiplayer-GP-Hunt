using TMPro;
using UnityEngine;
using Photon.Pun;

public class TestConnectionText : MonoBehaviourPunCallbacks
{
    public TMP_Text text;

    // Update is called once per frame
    void Update()
    {
        text.text = "Connection status: " + PhotonNetwork.NetworkClientState + " Room: " + PhotonNetwork.CurrentRoom.Name;
    }
}
