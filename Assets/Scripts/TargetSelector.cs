using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CombatSystem; // Asegúrate que este namespace exista

public class TargetSelector : MonoBehaviour
{
    public static TargetSelector Instance { get; private set; }

    [Header("UI References")]
    public GameObject root; // El GameObject raíz de la UI del selector (puede estar vacío si solo controla lógica)
    // Podrías añadir aquí una referencia al cursor si TargetSelector lo gestiona directamente,
    // pero actualmente parece que lo gestionan los EnemyWorldAnchor/AllyWorldAnchor.

    // --- Sonido (Opcional) ---
    [Header("Audio (Opcional)")]
    public AudioSource audioSource; // Asignar o se buscará
    public AudioClip moveSound;
    public AudioClip confirmSound;
    public AudioClip cancelSound;
    // --------------------------

    // Evento y estado interno
    private Action<object> onTargetConfirmed; // Callback a llamar al confirmar
    private CharacterStats currentCharacter;  // Personaje que está seleccionando

    // Estructura de datos para la parrilla lógica de objetivos
    private List<SelectableTarget> gridTargets = new List<SelectableTarget>();
    private int currentCol = 0; // Columna actual seleccionada
    private int currentRow = 0; // Fila actual seleccionada
    private SelectableTarget currentSelectableTarget = null; // Objetivo actual

    // Info sobre la estructura de la parrilla
    private int numberOfColumns = 0; // Cuántas columnas hay activas
    private Dictionary<int, int> maxRowsInColumn = new Dictionary<int, int>(); // Fila máxima (índice) por columna

    // Clase interna para manejar cada objetivo seleccionable
    private class SelectableTarget
    {
        public Transform anchor;            // Punto de anclaje visual (para el cursor/efectos)
        public object owner;                // Referencia al CharacterStats o EnemyInstance
        public int column;                  // Columna lógica
        public int row;                     // Fila lógica
        public EnemyWorldAnchor enemyAnchor { get; private set; } // Cache del componente visual enemigo
        public AllyWorldAnchor allyAnchor { get; private set; }   // Cache del componente visual aliado

        public SelectableTarget(Transform anchor, object owner, int column, int row)
        {
            this.anchor = anchor;
            this.owner = owner;
            this.column = column;
            this.row = row;
            // Intentar obtener y cachear los componentes visuales si el anchor existe
            if (anchor != null)
            {
                if (owner is EnemyInstance) this.enemyAnchor = anchor.GetComponentInParent<EnemyWorldAnchor>(); // Buscar en padre por si anchor es hijo
                else if (owner is CharacterStats) this.allyAnchor = anchor.GetComponentInParent<AllyWorldAnchor>(); // Buscar en padre
            }
            if (owner is EnemyInstance && this.enemyAnchor == null && BattleFlowController.Instance != null)
            {
                // Fallback si el anchor no tenía el componente, buscar a través del owner
                // this.enemyAnchor = BattleFlowController.Instance.GetEnemyAnchorForInstance(owner as EnemyInstance); // Necesitaría este método en BFC
            }
            if (owner is CharacterStats && this.allyAnchor == null && BattleFlowController.Instance != null)
            {
                this.allyAnchor = BattleFlowController.Instance.GetVisualAnchorForCharacter(owner as CharacterStats);
            }
        }

        // Aplica/quita los efectos visuales de selección (cursor, flash)
        public void SetSelected(bool isSelected)
        {
            enemyAnchor?.SetCursorVisible(isSelected); // Asume que EnemyWorldAnchor tiene estos métodos
            if (isSelected) enemyAnchor?.StartFlashing(); else enemyAnchor?.StopFlashing();

            allyAnchor?.SetCursorVisible(isSelected); // Asume que AllyWorldAnchor tiene SetCursorVisible
            // allyAnchor?.StartFlashing(); // Añadir si se implementa flash para aliados
            // allyAnchor?.StopFlashing();
        }
    }

    private void Awake()
    {
        // Singleton
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Ocultar UI raíz si existe y está asignada
        if (root != null) root.SetActive(false);
        else Debug.LogWarning("[TargetSelector] GameObject 'root' no asignado (opcional).", this);

        // Configurar AudioSource (si se usa)
        SetupAudioSource();
    }

    // --- Métodos Públicos para Abrir el Selector ---

