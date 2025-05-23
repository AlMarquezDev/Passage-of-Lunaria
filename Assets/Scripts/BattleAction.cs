using UnityEngine;
using CombatSystem;
public enum BattleCommand
{
    Attack,
    Defend,
    Special, Item,
    Flee
}

[System.Serializable]
public class BattleAction
{
    public CharacterStats character { get; private set; }
    public EnemyInstance enemyActor { get; private set; }
    public object actor => character != null ? (object)character : (object)enemyActor;
    public BattleCommand command { get; private set; }
    public object target { get; set; }
    public AbilityData ability { get; private set; }
    public MonsterAbilityData monsterAbility { get; private set; }
    public ConsumableItem item { get; private set; }
    public BattleAction(CharacterStats character, BattleCommand command, object target = null, AbilityData ability = null, ConsumableItem item = null)
    {
        this.character = character;
        this.enemyActor = null; this.command = command;
        this.target = target;
        this.ability = ability;
        this.monsterAbility = null; this.item = item;
    }

    public BattleAction(EnemyInstance enemy, BattleCommand command, object target = null, MonsterAbilityData monsterAbility = null)
    {
        this.character = null; this.enemyActor = enemy; this.command = command;
        this.target = target;
        this.ability = null; this.monsterAbility = monsterAbility; this.item = null;
    }
}