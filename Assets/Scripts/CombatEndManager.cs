using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class CombatEndManager : MonoBehaviour
{
    public static CombatEndManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void CheckForEndOfBattle(List<CharacterStats> party, List<EnemyInstance> enemies)
    {
        if (party == null) party = new List<CharacterStats>();
        if (enemies == null) enemies = new List<EnemyInstance>();

        bool allEnemiesDefeated = !enemies.Any(e => e != null && e.IsAlive);
        bool allPartyDefeated = !party.Any(c => c != null && c.currentHP > 0);

        Debug.Log($"[CombatEndManager] CheckForEndOfBattle - AllEnemiesDefeated: {allEnemiesDefeated}, AllPartyDefeated: {allPartyDefeated}");

        if (allEnemiesDefeated && !allPartyDefeated)
        {
            List<VictoryScreenManager.CharacterStatsSnapshot> partySnapshots = new List<VictoryScreenManager.CharacterStatsSnapshot>();
            foreach (var member in party)
            {
                if (member != null && member.currentHP > 0)
                {
                    partySnapshots.Add(new VictoryScreenManager.CharacterStatsSnapshot(member));
                }
            }

            int totalExpFromEnemies = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.enemyData != null) totalExpFromEnemies += enemy.enemyData.experienceReward;
                else Debug.LogWarning("CombatEndManager: Null enemy or enemyData found while calculating total EXP.");
            }

            int totalGoldFromEnemies = 0;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.enemyData != null) totalGoldFromEnemies += enemy.enemyData.goldReward;
                else Debug.LogWarning("CombatEndManager: Null enemy or enemyData found while calculating total Gold.");
            }

            List<ItemBase> droppedItems = new List<ItemBase>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.enemyData != null && enemy.enemyData.dropTable != null)
                {
                    foreach (var drop in enemy.enemyData.dropTable)
                    {
                        if (drop != null && drop.item != null && Random.value <= drop.dropChance)
                        {
                            droppedItems.Add(drop.item);
                        }
                    }
                }
            }

            if (GilManager.Instance != null)
            {
                GilManager.Instance.AddGil(totalGoldFromEnemies);
            }
            else Debug.LogError("CombatEndManager: GilManager.Instance is NULL! Cannot add Gil.");

            if (InventorySystem.Instance != null)
            {
                foreach (var item in droppedItems)
                {
                    if (item != null) InventorySystem.Instance.AddItem(item, 1);
                }
            }
            else Debug.LogError("CombatEndManager: InventorySystem.Instance is NULL! Cannot add items.");

            var alivePartyMembers = party.Where(p => p != null && p.currentHP > 0).ToList();
            int expPerMember = alivePartyMembers.Count > 0 ? totalExpFromEnemies / alivePartyMembers.Count : 0;

            foreach (var member in alivePartyMembers)
            {
                member.GainExperience(expPerMember);
            }

            OnVictory(party, partySnapshots, totalGoldFromEnemies, totalExpFromEnemies, droppedItems);
        }
        else if (allPartyDefeated)
        {
            Debug.LogError("[CombatEndManager] CONDICIÓN DE DERROTA CUMPLIDA. Llamando a OnDefeat().");
            OnDefeat();
        }
    }

    private void OnVictory(List<CharacterStats> finalizedParty,
                           List<VictoryScreenManager.CharacterStatsSnapshot> partyBeforeRewards,
                           int goldEarned,
                           int totalExpEarned,
                           List<ItemBase> itemsObtained)
    {
        Debug.Log("Victory! All enemies defeated.");

        string garlandEnemyGroupName = "GarlandGroup";
        if (CombatSessionData.Instance != null && CombatSessionData.Instance.enemyGroup != null &&
            CombatSessionData.Instance.enemyGroup.name == garlandEnemyGroupName)
        {
            Debug.Log($"[CombatEndManager] Garland ({garlandEnemyGroupName}) defeated! Showing 'Thank You' screen.");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StopMusicWithFade();
            }
            if (ThankYouScreenUI.Instance != null)
            {
                ThankYouScreenUI.Instance.ShowScreen(() =>
                {
                    Debug.Log("[CombatEndManager] 'Thank You' screen closed. Returning to Main Menu and clearing session.");
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.Logout();
                    }
                    else
                    {
                        Debug.LogWarning("[CombatEndManager] SessionManager.Instance is NULL. Cannot clear session.");
                    }
                    if (SceneTransition.Instance != null)
                    {
                        SceneTransition.Instance.LoadScene("1_MainMenu", SceneTransition.TransitionContext.Generic);
                    }
                    else
                    {
                        SceneManager.LoadScene("1_MainMenu");
                    }
                });
            }
            else
            {
                Debug.LogError("CombatEndManager: ThankYouScreenUI.Instance is NULL! Cannot show 'Thank You' screen. Proceeding with normal victory flow fallback.");
                ContinueNormalVictoryFlow(finalizedParty, partyBeforeRewards, goldEarned, totalExpEarned, itemsObtained);
            }
        }
        else
        {
            ContinueNormalVictoryFlow(finalizedParty, partyBeforeRewards, goldEarned, totalExpEarned, itemsObtained);
        }
    }

    private void ContinueNormalVictoryFlow(List<CharacterStats> finalizedParty,
                                           List<VictoryScreenManager.CharacterStatsSnapshot> partyBeforeRewards,
                                           int goldEarned,
                                           int totalExpEarned,
                                           List<ItemBase> itemsObtained)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayVictoryTheme();
        }
        else Debug.LogWarning("CombatEndManager: MusicManager.Instance is NULL. Cannot play victory theme.");

        if (VictoryScreenManager.Instance != null)
        {
            VictoryScreenManager.Instance.ShowScreen(
                finalizedParty,
                partyBeforeRewards,
                totalExpEarned,
                itemsObtained,
                () =>
                {
                    if (CombatSessionData.Instance != null)
                    {
                        string prevScene = CombatSessionData.Instance.GetPreviousSceneName();
                        if (!string.IsNullOrEmpty(prevScene))
                        {
                            if (SceneTransition.Instance != null)
                            {
                                Debug.Log($"[CombatEndManager] Victory continue: Transitioning to '{prevScene}' via SceneTransition.");
                                SceneTransition.Instance.LoadScene(prevScene, SceneTransition.TransitionContext.FromBattle);
                            }
                            else
                            {
                                Debug.LogError("[CombatEndManager] SceneTransition.Instance es NULL. Cargando escena anterior directamente como fallback.");
                                SceneManager.LoadScene(prevScene);
                                CombatSessionData.Instance.FinalizeReturnToMap();
                            }
                        }
                        else Debug.LogError("[CombatEndManager] No se pudo obtener el nombre de la escena anterior desde CombatSessionData.");
                    }
                    else Debug.LogError("[CombatEndManager] CombatSessionData.Instance es NULL en el callback de victoria.");
                }
            );
        }
        else
        {
            Debug.LogError("CombatEndManager: VictoryScreenManager.Instance is NULL! Cannot show victory screen. Attempting direct return.");
            if (CombatSessionData.Instance != null)
            {
                string prevScene = CombatSessionData.Instance.GetPreviousSceneName();
                if (!string.IsNullOrEmpty(prevScene))
                {
                    if (SceneTransition.Instance != null)
                    {
                        SceneTransition.Instance.LoadScene(prevScene, SceneTransition.TransitionContext.FromBattle);
                    }
                    else
                    {
                        SceneManager.LoadScene(prevScene);
                        CombatSessionData.Instance.FinalizeReturnToMap();
                    }
                }
                else { Debug.LogError("CombatEndManager Fallback: Previous scene name is null."); }
            }
            else { Debug.LogError("CombatEndManager Fallback: CombatSessionData.Instance is null."); }
        }
    }

    private void OnDefeat()
    {
        Debug.Log("Defeat... All characters have been defeated.");
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayDefeatTheme();
        }
        else Debug.LogWarning("CombatEndManager: MusicManager.Instance is NULL. Cannot play defeat theme.");

        if (GameOverPanel.Instance != null)
        {
            Debug.Log("[CombatEndManager] GameOverPanel.Instance ENCONTRADO. Llamando a Show().");
            GameOverPanel.Instance.Show();
        }
        else
        {
            Debug.LogError("CombatEndManager: GameOverPanel.Instance es NULL! No se puede mostrar el panel de Game Over.");
        }
    }
}