    /// <summary>
    /// Abre el selector para elegir entre enemigos y aliados vivos.
    /// </summary>
    public void OpenAnyTargets(CharacterStats user, List<EnemyInstance> enemies, List<CharacterStats> allies, Action<object> onTargetChosen)
    {
        Debug.Log($"[TargetSelector] OpenAnyTargets llamado por: {user?.characterName ?? "NULL"}");
        SetupSelector(user, enemies, allies, onTargetChosen);
    }

    /// <summary>
    /// Abre el selector para elegir solo entre enemigos vivos.
    /// </summary>
    public void OpenEnemyTargets(CharacterStats user, List<EnemyInstance> enemies, Action<object> onTargetChosen)
    {
        Debug.Log($"[TargetSelector] OpenEnemyTargets llamado por: {user?.characterName ?? "NULL"}");
        // Llama a Setup pasando una lista vacía de aliados
        SetupSelector(user, enemies, new List<CharacterStats>(), onTargetChosen);
    }

    /// <summary>
    /// Abre el selector para elegir solo entre aliados vivos.
    /// </summary>
    public void OpenAllyTargets(CharacterStats user, List<CharacterStats> allies, Action<object> onTargetChosen)
    {
        Debug.Log($"[TargetSelector] OpenAllyTargets llamado por: {user?.characterName ?? "NULL"}");
        // Llama a Setup pasando una lista vacía de enemigos
        SetupSelector(user, new List<EnemyInstance>(), allies, onTargetChosen);
    }

    // --- Lógica Interna de Preparación ---

    /// <summary>
    /// Método central que configura la estructura lógica (grid) de los objetivos seleccionables.
    /// </summary>
    private void SetupSelector(CharacterStats user, List<EnemyInstance> enemies, List<CharacterStats> allies, Action<object> onTargetChosen)
    {
        // Validar usuario
        if (user == null)
        {
            Debug.LogError("[TargetSelector] SetupSelector: User es null.");
            CloseInternal(false); // Cerrar sin llamar a BFC
            return;
        }
        this.currentCharacter = user;

        // Establecer foco en este UI
        BattleUIFocusManager.Instance?.SetFocus(this);

        // Limpiar estado anterior
        gridTargets.Clear();
        maxRowsInColumn.Clear();

        // Filtrar listas para obtener solo objetivos vivos y válidos
        List<EnemyInstance> aliveEnemies = enemies?.Where(e => e != null && e.IsAlive).ToList() ?? new List<EnemyInstance>();
        List<CharacterStats> aliveAllies = allies?.Where(a => a != null && a.currentHP > 0).ToList() ?? new List<CharacterStats>();

        // Determinar layout de columnas basado en enemigos
        bool useTwoEnemyColumns = aliveEnemies.Count > 3;
        int enemyCol1 = useTwoEnemyColumns ? 1 : 0; // Columna derecha de enemigos (o única)
        int enemyCol0 = useTwoEnemyColumns ? 0 : -1; // Columna izquierda de enemigos (si existe)
        int allyCol = -1; // Columna de aliados, determinar si hay enemigos o no

        // Asignar columna de aliados dependiendo de si hay enemigos
        if (aliveEnemies.Count == 0 && aliveAllies.Count > 0)
        { // Solo aliados
            allyCol = 0; // Usar la primera columna si solo hay aliados
            numberOfColumns = 1;
        }
        else if (aliveAllies.Count > 0)
        { // Hay enemigos Y aliados
            allyCol = useTwoEnemyColumns ? 2 : 1; // Columna más a la derecha
            numberOfColumns = useTwoEnemyColumns ? 3 : 2;
        }
        else
        { // Solo enemigos (o ninguno)
            numberOfColumns = useTwoEnemyColumns ? 2 : (aliveEnemies.Count > 0 ? 1 : 0);
            // allyCol permanece -1
        }


        // --- Construir la Lista gridTargets ---
        // Enemigos
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyInstance enemy = aliveEnemies[i];
            Transform anchor = enemy.targetAnchor ?? enemy.worldTransform; // Usar anchor específico o fallback
            if (anchor == null) { Debug.LogWarning($"Enemy {enemy.enemyData?.enemyName} sin anchor visual."); continue; }

            int col = useTwoEnemyColumns ? ((i < 3) ? enemyCol1 : enemyCol0) : enemyCol1;
            int row = useTwoEnemyColumns ? ((i < 3) ? i : i - 3) : i;

            gridTargets.Add(new SelectableTarget(anchor, enemy, col, row));
            maxRowsInColumn[col] = maxRowsInColumn.ContainsKey(col) ? Mathf.Max(maxRowsInColumn[col], row) : row;
        }
        // Aliados (solo si allyCol es válida)
        if (allyCol != -1)
        {
            for (int i = 0; i < aliveAllies.Count; i++)
            {
                CharacterStats ally = aliveAllies[i];
                Transform anchor = ally.targetAnchor; // Usar el anchor asignado en CharacterStats
                if (anchor == null)
                {
                    // Fallback si no tiene anchor asignado en CharacterStats
                    AllyWorldAnchor visualAnchor = BattleFlowController.Instance?.GetVisualAnchorForCharacter(ally);
                    anchor = visualAnchor?.transform;
                    if (anchor != null) ally.targetAnchor = anchor; // Intentar reasignar por si acaso
                    else { Debug.LogWarning($"Ally {ally.characterName} sin anchor visual."); continue; }
                }

                int col = allyCol;
                int row = i;
                gridTargets.Add(new SelectableTarget(anchor, ally, col, row));
                maxRowsInColumn[col] = maxRowsInColumn.ContainsKey(col) ? Mathf.Max(maxRowsInColumn[col], row) : row;
            }
        }

