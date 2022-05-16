using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SMU.Utilities;

namespace SRXDScoreMod; 

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.6")]
[BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.2.0.9")]
internal class Plugin : BaseUnityPlugin {
    public static Plugin Instance { get; private set; }
    public new static ManualLogSource Logger { get; private set; }
    public static Bindable<int> CurrentSystem { get; private set; }
    public static Bindable<GameplayUI.PaceType> PaceType { get; private set; }
    public static Bindable<int> TapTimingOffset { get; private set; }
    public static Bindable<int> BeatTimingOffset { get; private set; }
    
    private void Awake() {
        Instance = this;
        Logger = base.Logger;

        CurrentSystem = new Bindable<int>(Config.Bind("Config", "CurrentSystem", 0).Value);
        PaceType = new Bindable<GameplayUI.PaceType>(Config.Bind("Config", "PaceType", GameplayUI.PaceType.Both).Value);
        TapTimingOffset = new Bindable<int>(Config.Bind("Config", "TapTimingOffset", 0).Value);
        BeatTimingOffset = new Bindable<int>(Config.Bind("Config", "BeatTimingOffset", 0).Value);

        var harmony = new Harmony("ScoreMod");
            
        harmony.PatchAll(typeof(GameplayState));
        harmony.PatchAll(typeof(GameplayUI));
        harmony.PatchAll(typeof(CompleteScreenUI));
        harmony.PatchAll(typeof(LevelSelectUI));
        HighScoresContainer.LoadHighScores();
        ScoreMod.Init();
        ScoreMod.LateInit();
    }
}