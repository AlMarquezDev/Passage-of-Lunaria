using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("UI References")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Managers")]
    [SerializeField] private MenuManager menuManager;

    private const string loginUrl = "https://rpgapi-dgtn.onrender.com/auth/login";

    private void Start()
    {
        feedbackText.text = "";
        feedbackText.gameObject.SetActive(false);
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(AttemptLogin);
    }

    private void AttemptLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        StartCoroutine(SendLoginRequest(username, password));
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

    private IEnumerator SendLoginRequest(string username, string password)
    {
        loginButton.interactable = false;

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

                yield return new WaitForSeconds(2f); // Esperar antes de limpiar

                usernameInput.text = "";
                passwordInput.text = "";

                if (menuManager != null)
                {
                    menuManager.ShowLastScreen();
                }
                else
                {
                    Debug.LogWarning("MenuManager reference not set in LoginManager.");
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
        else if (request.result == UnityWebRequest.Result.ConnectionError ||
                 request.result == UnityWebRequest.Result.ProtocolError ||
                 request.result == UnityWebRequest.Result.DataProcessingError)
        {
            ShowError("Server unreachable. Please try again later.");
        }
        else
        {
            string serverMessage = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(serverMessage))
            {
                ShowError("Login failed: " + serverMessage);
            }
            else
            {
                ShowError("Login failed: Unexpected server error.");
            }
        }

        loginButton.interactable = true;
        request.Dispose();
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