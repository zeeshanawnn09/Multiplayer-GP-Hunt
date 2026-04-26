using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField usernameInput;

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    void Start()
    {
    }

// ------------------------------------------Registration method-------------------------------------------------------------
    public void UserRegister()
    {
        if (passwordInput.text.Length < 8)
        {
            Debug.LogError("Password must be at least 8 characters long.");
            return;
        }
        var request = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
            Username = usernameInput.text,
            RequireBothUsernameAndEmail = true
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterFailure);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log($"Registration successful! Username:{result.Username}");
        LoadLobbyScene();
    }

    void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError("Registration failed: " + error.ErrorMessage);
    }

// ----------------------------------------------------Login method-------------------------------------------------------------

    public void UserLogin()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"Login successful! User ID: {result.PlayFabId}");
        LoadLobbyScene();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("Login failed: " + error.ErrorMessage);
    }

// ----------------------------------------------------Password reset method-------------------------------------------------------------
    public void ResetPassword()
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = emailInput.text,
            TitleId = "65710"
        };

        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnPasswordResetSuccess, OnPasswordResetFailure);
    }

    void OnPasswordResetSuccess(SendAccountRecoveryEmailResult result)
    {
        Debug.Log("Password reset email sent successfully.");
    }

    void OnPasswordResetFailure(PlayFabError error)
    {
        Debug.LogError("Failed to send password reset email: " + error.ErrorMessage);
    }

// ----------------------------------------------------Password reset method-------------------------------------------------------------
    public void GuestLogin()
    {
        Debug.Log("Guest button pressed. Loading lobby scene directly.");
        LoadLobbyScene();
    }

    private void LoadLobbyScene()
    {
        if (string.IsNullOrWhiteSpace(lobbySceneName))
        {
            Debug.LogError("Lobby scene name is empty on PlayFabManager.");
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
    }
}
