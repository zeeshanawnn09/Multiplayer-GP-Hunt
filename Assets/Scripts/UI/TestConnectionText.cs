using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;

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
    public TMP_Text pickupPromptText;
    public TMP_Text soulPickupDebugText;

    [SerializeField]
    private bool debugLogs = true;

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

        RefreshFlowerCountFromRoom();
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

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null || !propertiesThatChanged.ContainsKey(PlayerControls.FlowerCountRoomPropertyKey))
        {
            return;
        }

        RefreshFlowerCountFromRoom();
    }

    private void RefreshFlowerCountFromRoom()
    {
        if (flowerCountText == null)
        {
            return;
        }

        int currentCount = 0;
        bool hasFlowerCount = false;

        if (PhotonNetwork.CurrentRoom != null
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PlayerControls.FlowerCountRoomPropertyKey, out object value)
            && value is int storedCount)
        {
            hasFlowerCount = true;
            currentCount = storedCount;
        }

        PlayerControls localPlayer = null;
        if (PlayerControls.localPlayerInstance != null)
        {
            localPlayer = PlayerControls.localPlayerInstance.GetComponent<PlayerControls>();
        }

        if (localPlayer == null || !localPlayer.IsPriest)
        {
            ClearFlowerCount();

            if (debugLogs)
            {
                Debug.Log($"[TestConnectionText] Flower count cleared. Local priest ready={localPlayer != null && localPlayer.IsPriest}, HasValue={hasFlowerCount}, Value={currentCount}");
            }

            return;
        }

        DisplayFlowerCount(currentCount);

        if (debugLogs)
        {
            Debug.Log($"[TestConnectionText] Flower count refreshed to {currentCount} for local priest '{localPlayer.name}'.");
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

    public void DisplayPickupPrompt(string prompt)
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = prompt;
        }
    }

    public void ClearPickupPrompt()
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = string.Empty;
        }
    }

    public void DisplaySoulPickupDebug(string debugMessage)
    {
        if (soulPickupDebugText != null)
        {
            soulPickupDebugText.text = debugMessage;
        }
    }

    public void ClearSoulPickupDebug()
    {
        if (soulPickupDebugText != null)
        {
            soulPickupDebugText.text = string.Empty;
        }
    }
}
