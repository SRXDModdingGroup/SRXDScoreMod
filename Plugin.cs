using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SRXDScoreMod; 

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.2.0.8")]
internal class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }
    public static ConfigEntry<string> DefaultSystem { get; private set; }
    public static ConfigEntry<string> PaceType { get; private set; }
    public static ConfigEntry<float> TapTimingOffset { get; private set; }
    public static ConfigEntry<float> BeatTimingOffset { get; private set; }
    
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
        ScoreMod.Init();
    }
}