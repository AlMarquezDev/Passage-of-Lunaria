using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Screen References")]
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject signupScreen;
    [SerializeField] private GameObject lastScreen;

    [Header("Persistent Elements")]
    [SerializeField] private GameObject persistentElements;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private float screenTransitionDelay = 0.1f;

    private void Awake()
    {
        // Si no se asigna un AudioSource, se crea uno dinámicamente.
        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();

        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0;
    }

    private void Start()
    {
        InitializeMenu();
    }

    /// <summary>
    /// Inicializa el menú activando los elementos persistentes y mostrando la pantalla de inicio.
    /// </summary>
    private void InitializeMenu()
    {
        if (persistentElements != null)
            persistentElements.SetActive(true);
        else
            Debug.LogWarning("No se han asignado los elementos persistentes.", this);

        ToggleScreens(startScreen);
    }

    // Métodos de navegación

    public void HandleStartClick()
    {
        StartCoroutine(DelayedScreenTransition(mainMenuScreen));
    }

    public void ShowLogin()
    {
        StartCoroutine(DelayedScreenTransition(loginScreen));
    }

    public void ShowSignup()
    {
        StartCoroutine(DelayedScreenTransition(signupScreen));
    }

    public void ShowLastScreen()
    {
        StartCoroutine(DelayedScreenTransition(lastScreen));
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(DelayedScreenTransition(mainMenuScreen));
    }

    /// <summary>
    /// Transición entre pantallas con un breve retardo.
    /// </summary>
    private IEnumerator DelayedScreenTransition(GameObject screen)
    {
        yield return new WaitForSecondsRealtime(screenTransitionDelay);
        ToggleScreens(screen);
    }

    public void QuitGame()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.Logout();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void StartGame()
    {
        SceneTransition.Instance.LoadScene("2_CharactersSelection");
    }

    /// <summary>
    /// Desactiva todas las pantallas y activa únicamente la pantalla indicada.
    /// </summary>
    private void ToggleScreens(GameObject screenToShow)
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (loginScreen != null) loginScreen.SetActive(false);
        if (signupScreen != null) signupScreen.SetActive(false);
        if (lastScreen != null) lastScreen.SetActive(false);

        if (screenToShow != null)
            screenToShow.SetActive(true);
        else
            Debug.LogWarning("La pantalla a mostrar es nula.", this);
    }
}