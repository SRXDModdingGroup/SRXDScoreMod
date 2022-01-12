using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SRXDScoreMod {
    // Contains code to initialize the mod
    [BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.1.1.0")]
    public class Main : BaseUnityPlugin {
        public new static ManualLogSource Logger { get; private set; }
        public static ConfigEntry<bool> StartEnabled { get; private set; }
        public static ConfigEntry<string> DefaultProfile { get; private set; }
        public static ConfigEntry<string> PaceType { get; private set; }
        public static ConfigEntry<float> TapTimingOffset { get; private set; }
        public static ConfigEntry<float> BeatTimingOffset { get; private set; }
        
        public static BaseScoreContainerWrapper BaseScoreContainerWrapper { get; private set; }
        
        public static IReadOnlyScoreContainer CurrentScoreContainer { get; private set; }

        private static string fileDirectory;

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

            BaseScoreContainerWrapper = new BaseScoreContainerWrapper();
            HighScoresContainer.LoadHighScores();
            ModState.Initialize(string.Empty, 0, 0, 0, 0);
            
            if (StartEnabled.Value)
                ModState.ToggleModdedScoring();
        }

        public static bool TryGetFileDirectory(out string directory) {
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
    }
}