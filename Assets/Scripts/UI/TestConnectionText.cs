using TMPro;
using UnityEngine;
using Photon.Pun;

public class TestConnectionText : MonoBehaviourPunCallbacks
{
    public TMP_Text text;
    public TMP_Text iMText;
    public TMP_Text ownerText;
    public TMP_Text roleText;
    public TMP_Text healthText;
    public TMP_Text flowerCountText;

    public static GameObject TestUI;

    private void Awake()
    {
        TestUI = this.gameObject;
        Debug.Log("TestConnectionText.TestUI initialized in Awake");
    }

    private void Start()
    {
        // Ensure TestUI is set (redundant but safe)
        if (TestUI == null)
        {
            TestUI = this.gameObject;
            Debug.LogWarning("TestUI was null in Start, setting it now");
        }
    }

    // Update is called once per frame
    void Update()
    {
        string roomName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "None";
        text.text = "Connection status: " + PhotonNetwork.NetworkClientState + " Room: " + roomName;
        
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
        if (iMText != null)
        {
            iMText.text = "IsMine: " + input;
        }
    }

    //Display current owner of GameObject
    public void DisplayOwner(string input)
    {
        if (ownerText != null)
        {
            ownerText.text = "Owner: " + input;
        }
    }

    //Display player's assigned role
    public void DisplayRole(string role)
    {
        if (roleText != null)
        {
            roleText.text = "Role: " + role;
        }
    }

    public void DisplayHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + currentHealth + "/" + maxHealth;
        }
    }

    public void DisplayFlowerCount(int currentCount)
    {
        if (flowerCountText != null)
        {
            flowerCountText.text = "Flowers: " + currentCount;
        }
    }

    public void ClearFlowerCount()
    {
        if (flowerCountText != null)
        {
            flowerCountText.text = string.Empty;
        }
    }
}
