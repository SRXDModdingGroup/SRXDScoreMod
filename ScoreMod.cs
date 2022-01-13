using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains code to initialize the mod
[BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.1.1.0")]
public class ScoreMod : BaseUnityPlugin {
    internal new static ManualLogSource Logger { get; private set; }
    internal static ConfigEntry<bool> StartEnabled { get; private set; }
    internal static ConfigEntry<string> DefaultProfile { get; private set; }
    internal static ConfigEntry<string> PaceType { get; private set; }
    internal static ConfigEntry<float> TapTimingOffset { get; private set; }
    internal static ConfigEntry<float> BeatTimingOffset { get; private set; }

    public static IReadOnlyScoreSystem CurrentScoreSystem => CurrentScoreSystemInternal;

    internal static IScoreSystem CurrentScoreSystemInternal { get; private set; }

    private static bool pickedNewScoreSystem;
    private static string fileDirectory;
    private static List<IScoreSystem> scoreSystems;

    public static void AddScoreSystem(IScoreSystem scoreSystem) => scoreSystems.Add(scoreSystem);

    private void Awake() {
        Logger = base.Logger;

        var harmony = new Harmony("ScoreMod");
            
        harmony.PatchAll(typeof(GameplayState));
        harmony.PatchAll(typeof(GameplayUI));
        harmony.PatchAll(typeof(CompleteScreenUI));
        harmony.PatchAll(typeof(LevelSelectUI));

        StartEnabled = Config.Bind("Settings", "StartEnabled", true, "Enable modded score on startup");
        DefaultProfile = Config.Bind("Settings", "DefaultProfile", "0", "The name or index of the default scoring profile");
        PaceType = Config.Bind("Settings", "PaceType", "Both", new ConfigDescription("Whether to show the max possible score, its delta relative to PB, both, or hide the Pace display", new AcceptableValueList<string>("Delta", "Score", "Both", "Hide")));
        TapTimingOffset = Config.Bind("Settings", "TapTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for taps and liftoffs");
        BeatTimingOffset = Config.Bind("Settings", "BeatTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for beats and hard beat releases");
        
        scoreSystems.Add(new BaseScoreSystemWrapper());
        HighScoresContainer.LoadHighScores();
        ModState.Initialize(string.Empty, 0, 0, 0, 0);
            
        if (StartEnabled.Value)
            ModState.ToggleModdedScoring();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.P))
            pickedNewScoreSystem = false;

        if (Input.GetKey(KeyCode.P)) {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                pickedNewScoreSystem = PickScoreSystem(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                pickedNewScoreSystem = PickScoreSystem(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                pickedNewScoreSystem = PickScoreSystem(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                pickedNewScoreSystem = PickScoreSystem(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                pickedNewScoreSystem = PickScoreSystem(5);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                pickedNewScoreSystem = PickScoreSystem(6);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                pickedNewScoreSystem = PickScoreSystem(7);
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                pickedNewScoreSystem = PickScoreSystem(8);
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                pickedNewScoreSystem = PickScoreSystem(9);
        }

        if (Input.GetKeyUp(KeyCode.P) && !pickedNewScoreSystem)
            PickScoreSystem(0);
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

    private static bool PickScoreSystem(int index) {
        if (index >= scoreSystems.Count)
            return false;

        CurrentScoreSystemInternal = scoreSystems[index];
        CompleteScreenUI.UpdateUI();
        LevelSelectUI.UpdateUI();

        return true;
    }
}