using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    public string authToken { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (string.IsNullOrEmpty(authToken))
            {
                string storedToken = PlayerPrefs.GetString("auth_token", null);
                if (!string.IsNullOrEmpty(storedToken))
                {
                    SetToken(storedToken);
                    Debug.Log("Token restaurado desde PlayerPrefs en SessionManager");
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*PARA USAR EL TOKEN:

    string token = SessionManager.Instance.GetToken();
    request.SetRequestHeader("Authorization", "Bearer " + token);
    */

    public void SetToken(string token)
    {
        authToken = token;
        PlayerPrefs.SetString("auth_token", token);
        Debug.Log("Token almacenado en memoria y PlayerPrefs");
    }

    public string GetToken()
    {
        return authToken;
    }

    public void Logout()
    {
        ClearSession();
    }

    public void ClearSession()
    {
        authToken = null;
        PlayerPrefs.DeleteKey("auth_token");
        Debug.Log("Sesión limpiada completamente");
    }

    private void OnApplicationQuit()
    {
        ClearSession();
    }
}