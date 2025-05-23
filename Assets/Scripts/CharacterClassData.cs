using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterClass", menuName = "RPG/CharacterClass")]
public class CharacterClassData : ScriptableObject
{
    public CharacterJob characterJob;
    public Sprite classSprite; // Para selección
    public Sprite portrait;    // Para menús y UI generales
    public string className;
    [TextArea(2, 4)] public string classDescription;

    [Header("Base Stats (Level 1)")]
    public int baseHP;
    public int baseMP;
    public int strength;
    public int defense;
    public int intelligence;
    public int agility;

    [Header("Growth Per Level")]
    public int hpGrowth;
    public int mpGrowth;
    public int strengthGrowth;
    public int defenseGrowth;
    public int intelligenceGrowth;
    public int agilityGrowth;

    [Header("Allowed Equipment Types")]
    public WeaponType[] allowedWeaponTypes;
    public ArmorType[] allowedArmorTypes;

    [Header("Learnable Abilities")]
    public List<LearnableAbility> learnableAbilities = new();

    [Header("Combat Visual")]
    public Sprite battleSprite;

    [Header("Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip travelAnimation;
    public AnimationClip attackAnimation;
    public AnimationClip receivedDamageAnimation; // NUEVO: Animación al recibir daño
    public AnimationClip deadAnimation;

    [Header("Audio")]
    public AudioClip attackSFX;
    [Header("Travel VFX")] // Encabezado ajustado
    [Tooltip("Prefab (con un ParticleSystem) a instanciar durante el movimiento de 'Travel' de esta clase.")]
    public GameObject travelVFXPrefab; // Campo para el prefab específico

}
