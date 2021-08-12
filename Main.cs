using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace ScoreMod {
    [BepInPlugin("ScoreMod", "ScoreMod", "1.0.0.0")]
    public class Main : BasePlugin {
        public static ManualLogSource Logger { get; private set; }
        public static ConfigEntry<string> DefaultProfile { get; private set; }
        public static ConfigEntry<bool> ShowPace { get; private set; }
        public static ConfigEntry<float> TapTimingOffset { get; private set; }
        public static ConfigEntry<float> BeatTimingOffset { get; private set; }

        private static string fileDirectory;

        public override void Load() {
            Logger = Log;

            var harmony = new Harmony("ScoreMod");
            
            harmony.PatchAll(typeof(GameplayState));
            harmony.PatchAll(typeof(GameplayUI));
            harmony.PatchAll(typeof(CompleteScreenUI));
            harmony.PatchAll(typeof(LevelSelectUI));

            DefaultProfile = Config.Bind("Settings", "DefaultProfile", "0", "The name or index of the default scoring profile");
            ShowPace = Config.Bind("Settings", "ShowPace", true, "Show PACE, the maximum possible PB, below score");
            TapTimingOffset = Config.Bind("Settings", "TapTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for taps and liftoffs");
            BeatTimingOffset = Config.Bind("Settings", "BeatTimingOffset", 0f, "Global offset (in ms) applied to all mod timing calculations for beats and hard beat releases");
            
            HighScoresContainer.LoadHighScores();
            ModState.Initialize(string.Empty, 0);
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