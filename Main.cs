using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace ScoreMod {
    [BepInPlugin("ScoreMod", "ScoreMod", "1.0.0.0")]
    public class Main : BasePlugin {
        public static ManualLogSource Logger { get; private set; }

        public override void Load() {
            Logger = Log;

            var harmony = new Harmony("ScoreMod");
            
            harmony.PatchAll(typeof(GameplayState));
            harmony.PatchAll(typeof(GameplayUI));
            harmony.PatchAll(typeof(CompleteScreenUI));
            harmony.PatchAll(typeof(LevelSelectUI));
            
            ModState.Initialize();
            HighScoresContainer.LoadHighScores();
        }
    }
}