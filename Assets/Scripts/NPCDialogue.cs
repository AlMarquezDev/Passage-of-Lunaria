// NPCDialogue.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; // Necesario para List<CharacterStats>

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string[] dialogueLines;
    public float interactionDistance = 2.5f;
    public DialogueBubbleUI dialogueBubble;

    [Header("Player Interaction")]
    public string playerTag = "Player";
    public KeyCode interactionKey = KeyCode.Return;

    [Header("Combat Trigger Settings")]
    public bool triggerCombatAfterDialogue = false;
    public string combatSceneName;
    public EnemyGroupData bossEncounterData; // Esto es tu "enemyGroupForThisTrigger"
    public AudioClip bossBattleMusic;

    private Transform playerTransform;
    private PlayerController playerController;
    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
    private int currentDialogueIndex = 0;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError($"NPCDialogue en '{gameObject.name}': No se encontró PlayerController en '{playerObject.name}'.");
            }
        }
        else
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': No se encontró GameObject con tag '{playerTag}'.");
        }

        if (dialogueBubble == null)
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': 'dialogueBubble' no asignado.", this);
            enabled = false;
            return;
        }
        dialogueBubble.HideBubble();
    }

    void Update()
    {
        if (playerTransform == null || dialogueBubble == null || dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerInRange = distanceToPlayer <= interactionDistance;

        bool canCurrentlyInteract = true;
        if (playerController != null)
        {
            canCurrentlyInteract = !playerController.IsMovementLocked() || isDialogueActive;
        }

        if (isPlayerInRange && canCurrentlyInteract && Input.GetKeyDown(interactionKey))
        {
            HandleInteraction();
        }

        if (isDialogueActive && !isPlayerInRange)
        {
            EndDialogue(false);
        }
    }

    private void HandleInteraction()
    {
        if (!isDialogueActive)
        {
            isDialogueActive = true;
            currentDialogueIndex = 0;
            if (playerController != null)
            {
                playerController.SetMovementLock(true);
            }
            ShowDialogueLine(currentDialogueIndex);
        }
        else
        {
            if (dialogueBubble.IsTyping())
            {
                dialogueBubble.SkipTypewriter();
            }
            else
            {
                currentDialogueIndex++;
                if (currentDialogueIndex < dialogueLines.Length)
                {
                    ShowDialogueLine(currentDialogueIndex);
                }
                else
                {
                    EndDialogue(true);
                }
            }
        }
    }

    private void ShowDialogueLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Length)
        {
            EndDialogue(false);
            return;
        }

        dialogueBubble.ShowBubble();
        dialogueBubble.StartTypewriterEffect(dialogueLines[index], OnCurrentLineFinished);
    }

    private void OnCurrentLineFinished()
    {
        // No action needed here for now
    }

    private void EndDialogue(bool completedNormally)
    {
        if (!isDialogueActive && currentDialogueIndex == 0 && !triggerCombatAfterDialogue) return;

        bool wasActive = isDialogueActive;
        isDialogueActive = false;
        currentDialogueIndex = 0;

        if (dialogueBubble != null)
        {
            dialogueBubble.HideBubble();
        }

        if (completedNormally && triggerCombatAfterDialogue && !string.IsNullOrEmpty(combatSceneName) && bossEncounterData != null)
        {
            if (playerController != null && !playerController.IsMovementLocked())
            {
                playerController.SetMovementLock(true);
            }
            InitiateCombat();
        }
        else
        {
            if (wasActive && playerController != null)
            {
                playerController.SetMovementLock(false);
            }
        }
    }

    private void InitiateCombat()
    {
        if (CombatSessionData.Instance == null)
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': CombatSessionData.Instance es null. No se puede iniciar combate.");
            if (playerController != null) playerController.SetMovementLock(false);
            return;
        }
        if (SceneTransition.Instance == null)
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': SceneTransition.Instance es null. No se puede iniciar combate.");
            if (playerController != null) playerController.SetMovementLock(false);
            return;
        }
        if (GameManager.Instance == null || GameManager.Instance.partyMembers == null)
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': GameManager.Instance o partyMembers es null. No se puede iniciar combate.");
            if (playerController != null) playerController.SetMovementLock(false);
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogError($"NPCDialogue en '{gameObject.name}': playerTransform es null. Usando posición del NPC para SavePreCombatState.");
        }


        CombatSessionData.Instance.SavePreCombatState(
            SceneManager.GetActiveScene().name,
            playerTransform != null ? playerTransform.position : transform.position // Usar pos del NPC si playerTransform es null
        );
        CombatSessionData.Instance.enemyGroup = this.bossEncounterData;
        CombatSessionData.Instance.partyMembers = new List<CharacterStats>(GameManager.Instance.partyMembers);
        CombatSessionData.Instance.customBattleMusic = this.bossBattleMusic;

        CombatContext.EnemyGroupToLoad = this.bossEncounterData;

        Debug.Log($"NPCDialogue en '{gameObject.name}': Iniciando combate. Escena: {combatSceneName}, Enemigos: {bossEncounterData.name}, Música Jefe: {(bossBattleMusic != null ? bossBattleMusic.name : "Default")}");
        SceneTransition.Instance.LoadScene(this.combatSceneName, SceneTransition.TransitionContext.ToBattle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}