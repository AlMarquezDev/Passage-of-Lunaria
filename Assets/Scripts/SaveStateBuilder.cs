using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveStateBuilder
{
    public static SaveStateData CreateSaveState(Vector3 playerPosition)
    {
        var saveData = new SaveStateData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = new Vector3Serializable(playerPosition),
            partyMembers = new List<SaveCharacterData>(),
            inventory = new List<SaveItemEntry>()
        };

        foreach (var member in GameManager.Instance.partyMembers)
        {
            var charData = new SaveCharacterData
            {
                name = member.characterName,
                job = member.characterJob.ToString(),

                level = member.level,
                experience = member.currentExp,
                currentHP = member.currentHP,
                maxHP = member.maxHP,
                currentMP = member.currentMP,
                maxMP = member.maxMP,
                strength = member.strength,
                defense = member.defense,
                intelligence = member.intelligence,
                agility = member.agility,

                baseStrength = member.baseStrength,
                baseDefense = member.baseDefense,
                baseIntelligence = member.baseIntelligence,
                baseAgility = member.baseAgility,
                baseMaxHP = member.baseMaxHP,
                baseMaxMP = member.baseMaxMP,
                rightHandID = member.rightHand ? member.rightHand.itemName : null,
                leftHandID = member.leftHand ? member.leftHand.itemName : null,
                headID = member.head ? member.head.itemName : null,
                bodyID = member.body ? member.body.itemName : null,
                accessoryID = member.accessory ? member.accessory.itemName : null,
                knownAbilitiesIDs = new List<string>()
            };

            foreach (var ability in member.knownAbilities)
                charData.knownAbilitiesIDs.Add(ability.abilityName);

            saveData.partyMembers.Add(charData);
        }

        foreach (var entry in InventorySystem.Instance.GetAllItems())
        {
            saveData.inventory.Add(new SaveItemEntry
            {
                itemID = entry.item.itemName,
                quantity = entry.quantity
            });
        }

        return saveData;
    }
}
