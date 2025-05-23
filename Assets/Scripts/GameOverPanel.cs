using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    public static GameOverPanel Instance { get; private set; }

    [Header("UI References")]
    public GameObject root;
    public TMP_Text messageText;
    public SaveSlotPanelUI gameOverSaveSlotPanel;

    [Header("Buttons")]
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button exitGameButton;

    [Header("Settings")]
    [Tooltip("Nombre de la escena a la que volver si se 'Sale del juego' (ej. Login o MainMenu)")]
    [SerializeField] private string sceneToLoadOnExit = "1_MainMenu";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (root != null)
        {
            root.SetActive(false);
        }
        else
        {
            Debug.LogError("GameOverPanel: El objeto 'root' del panel no está asignado.", this);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }
    }

    public void Show()
    {
        if (root == null)
        {
            Debug.LogError("GameOverPanel: El objeto 'root' del panel no está asignado y no se puede mostrar.", this);
            return;
        }
        root.SetActive(true);

        BattleUIFocusManager.Instance?.SetFocus(this);

        if (messageText != null)
        {
            messageText.text = "GAME OVER";
        }

        if (gameOverSaveSlotPanel != null && gameOverSaveSlotPanel.gameObject.activeSelf)
        {
            gameOverSaveSlotPanel.Close();
        }
    }

    private void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
        if (gameOverSaveSlotPanel != null && gameOverSaveSlotPanel.gameObject.activeSelf)
        {
            gameOverSaveSlotPanel.Close();
        }
    }

    private void OnLoadGameClicked()
    {
        Debug.Log("GameOverPanel: Botón 'Cargar Partida' presionado.");

        LoadGameManager loadManager = FindObjectOfType<LoadGameManager>();
        if (loadManager != null)
        {
            if (gameOverSaveSlotPanel != null)
            {
                loadManager.ShowLoadPanel(gameOverSaveSlotPanel);
            }
            else
            {
                Debug.LogError("GameOverPanel: gameOverSaveSlotPanel no está asignado en el Inspector de GameOverPanel. No se puede mostrar el panel de carga.");
            }
        }
        else
        {
            Debug.LogError("GameOverPanel: LoadGameManager (buscado con FindObjectOfType) no encontrado en la escena.");
        }
    }

    private void OnExitGameClicked()
    {
        Debug.Log("GameOverPanel: Botón 'Salir del Juego' presionado.");
        BattleUIFocusManager.Instance?.ClearFocus(this);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.Logout();
            Debug.Log("GameOverPanel: Sesión limpiada (token borrado).");
        }
        else
        {
            Debug.LogWarning("GameOverPanel: SessionManager.Instance no encontrado. No se pudo limpiar la sesión.");
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusicWithFade();
            Debug.Log("GameOverPanel: Música de derrota detenida (o intentando detener).");
        }
        else
        {
            Debug.LogWarning("GameOverPanel: MusicManager.Instance no disponible para detener la música de derrota.");
        }

        Hide();

        Debug.Log($"GameOverPanel: Intentando cargar la escena de salida: {sceneToLoadOnExit}");
        if (SceneTransition.Instance != null && !string.IsNullOrEmpty(sceneToLoadOnExit))
        {
            SceneTransition.Instance.LoadScene(sceneToLoadOnExit, SceneTransition.TransitionContext.Generic);
        }
        else
        {
            Debug.LogWarning($"GameOverPanel: SceneTransition.Instance no disponible o sceneToLoadOnExit no configurada. Cargando escena '{sceneToLoadOnExit}' directamente como fallback o saliendo de la aplicación.");
            if (!string.IsNullOrEmpty(sceneToLoadOnExit))
            {
                SceneManager.LoadScene(sceneToLoadOnExit);
            }
            else
            {
                Debug.LogError("GameOverPanel: No hay escena de salida configurada y SceneTransition no está disponible. Intentando Application.Quit().");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
                    Debug.Log("GameOverPanel: Application.Quit() no es efectivo en WebGL. El usuario debe cerrar la pestaña/ventana.");
#else
                    Application.Quit();
#endif
            }
        }
    }

    private void OnDisable()
    {
        if (BattleUIFocusManager.Instance != null && BattleUIFocusManager.Instance.activeFocusSources.Contains(this))
        {
            BattleUIFocusManager.Instance.ClearFocus(this);
        }
    }

    private void OnDestroy()
    {
        if (loadGameButton != null)
        {
            loadGameButton.onClick.RemoveAllListeners();
        }
        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveAllListeners();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}