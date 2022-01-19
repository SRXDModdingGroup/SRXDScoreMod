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
[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.1")]
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
    
    internal static List<IScoreSystem> ScoreSystems { get; private set; }
    
    internal static List<CustomScoreSystem> CustomScoreSystems { get; private set; }

    private static string fileDirectory;
    
    public static void AddCustomScoreSystem(ScoreSystemProfile profile) {
        var scoreSystem = new CustomScoreSystem(profile);
        
        ScoreSystems.Add(scoreSystem);
        CustomScoreSystems.Add(scoreSystem);
    }

    private void Awake() {
        Logger = base.Logger;
        
        StartEnabled = Config.Bind("Settings", "StartEnabled", true, "Enable modded score on startup");
        DefaultProfile = Config.Bind("Settings", "DefaultProfile", "0", "The name or index of the default scoring profile");
        PaceType = Config.Bind("Settings", "PaceType", "Both", new ConfigDescription("Whether to show the max possible score, its delta relative to PB, both, or hide the Pace display", new AcceptableValueList<string>("Delta", "Score", "Both", "Hide")));
        TapTimingOffset = Config.Bind("Settings", "TapTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for taps and liftoffs");
        BeatTimingOffset = Config.Bind("Settings", "BeatTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for beats and hard beat releases");

        var harmony = new Harmony("ScoreMod");
            
        harmony.PatchAll(typeof(GameplayState));
        harmony.PatchAll(typeof(GameplayUI));
        harmony.PatchAll(typeof(CompleteScreenUI));
        harmony.PatchAll(typeof(LevelSelectUI));
        ScoreSystems = new List<IScoreSystem>();
        ScoreSystems.Add(new BaseScoreSystemWrapper());
        CurrentScoreSystemInternal = ScoreSystems[0];
        CustomScoreSystems = new List<CustomScoreSystem>();
        AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM16);
        //AddCustomScoreSystem(DefaultScoreSystemProfiles.StandardPPM32);
        CurrentScoreSystemInternal = ScoreSystems[1];
        HighScoresContainer.LoadHighScores();
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

    private static void PickScoreSystem(int index) {
        if (index >= ScoreSystems.Count)
            return;

        CurrentScoreSystemInternal = ScoreSystems[index];
        CompleteScreenUI.UpdateUI();
        LevelSelectUI.UpdateUI();
    }
}