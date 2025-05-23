using System.Collections.Generic;
using UnityEngine;

public class BattleRewardProcessor : MonoBehaviour
{
    public static BattleRewardProcessor Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void DistributeRewards(List<CharacterStats> party, List<EnemyInstance> defeatedEnemies)
    {
        int totalExp = 0;
        int totalGil = 0;
        List<ItemBase> droppedItems = new();

        foreach (var enemy in defeatedEnemies)
        {
            totalExp += enemy.enemyData.experienceReward;
            totalGil += enemy.enemyData.goldReward;

            foreach (var drop in enemy.enemyData.dropTable)
            {
                if (Random.value <= drop.dropChance)
                {
                    droppedItems.Add(drop.item);
                    InventorySystem.Instance.AddItem(drop.item, 1);
                }
            }
        }

                int expPerMember = party.Count > 0 ? totalExp / party.Count : 0;
        foreach (var member in party)
        {
            if (member.currentHP > 0)             {
                member.GainExperience(expPerMember);
                Debug.Log($"{member.characterName} gana {expPerMember} EXP.");
            }
        }

                GilManager.Instance.AddGil(totalGil);

        Debug.Log($"Ganaste {totalGil} Gil y {totalExp} EXP.");
        foreach (var item in droppedItems)
        {
            Debug.Log($"Obtuviste: {item.itemName}");
        }

            }
}
