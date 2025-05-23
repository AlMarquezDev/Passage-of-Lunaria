using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text; // Necesario para StringBuilder

public class CharacterVictoryDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Image characterSpriteImage;
    public TMP_Text characterNameText;
    public TMP_Text levelText;
    public Slider experienceSlider;
    public TMP_Text experienceValueText;
    public GameObject learnedAbilitiesPanel;
    public TMP_Text learnedAbilitiesText;

    [Header("Animation Settings")]
    public float expFillDurationPerBar = 1.5f;
    public float levelUpPauseDuration = 1.0f;

    private CharacterStats _finalCharacterStats;
    private List<string> _newlyLearnedAbilitiesInThisCombat; // Para acumular habilidades nuevas

    public void PrepareDisplay(CharacterStats finalCharacterStats)
    {
        _finalCharacterStats = finalCharacterStats;
        _newlyLearnedAbilitiesInThisCombat = new List<string>(); // Resetear lista

        if (_finalCharacterStats == null)
        {
            Debug.LogError("CharacterVictoryDisplay: Final CharacterStats is null for PrepareDisplay.");
            gameObject.SetActive(false);
            return;
        }
        if (characterNameText == null || levelText == null || experienceSlider == null || experienceValueText == null || learnedAbilitiesPanel == null || learnedAbilitiesText == null || characterSpriteImage == null)
        {
            Debug.LogError($"CharacterVictoryDisplay for {_finalCharacterStats.characterName}: One or more UI references are not assigned in the Inspector!");
            gameObject.SetActive(false);
            return;
        }

        characterNameText.text = _finalCharacterStats.characterName;
        CharacterClassData classData = null;
        if (GameManager.Instance != null)
        {
            classData = GameManager.Instance.GetClassData(_finalCharacterStats.characterJob);
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null, cannot get class data for sprite.");
        }

        if (classData != null && classData.classSprite != null)
        {
            characterSpriteImage.sprite = classData.classSprite;
            characterSpriteImage.enabled = true;
        }
        else
        {
            characterSpriteImage.enabled = false;
            if (classData == null) Debug.LogWarning($"ClassData not found for job {_finalCharacterStats.characterJob} on character {_finalCharacterStats.characterName}");
            else if (classData.classSprite == null) Debug.LogWarning($"ClassSprite is null for job {classData.className} on character {_finalCharacterStats.characterName}");
        }

        if (learnedAbilitiesPanel != null) learnedAbilitiesPanel.SetActive(false);
        if (learnedAbilitiesText != null) learnedAbilitiesText.text = "";
    }

    public IEnumerator AnimateProgression(VictoryScreenManager.CharacterStatsSnapshot characterSnapshotAtStart, int expGainedThisFight)
    {
        if (_finalCharacterStats == null || characterSnapshotAtStart == null)
        {
            Debug.LogError("AnimateProgression: Final CharacterStats or Start Snapshot missing.");
            yield break;
        }
        if (experienceSlider == null || experienceValueText == null || levelText == null)
        {
            Debug.LogError("AnimateProgression: Core UI elements for EXP animation are null.");
            yield break;
        }

        int animLevel = characterSnapshotAtStart.level;
        int animCurrentExp = characterSnapshotAtStart.currentExp;
        int animExpToNext = characterSnapshotAtStart.expToNextLevel;

        if (animExpToNext <= 0 && animLevel < 99)
        {
            animExpToNext = (GameManager.Instance != null && GameManager.Instance.expCurve != null) ?
                            GameManager.Instance.expCurve.GetExpRequiredForLevel(animLevel) : 100;
        }

        levelText.text = $"Lvl. {animLevel}";
        experienceSlider.maxValue = animExpToNext > 0 ? animExpToNext : 100;
        experienceSlider.value = animCurrentExp;
        experienceValueText.text = $"EXP: {animCurrentExp} / {animExpToNext}";

        if (expGainedThisFight <= 0)
        {
            // Si no se gan� EXP, asegurarse de que no se muestren habilidades aprendidas (a menos que ya estuvieran en _newlyLearnedAbilitiesInThisCombat, lo cual no deber�a pasar aqu�)
            FinalizeLearnedAbilitiesDisplay();
            yield break;
        }

        int totalExpAnimatedSoFar = 0;

        while (totalExpAnimatedSoFar < expGainedThisFight)
        {
            int expNeededForLevelUp = animExpToNext - animCurrentExp;
            int expRemainingInGain = expGainedThisFight - totalExpAnimatedSoFar;
            int expToAnimateThisSegment = Mathf.Min(expNeededForLevelUp, expRemainingInGain);

            if (expToAnimateThisSegment <= 0 && expRemainingInGain > 0)
            {
                Debug.LogWarning($"expToAnimateThisSegment is {expToAnimateThisSegment} with expRemainingInGain {expRemainingInGain}. Breaking to avoid infinite loop for {characterSnapshotAtStart.characterName}");
                break;
            }
            if (expToAnimateThisSegment <= 0) break;

            float startExpInBarForLerp = animCurrentExp;
            float targetExpInBarForLerp = animCurrentExp + expToAnimateThisSegment;
            float currentSegmentFillNormalized = (animExpToNext > 0) ? (float)expToAnimateThisSegment / animExpToNext : 1.0f;
            float currentSegmentDuration = expFillDurationPerBar * currentSegmentFillNormalized;
            if (currentSegmentDuration < 0.1f && expToAnimateThisSegment > 0) currentSegmentDuration = 0.1f;

            float timer = 0f;
            while (timer < currentSegmentDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / currentSegmentDuration);
                animCurrentExp = (int)Mathf.Lerp(startExpInBarForLerp, targetExpInBarForLerp, progress);
                experienceSlider.value = animCurrentExp;
                experienceValueText.text = $"EXP: {animCurrentExp} / {animExpToNext}";
                yield return null;
            }
            animCurrentExp = (int)targetExpInBarForLerp;
            experienceSlider.value = animCurrentExp;
            experienceValueText.text = $"EXP: {animCurrentExp} / {animExpToNext}";

            totalExpAnimatedSoFar += expToAnimateThisSegment;

            if (animCurrentExp >= animExpToNext && animLevel < 99)
            {
                int previousAnimLevel = animLevel; // Guardar el nivel antes de incrementarlo
                animLevel++;
                animCurrentExp = 0;

                levelText.text = $"Lvl. {animLevel} LEVEL UP!";

                CharacterClassData classData = (GameManager.Instance != null) ? GameManager.Instance.GetClassData(_finalCharacterStats.characterJob) : null;
                if (classData != null)
                {
                    foreach (var learnable in classData.learnableAbilities)
                    {
                        if (learnable.ability == null) continue;

                        // Habilidad se aprende EXACTAMENTE en este nuevo 'animLevel'
                        if (learnable.levelLearned == animLevel)
                        {
                            // Y no la conoc�a en el nivel ANTERIOR al inicio del combate (snapshot)
                            // O, m�s simple, si no la conoc�a en el snapshot Y su nivel de aprendizaje es el actual 'animLevel'
                            // Esto asegura que solo contamos las aprendidas en ESTA subida de nivel espec�fica.
                            bool knewInSnapshotAtThisLevelOrLower = characterSnapshotAtStart.knownAbilities
                                .Any(ab => ab != null && ab.abilityName == learnable.ability.abilityName && learnable.levelLearned <= characterSnapshotAtStart.level);

                            bool knowsInFinalStats = _finalCharacterStats.knownAbilities
                                .Any(ab => ab != null && ab.abilityName == learnable.ability.abilityName);

                            if (knowsInFinalStats && !knewInSnapshotAtThisLevelOrLower && !_newlyLearnedAbilitiesInThisCombat.Contains(learnable.ability.abilityName))
                            {
                                _newlyLearnedAbilitiesInThisCombat.Add(learnable.ability.abilityName);
                            }
                        }
                    }
                }

                animExpToNext = (GameManager.Instance != null && GameManager.Instance.expCurve != null) ?
                                GameManager.Instance.expCurve.GetExpRequiredForLevel(animLevel) : 100;
                experienceSlider.maxValue = animExpToNext > 0 ? animExpToNext : 100;
                experienceSlider.value = 0;
                experienceValueText.text = $"EXP: 0 / {animExpToNext}";

                yield return new WaitForSeconds(levelUpPauseDuration);
                levelText.text = $"Lvl. {animLevel}";
            }
        }

        bool actualLevelUpOccurredDuringFight = _finalCharacterStats.level > characterSnapshotAtStart.level;
        if (actualLevelUpOccurredDuringFight)
        {
            levelText.text = $"Lvl. {_finalCharacterStats.level} LEVEL UP!";
        }
        else
        {
            levelText.text = $"Lvl. {_finalCharacterStats.level}";
        }

        experienceSlider.maxValue = _finalCharacterStats.expToNextLevel > 0 ? _finalCharacterStats.expToNextLevel : 100;
        experienceSlider.value = _finalCharacterStats.currentExp;
        experienceValueText.text = $"EXP: {_finalCharacterStats.currentExp} / {_finalCharacterStats.expToNextLevel}";

        // Llamar a la funci�n para mostrar las habilidades acumuladas
        FinalizeLearnedAbilitiesDisplay();

        if (actualLevelUpOccurredDuringFight)
        {
            yield return new WaitForSeconds(levelUpPauseDuration * 0.75f);
            levelText.text = $"Lvl. {_finalCharacterStats.level}";
        }
    }

    // Nueva funci�n para centralizar la visualizaci�n de habilidades aprendidas
    private void FinalizeLearnedAbilitiesDisplay()
    {
        if (_newlyLearnedAbilitiesInThisCombat.Any())
        {
            if (learnedAbilitiesPanel != null && learnedAbilitiesText != null)
            {
                learnedAbilitiesPanel.SetActive(true);
                StringBuilder abilitiesBuilder = new StringBuilder();
                // Podr�as a�adir un encabezado si quieres, pero lo has quitado.
                // abilitiesBuilder.AppendLine("Learned Abilities:"); 
                foreach (var abilityName in _newlyLearnedAbilitiesInThisCombat)
                {
                    abilitiesBuilder.AppendLine($"- {abilityName}");
                }
                learnedAbilitiesText.text = abilitiesBuilder.ToString();
            }
        }
        else
        {
            // Si no se aprendi� nada nuevo, asegurarse que el panel est� oculto
            if (learnedAbilitiesPanel != null) learnedAbilitiesPanel.SetActive(false);
            if (learnedAbilitiesText != null) learnedAbilitiesText.text = "";
        }
    }
}