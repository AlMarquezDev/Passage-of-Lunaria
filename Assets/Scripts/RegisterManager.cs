using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using static System.Net.WebRequestMethods;

public class RegisterManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField repeatPasswordInput;

    [Header("UI References")]
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Managers")]
    [SerializeField] private MenuManager menuManager;

    private const string registerUrl = "https://rpgapi-dgtn.onrender.com/auth/register";
    private const string loginUrl = "https://rpgapi-dgtn.onrender.com/auth/login";

    private void Start()
    {
        feedbackText.text = "";
        feedbackText.gameObject.SetActive(false);
        registerButton.onClick.RemoveAllListeners();
        registerButton.onClick.AddListener(AttemptRegister);
    }

    private void AttemptRegister()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        string repeatPassword = repeatPasswordInput.text;

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Username is required");
            return;
        }
        if (username.Length < 4 || username.Length > 16)
        {
            ShowError("Username must be between 4 and 16 characters");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Password is required");
            return;
        }
        if (password.Length < 6 || password.Length > 32)
        {
            ShowError("Password must be between 6 and 32 characters");
            return;
        }
        if (password != repeatPassword)
        {
            ShowError("Passwords do not match");
            return;
        }

        StartCoroutine(SendRegisterRequest(username, password));
    }

    private void ShowError(string message)
    {
        StartCoroutine(DisplayMessage(message, Color.red));
    }

    private void ShowSuccess(string message)
    {
        StartCoroutine(DisplayMessage(message, Color.blue));
    }

    private IEnumerator DisplayMessage(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);

        feedbackText.gameObject.SetActive(false);
    }

    private IEnumerator SendRegisterRequest(string username, string password)
    {
        registerButton.interactable = false;

        var json = JsonUtility.ToJson(new RegisterRequest { username = username, password = password });
        var request = new UnityWebRequest(registerUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
        {
            ShowSuccess("Registration successful!");
            request.Dispose();

            yield return StartCoroutine(SendLoginRequest(username, password));
        }
        else
        {
            ShowError($"Register failed: {request.downloadHandler.text}");
            request.Dispose();
        }

        registerButton.interactable = true;
    }

    private IEnumerator SendLoginRequest(string username, string password)
    {
        var json = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        var request = new UnityWebRequest(loginUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.responseCode == 200)
        {
            string responseText = request.downloadHandler.text;
            LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(responseText);

            if (!string.IsNullOrEmpty(loginResponse.token))
            {
                PlayerPrefs.SetString("auth_token", loginResponse.token);
                PlayerPrefs.Save();
                SessionManager.Instance.SetToken(loginResponse.token);
                ShowSuccess("Login successful!");

                yield return new WaitForSeconds(2f);

                usernameInput.text = "";
                passwordInput.text = "";
                repeatPasswordInput.text = "";

                feedbackText.gameObject.SetActive(false);

                if (menuManager != null)
                {
                    menuManager.ShowLastScreen();
                }
                else
                {
                    Debug.LogWarning("MenuManager reference not set in RegisterManager.");
                }
            }
            else
            {
                ShowError("Login failed: Invalid response from server.");
            }
        }
        else if (request.responseCode == 403)
        {
            ShowError("Invalid username or password.");
        }
        else
        {
            ShowError("Login failed: " + request.error);
        }

        request.Dispose();
    }

    [System.Serializable]
    private class RegisterRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
    }
}