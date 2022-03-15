using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SMU.Utilities;
using SpinCore;
using SpinCore.UI;
using UnityEngine;

namespace SRXDScoreMod; 

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInDependency("com.pink.spinrhythm.spincore")]
[BepInPlugin("SRXD.ScoreMod", "ScoreMod", "1.2.0.9")]
internal class Plugin : SpinPlugin {
    public static Plugin Instance { get; private set; }
    public new static ManualLogSource Logger { get; private set; }
    public static Bindable<int> CurrentSystem { get; private set; }
    public static Bindable<GameplayUI.PaceType> PaceType { get; private set; }
    public static Bindable<int> TapTimingOffset { get; private set; }
    public static Bindable<int> BeatTimingOffset { get; private set; }
    
    protected override void Awake() {
        base.Awake();
        Instance = this;
        Logger = base.Logger;

        CurrentSystem = AddBindableConfig("CurrentSystem", 0);
        PaceType = AddBindableConfig("PaceType", GameplayUI.PaceType.Both);
        TapTimingOffset = AddBindableConfig("TapTimingOffset", 0);
        BeatTimingOffset = AddBindableConfig("BeatTimingOffset", 0);

        var harmony = new Harmony("ScoreMod");
            
        harmony.PatchAll(typeof(GameplayState));
        harmony.PatchAll(typeof(GameplayUI));
        harmony.PatchAll(typeof(CompleteScreenUI));
        harmony.PatchAll(typeof(LevelSelectUI));
        HighScoresContainer.LoadHighScores();
        ScoreMod.Init();
    }

    protected override void CreateMenus() {
        var root = CreateOptionsTab("Score Mod").UIRoot;

        if (CurrentSystem.Value >= ScoreMod.ScoreSystems.Count)
            CurrentSystem.Value = 0;
        
        SpinUI.CreateDropdown("Current Score System", root, ScoreMod.ScoreSystems.Select(scoreSystem => scoreSystem.Name).ToArray()).Bind(CurrentSystem);
        SpinUI.CreateDropdown<GameplayUI.PaceType>("Pace Type", root).Bind(PaceType);
        SpinUI.CreateSlider("Tap Timing Offset", root, -100f, 100f, wholeNumbers: true, valueDisplay: value => $"{Mathf.RoundToInt(value)}ms").Bind(TapTimingOffset);
        SpinUI.CreateSlider("Beat Timing Offset", root, -100f, 100f, wholeNumbers: true, valueDisplay: value => $"{Mathf.RoundToInt(value)}ms").Bind(BeatTimingOffset);
    }

    protected override void LateInit() => ScoreMod.LateInit();
}