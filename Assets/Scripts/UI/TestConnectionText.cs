using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class TestConnectionText : MonoBehaviourPunCallbacks
{
    public TMP_Text text;
    public TMP_Text iMText;
    public TMP_Text ownerText;
    public TMP_Text roleText;
    public Image healthUnfilledBackgroundImage;
    public Image healthFilledImage;
    public TMP_Text flowerCountText;
    public TMP_Text soulCountText;
    public TMP_Text vineCountText;
    public TMP_Text bloodPoolCountText;
    public TMP_Text burstCooldownText;

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
        if (healthUnfilledBackgroundImage != null)
        {
            healthUnfilledBackgroundImage.enabled = true;
        }

        if (healthFilledImage == null)
        {
            return;
        }

        healthFilledImage.type = Image.Type.Filled;
        healthFilledImage.fillMethod = Image.FillMethod.Horizontal;
        healthFilledImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        int safeMaxHealth = Mathf.Max(1, maxHealth);
        healthFilledImage.fillAmount = Mathf.Clamp01((float)currentHealth / safeMaxHealth);
    }

    public void DisplayFlowerCount(int currentCount)
    {
        if (flowerCountText != null)
        {
            flowerCountText.text = " " + currentCount;
        }
    }

    public void ClearFlowerCount()
    {
        if (flowerCountText != null)
        {
            flowerCountText.text = string.Empty;
        }
    }

    public void DisplayVineCount(int currentCount)
    {
        if (vineCountText != null)
        {
            vineCountText.text = " " + currentCount;
        }
    }

    public void ClearVineCount()
    {
        if (vineCountText != null)
        {
            vineCountText.text = string.Empty;
        }
    }

    public void DisplayBloodPoolCount(int currentCount)
    {
        if (bloodPoolCountText != null)
        {
            bloodPoolCountText.text = " " + currentCount;
        }
    }

    public void ClearBloodPoolCount()
    {
        if (bloodPoolCountText != null)
        {
            bloodPoolCountText.text = string.Empty;
        }
    }

    public void DisplayBurstCooldown(int secondsRemaining)
    {
        if (burstCooldownText != null)
        {
            burstCooldownText.text = secondsRemaining > 0 ? $"{secondsRemaining}" : string.Empty;
        }
    }

    public void ClearBurstCooldown()
    {
        if (burstCooldownText != null)
        {
            burstCooldownText.text = string.Empty;
        }
    }

    public void DisplaySoulCount(int currentCount, int maxCount)
    {
        if (soulCountText != null)
        {
            soulCountText.text = $"{currentCount}/{maxCount}";
        }
    }

    public void ClearSoulCount()
    {
        if (soulCountText != null)
        {
            soulCountText.text = string.Empty;
        }
    }
}
