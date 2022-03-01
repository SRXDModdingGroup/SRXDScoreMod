using System;
using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

/// <summary>
/// Enables the introduction of custom score systems and score modifiers
/// </summary>
public static class ScoreMod {

    /// <summary>
    /// Invoke when the current score system is changed
    /// </summary>
    public static event Action<IScoreSystem> OnScoreSystemChanged;

    /// <summary>
    /// The currently visible score system
    /// </summary>
    public static IScoreSystem CurrentScoreSystem => CurrentScoreSystemInternal;

    /// <summary>
    /// The currently used modifier set
    /// </summary>
    public static ModifierSet CurrentModifierSet { get; private set; }

    internal static bool AnyModifiersEnabled { get; private set; }
    internal static string ScoreSystemAndMultiplierLabel { get; private set; }
    internal static IScoreSystemInternal CurrentScoreSystemInternal { get; private set; }
    internal static List<IScoreSystemInternal> ScoreSystems { get; } = new();
    internal static List<CustomScoreSystem> CustomScoreSystems { get; } = new();
    
    private static int scoreSystemIndex;
    private static float modifierMultiplier = 1f;

    /// <summary>
    /// Adds a new custom score system with a given profile
    /// </summary>
    /// <param name="profile">The profile to use</param>
    public static void AddCustomScoreSystem(ScoreSystemProfile profile) {
        foreach (var system in ScoreSystems) {
            if (profile.Id != system.Id)
                continue;
            
            Plugin.Logger.LogWarning($"WARNING: Score system with ID {profile.Id} already exists");
                
            return;
        }
        
        var scoreSystem = new CustomScoreSystem(profile);

        ScoreSystems.Add(scoreSystem);
        CustomScoreSystems.Add(scoreSystem);
    }

    /// <summary>
    /// Sets the modifier set to use
    /// </summary>
    /// <param name="modifierSet">The modifier set to use</param>
    public static void SetModifierSet(ModifierSet modifierSet) {
        if (modifierSet == CurrentModifierSet)
            return;

        if (CurrentModifierSet != null) {
            CurrentModifierSet.ModifierChanged -= OnModifierChanged;
            CurrentModifierSet.DisableAll();
        }

        CurrentModifierSet = modifierSet;

        if (modifierSet != null) {
            modifierSet.DisableAll();
            modifierSet.ModifierChanged += OnModifierChanged;
            HighScoresContainer.RemoveInvalidHighScoresForModifierSet(modifierSet);
        }

        OnModifierChanged();
    }

    internal static void Init() {
        ScoreSystems.Add(new BaseScoreSystemWrapper());
        AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM16);
        // AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM32);

        string defaultSystem = Plugin.DefaultSystem.Value;

        if (!int.TryParse(defaultSystem, out scoreSystemIndex)) {
            scoreSystemIndex = 0;
            
            for (int i = 0; i < ScoreSystems.Count; i++) {
                if (ScoreSystems[i].Name != defaultSystem)
                    continue;

                scoreSystemIndex = i;

                break;
            }
        }

        if (scoreSystemIndex >= ScoreSystems.Count)
            scoreSystemIndex = ScoreSystems.Count - 1;

        CurrentScoreSystemInternal = ScoreSystems[scoreSystemIndex];
        UpdateLabelString();
    }
    
    internal static void GameUpdate() {
        if (Input.GetKey(KeyCode.P)) {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                PickScoreSystem(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                PickScoreSystem(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                PickScoreSystem(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                PickScoreSystem(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                PickScoreSystem(4);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                PickScoreSystem(5);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                PickScoreSystem(6);
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                PickScoreSystem(7);
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                PickScoreSystem(8);
            else if (Input.GetKeyDown(KeyCode.Alpha0))
                PickScoreSystem(9);
        }
    }

    internal static int GetModifiedScore(int score) {
        if (AnyModifiersEnabled)
            return Mathf.CeilToInt(modifierMultiplier * score);

        return score;
    }

    private static void PickScoreSystem(int index) {
        if (index >= ScoreSystems.Count || index == scoreSystemIndex)
            return;

        scoreSystemIndex = index;
        CurrentScoreSystemInternal = ScoreSystems[index];
        UpdateLabelString();
        GameplayUI.UpdateUI();
        CompleteScreenUI.UpdateUI(true);
        LevelSelectUI.UpdateUI();
        OnScoreSystemChanged?.Invoke(CurrentScoreSystem);
    }

    private static void UpdateLabelString() {
        if (AnyModifiersEnabled) {
            int multiplier = CurrentModifierSet.GetOverallMultiplier();

            ScoreSystemAndMultiplierLabel = $"{CurrentScoreSystem.Name} ({multiplier / 100}.{(multiplier % 100).ToString().TrimEnd('0')}x)";
        }
        else
            ScoreSystemAndMultiplierLabel = CurrentScoreSystem.Name;
    }

    private static void OnModifierChanged() {
        if (CurrentModifierSet == null) {
            AnyModifiersEnabled = false;
            modifierMultiplier = 1f;
        }
        else {
            AnyModifiersEnabled = CurrentModifierSet.GetAnyEnabled();
            modifierMultiplier = 0.01f * CurrentModifierSet.GetOverallMultiplier();
        }

        UpdateLabelString();
        LevelSelectUI.UpdateUI();
    }
}