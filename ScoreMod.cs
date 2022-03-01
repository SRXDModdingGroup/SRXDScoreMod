﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SRXDScoreMod; 

/// <summary>
/// Plugin that enables the introduction of custom score systems and score modifiers
/// </summary>
[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.2.0.8")]
public class ScoreMod : BaseUnityPlugin {
    internal new static ManualLogSource Logger { get; private set; }
    internal static ConfigEntry<string> DefaultSystem { get; private set; }
    internal static ConfigEntry<string> PaceType { get; private set; }
    internal static ConfigEntry<float> TapTimingOffset { get; private set; }
    internal static ConfigEntry<float> BeatTimingOffset { get; private set; }

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
    
    internal static List<IScoreSystemInternal> ScoreSystems { get; private set; }
    
    internal static List<CustomScoreSystem> CustomScoreSystems { get; private set; }

    private static int scoreSystemIndex;
    private static float modifierMultiplier;
    private static string fileDirectory;
    
    /// <summary>
    /// Adds a new custom score system with a given profile
    /// </summary>
    /// <param name="profile">The profile to use</param>
    public static void AddCustomScoreSystem(ScoreSystemProfile profile) {
        foreach (var system in ScoreSystems) {
            if (profile.Id != system.Id)
                continue;
            
            Logger.LogWarning($"WARNING: Score system with ID {profile.Id} already exists");
                
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

    private void Awake() {
        Logger = base.Logger;
        
        DefaultSystem = Config.Bind("Settings", "DefaultSystem", "0", "The name or index of the default scoring system");
        PaceType = Config.Bind("Settings", "PaceType", "Both", new ConfigDescription("Whether to show the max possible score, its delta relative to PB, both, or hide the Pace display", new AcceptableValueList<string>("Delta", "Score", "Both", "Hide")));
        TapTimingOffset = Config.Bind("Settings", "TapTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for taps and liftoffs");
        BeatTimingOffset = Config.Bind("Settings", "BeatTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for beats and hard beat releases");

        var harmony = new Harmony("ScoreMod");
            
        harmony.PatchAll(typeof(GameplayState));
        harmony.PatchAll(typeof(GameplayUI));
        harmony.PatchAll(typeof(CompleteScreenUI));
        harmony.PatchAll(typeof(LevelSelectUI));
        HighScoresContainer.LoadHighScores();
        ScoreSystems = new List<IScoreSystemInternal> { new BaseScoreSystemWrapper() };
        CustomScoreSystems = new List<CustomScoreSystem>();
        AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM16);
        // AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM32);

        if (!int.TryParse(DefaultSystem.Value, out scoreSystemIndex)) {
            scoreSystemIndex = 0;
            
            for (int i = 0; i < ScoreSystems.Count; i++) {
                if (ScoreSystems[i].Name != DefaultSystem.Value)
                    continue;
                
                scoreSystemIndex = i;

                break;
            }
        }

        if (scoreSystemIndex >= ScoreSystems.Count)
            scoreSystemIndex = ScoreSystems.Count - 1;
        
        CurrentScoreSystemInternal = ScoreSystems[scoreSystemIndex];
        modifierMultiplier = 1f;
        UpdateLabelString();
        // SetModifierSet(new ModifierSet("Test", "test",
        //     new Modifier("Mod0", 0, 10),
        //     new Modifier("Mod1", 1, 25),
        //     new Modifier("Mod2", 2, -50)));
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

    internal static bool TryGetFileDirectory(out string directory) {
        if (!string.IsNullOrWhiteSpace(fileDirectory)) {
            directory = fileDirectory;

            return true;
        }
            
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrWhiteSpace(assemblyDirectory) || !Directory.Exists(assemblyDirectory)) {
            Logger.LogWarning("WARNING: Could not get assembly directory");
            directory = string.Empty;

            return false;
        }

        fileDirectory = Path.Combine(assemblyDirectory, "ScoreMod");
        directory = fileDirectory;

        if (!Directory.Exists(fileDirectory))
            Directory.CreateDirectory(fileDirectory);

        return true;
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