        // --- Finalizar Setup ---
        // Comprobar si se añadieron objetivos válidos
        if (gridTargets.Count == 0)
        {
            Debug.LogWarning("[TargetSelector] No se encontraron objetivos válidos para seleccionar.");
            CloseInternal(true); // Cerrar y notificar a BFC para volver
            return;
        }

        SelectDefaultTarget(); // Seleccionar objetivo inicial (prioriza enemigos)
        onTargetConfirmed = onTargetChosen; // Guardar callback

        if (root != null) root.SetActive(true); // Activar UI raíz (si existe)
        UpdateCursorVisuals(); // Mostrar cursor/flash en selección inicial
    }

    /// <summary>
    /// Selecciona el objetivo inicial por defecto (generalmente el primer enemigo vivo).
    /// </summary>
    private void SelectDefaultTarget()
    {
        currentSelectableTarget = null;
        // Prioridad 1: Primer enemigo en la columna principal (col 1 si existe, si no col 0)
        int initialEnemyCol = maxRowsInColumn.ContainsKey(1) ? 1 : (maxRowsInColumn.ContainsKey(0) ? 0 : -1);
        if (initialEnemyCol != -1)
        {
            currentSelectableTarget = gridTargets
                .Where(t => t.column == initialEnemyCol && t.owner is EnemyInstance)
                .OrderBy(t => t.row)
                .FirstOrDefault();
        }
        // Prioridad 2: Primer enemigo en la otra columna (si existe y no se encontró en la primaria)
        if (currentSelectableTarget == null)
        {
            int secondaryEnemyCol = (initialEnemyCol == 1 && maxRowsInColumn.ContainsKey(0)) ? 0 : -1;
            if (secondaryEnemyCol != -1)
            {
                currentSelectableTarget = gridTargets
                    .Where(t => t.column == secondaryEnemyCol && t.owner is EnemyInstance)
                    .OrderBy(t => t.row)
                    .FirstOrDefault();
            }
        }
        // Prioridad 3: Primer aliado (si no se encontraron enemigos)
        if (currentSelectableTarget == null)
        {
            int allyCol = -1;
            if (maxRowsInColumn.ContainsKey(2)) allyCol = 2;
            else if (maxRowsInColumn.ContainsKey(1) && !(gridTargets.Any(t => t.column == 1 && t.owner is EnemyInstance))) allyCol = 1; // Col 1 es aliado si no hay enemigos en Col 1
            else if (maxRowsInColumn.ContainsKey(0) && !(gridTargets.Any(t => t.column == 0 && t.owner is EnemyInstance))) allyCol = 0; // Col 0 es aliado si no hay enemigos en Col 0

            if (allyCol != -1)
            {
                currentSelectableTarget = gridTargets
                    .Where(t => t.column == allyCol && t.owner is CharacterStats)
                    .OrderBy(t => t.row)
                    .FirstOrDefault();
            }
        }
        // Fallback absoluto: el primer objetivo de la lista si todo lo demás falla
        if (currentSelectableTarget == null)
        {
            currentSelectableTarget = gridTargets.FirstOrDefault();
        }

        // Establecer fila y columna actuales
        if (currentSelectableTarget != null)
        {
            currentCol = currentSelectableTarget.column;
            currentRow = currentSelectableTarget.row;
            // Debug.Log($"[TargetSelector] Default Selection: Col={currentCol}, Row={currentRow}, Target={GetOwnerName(currentSelectableTarget.owner)}");
        }
        else
        {
            Debug.LogError("[TargetSelector] SelectDefaultTarget: ¡No se pudo seleccionar ningún objetivo!");
            currentCol = 0; currentRow = 0; // Resetear por si acaso
        }
    }

    // --- Manejo de Input ---

    private void Update()
    {
        // Comprobar foco
        if (root == null || !root.activeSelf || BattleUIFocusManager.Instance == null || !BattleUIFocusManager.Instance.CanInteract(this)) return;

        SelectableTarget previousTarget = currentSelectableTarget; // Guardar para comparar si cambió

        // Navegación (WASD o Flechas)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryMove(0, -1);    // Arriba
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryMove(0, 1);  // Abajo
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(-1, 0); // Izquierda
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryMove(1, 0); // Derecha
        // Confirmación
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
        // Cancelación
        else if (Input.GetKeyDown(KeyCode.Backspace)) Cancel(); // Llama a Cancel()

        // Actualizar visuales solo si la selección cambió
        if (previousTarget != currentSelectableTarget)
        {
            PlaySound(moveSound); // Reproducir sonido de movimiento
            UpdateCursorVisuals(previousTarget); // Actualizar cursor/flash
        }
    }

    /// <summary>
    /// Intenta moverse en la dirección dada (dx, dy) y actualiza la selección.
    /// </summary>
    private void TryMove(int dx, int dy)
    {
        SelectableTarget nextTarget = FindNextTarget(dx, dy);
        if (nextTarget != null && nextTarget != currentSelectableTarget)
        { // Si encontró un target diferente
            // Actualizar estado interno
            currentCol = nextTarget.column;
            currentRow = nextTarget.row;
            currentSelectableTarget = nextTarget;
            // La actualización visual se hará en Update al detectar el cambio
        }
        // else: No se encontró un target válido o es el mismo, no hacer nada
    }

    /// <summary>
    /// Lógica de navegación que encuentra el siguiente objetivo válido en la parrilla.
    /// Incluye wrap-around vertical y horizontal.
    /// </summary>
    private SelectableTarget FindNextTarget(int dx, int dy)
    {
        if (currentSelectableTarget == null || gridTargets.Count == 0) return gridTargets.FirstOrDefault();
        if (dx == 0 && dy == 0) return currentSelectableTarget;

        // Movimiento Vertical
        if (dy != 0)
        {
            int targetRow = currentRow + dy;
            int maxRow = maxRowsInColumn.ContainsKey(currentCol) ? maxRowsInColumn[currentCol] : -1;
            if (maxRow == -1) return currentSelectableTarget; // No targets en esta columna

            // Wrap Around Vertical
            if (targetRow < 0) targetRow = maxRow;
            else if (targetRow > maxRow) targetRow = 0;

            // Buscar en la fila destino (o envuelta)
            return gridTargets.FirstOrDefault(t => t.column == currentCol && t.row == targetRow) ?? currentSelectableTarget;
        }

        // Movimiento Horizontal
        if (dx != 0)
        {
            List<int> activeColumns = maxRowsInColumn.Keys.OrderBy(c => c).ToList();
            if (activeColumns.Count <= 1) return currentSelectableTarget; // No hay a dónde moverse horizontalmente

            int currentColumnIndex = activeColumns.IndexOf(currentCol);
            int targetColumnIndex = (currentColumnIndex + dx + activeColumns.Count) % activeColumns.Count; // Índice con wrap around
            int targetCol = activeColumns[targetColumnIndex];

            // Encontrar el objetivo más cercano verticalmente en la nueva columna
            var targetsInTargetCol = gridTargets.Where(t => t.column == targetCol).ToList();
            if (targetsInTargetCol.Any())
            {
                // Ordenar por cercanía vertical a la fila actual y tomar el primero
                return targetsInTargetCol.OrderBy(t => Mathf.Abs(t.row - currentRow)).First();
            }
        }

        return currentSelectableTarget; // No se movió si no encontró nada
    }

    /// <summary>
    /// Actualiza los efectos visuales (cursor/flash) del objetivo anterior y el actual.
    /// </summary>
    private void UpdateCursorVisuals(SelectableTarget previousTarget = null)
    {
        previousTarget?.SetSelected(false);    // Desactivar visuales del anterior
        currentSelectableTarget?.SetSelected(true); // Activar visuales del actual
    }

    /// <summary>
    /// Confirma el objetivo seleccionado actualmente y llama al callback.
    /// </summary>
    private void Confirm()
    {
        if (currentSelectableTarget != null && onTargetConfirmed != null)
        {
            PlaySound(confirmSound); // Sonido confirmación
            string ownerName = GetOwnerName(currentSelectableTarget.owner);
            // Debug.Log($"[TargetSelector] Confirmed: Target='{ownerName}'. Calling callback.");
            // Guardar callback por si CloseInternal lo limpia antes de Invoke
            Action<object> savedCallback = onTargetConfirmed;
            object selectedOwner = currentSelectableTarget.owner;
            CloseInternal(false); // Cerrar SIN notificar a BFC (el callback lo hará)
            savedCallback.Invoke(selectedOwner); // Invocar callback
        }
        else
        {
            Debug.LogWarning("[TargetSelector] Confirm: No target/callback.");
            PlaySound(cancelSound); // Sonido de error/cancelación
        }
    }

    /// <summary>
    /// Cancela la selección, cierra el panel y vuelve al menú de comandos.
    /// </summary>
    private void Cancel()
    {
        PlaySound(cancelSound); // Sonido cancelación
        CloseInternal(true); // Cerrar y notificar a BFC para volver
    }

    /// <summary>
    /// Método público para cerrar el selector desde fuera si es necesario.
    /// </summary>
    public void Close()
    {
        CloseInternal(false); // Por defecto, cerrar sin volver a BFC (asume que quien llama maneja eso)
    }

    /// <summary>
    /// Lógica interna para cerrar el panel, limpiar estado y foco.
    /// </summary>
    /// <param name="notifyBfcToReturn">Si es true, llama a ReturnToCommandSelection en BattleFlowController.</param>
    private void CloseInternal(bool notifyBfcToReturn)
    {
        // Detener efectos visuales del último objetivo
        currentSelectableTarget?.SetSelected(false);

        // Limpiar estado interno
        gridTargets.Clear();
        maxRowsInColumn.Clear();
        currentSelectableTarget = null;
        numberOfColumns = 0;
        onTargetConfirmed = null; // Limpiar callback
        CharacterStats characterThatOpened = currentCharacter; // Guardar referencia antes de limpiar
        currentCharacter = null;

        // Ocultar UI raíz (si existe)
        if (root != null) root.SetActive(false);

        // Limpiar foco
        BattleUIFocusManager.Instance?.ClearFocus(this); // <-- Liberar foco

        // Notificar a BattleFlowController para volver al menú de comandos (si se indicó)
        if (notifyBfcToReturn)
        {
            BattleFlowController.Instance?.ReturnToCommandSelection(characterThatOpened);
        }
    }

    // --- Helpers ---

    /// <summary>
    /// Configura el AudioSource buscando alternativas si no está asignado.
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null && BattleCommandUI.Instance?.audioSource != null)
        {
            audioSource = BattleCommandUI.Instance.audioSource;
        }
        else if (audioSource == null && TurnManager.Instance != null && TurnManager.Instance.TryGetComponent<AudioSource>(out var tmSource))
        {
            audioSource = tmSource;
        }
        else if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
        }
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Reproduce un sonido usando el AudioSource configurado.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Obtiene un nombre legible del dueño de un SelectableTarget.
    /// </summary>
    private string GetOwnerName(object owner)
    {
        return owner switch
        {
            CharacterStats pc => pc?.characterName ?? "Ally?",
            EnemyInstance en => en?.enemyData?.enemyName ?? "Enemy?",
            _ => "Unknown"
        };
    }

} // Fin de la clase TargetSelector