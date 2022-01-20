using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Utilities;
using TMPro;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains patch functions to display modded scores on the track complete screen
internal static class CompleteScreenUI {
    private static XDLevelCompleteMenu levelCompleteMenu;
    private static TranslatedTextMeshPro accLabel;
    private static TranslatedTextMeshPro fcLabel;
    private static TranslatedTextMeshPro pfcLabel;
    private static TMP_Text scoreSystemNameText;
    private static Vector3 nameTextPosition;
    
    public static void UpdateUI(bool updateGraphs) {
        if (levelCompleteMenu == null || !levelCompleteMenu.gameObject.activeSelf)
            return;

        var scoreSystem = ScoreMod.CurrentScoreSystemInternal;
        var scoreValueText = levelCompleteMenu.scoreValueText;

        if (scoreSystemNameText == null) {
            scoreSystemNameText = GameObject.Instantiate(scoreValueText.gameObject, scoreValueText.transform.parent, true).GetComponent<TMP_Text>();
            scoreSystemNameText.gameObject.SetActive(true);
            nameTextPosition = scoreSystemNameText.transform.localPosition + new Vector3(-25f, 70f, 0f);
            scoreSystemNameText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            scoreSystemNameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            scoreSystemNameText.overflowMode = TextOverflowModes.Overflow;
            scoreSystemNameText.rectTransform.anchorMax += 100f * Vector2.right;
            scoreSystemNameText.fontSize *= 0.7f;
            scoreSystemNameText.outlineColor = Color.cyan;
            scoreSystemNameText.outlineWidth = 0.15f;
        }

        if (scoreSystem.IsHighScore)
            scoreSystemNameText.transform.localPosition = nameTextPosition + 18f * Vector3.up;
        else
            scoreSystemNameText.transform.localPosition = nameTextPosition;
        
        scoreSystemNameText.SetText(scoreSystem.Name);
        scoreValueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            
        if (scoreSystem.SecondaryScore == 0)
            scoreValueText.SetText(scoreSystem.Score.ToString());
        else
            scoreValueText.SetText($"<line-height=50%>{scoreSystem.Score:0}\n<size=50%>+{scoreSystem.SecondaryScore:0}");
        
        levelCompleteMenu.streakValueText.SetText(scoreSystem.Streak.ToString());
        levelCompleteMenu.rankAnimator.Setup(scoreSystem.Rank, null);
        levelCompleteMenu.pfcStatusText.SetText(scoreSystem.FullComboState == FullComboState.PerfectFullCombo ? "PFC" : "FC");
        levelCompleteMenu.newBestGameObject.SetActive(scoreSystem.IsHighScore);
        levelCompleteMenu.FcBonusGameObject.SetActive(!string.IsNullOrWhiteSpace(scoreSystem.PostGameInfo1Value));
        fcLabel.textToAppend = scoreSystem.PostGameInfo1Name;
        levelCompleteMenu.fcBonusText.SetText(scoreSystem.PostGameInfo1Value);
        levelCompleteMenu.PfcBonusGameObject.SetActive(!string.IsNullOrWhiteSpace(scoreSystem.PostGameInfo2Value));
        pfcLabel.textToAppend = scoreSystem.PostGameInfo2Name;
        levelCompleteMenu.pfcBonusText.SetText(scoreSystem.PostGameInfo2Value);
        levelCompleteMenu.accuracyGameObject.SetActive(!string.IsNullOrWhiteSpace(scoreSystem.PostGameInfo3Value));
        accLabel.textToAppend = scoreSystem.PostGameInfo3Name;
        levelCompleteMenu.accuracyBonusText.SetText(scoreSystem.PostGameInfo3Value);
        
        if (!updateGraphs)
            return;

        var stats = levelCompleteMenu.playState.playStateStats;
        var extendedStats = levelCompleteMenu.extendedStats;
        
        levelCompleteMenu.timelineGraph.SetupPerformanceGraph(levelCompleteMenu.playState, stats, 1f);
        extendedStats.Setup(stats);
        
        if (PlayerSettingsData.Instance.ShowExtendedStats.Value > 0)
            extendedStats.Display();
    }

    private static void FillPerformanceGraphValues(List<float> values, List<Color> colors) {
        var pairs = ScoreMod.CurrentScoreSystemInternal.PerformanceGraphValues;

        foreach (var pair in pairs) {
            values.Add(pair.Value);
            colors.Add(pair.Color);
        }
    }

    private static PieGraphValue GetPieGraphValue(int index) => ScoreMod.CurrentScoreSystemInternal.PieGraphValues[index];

    private static string GetInterpolatedScoreString(int score, int secondaryScore, float time) {
        if (secondaryScore == 0)
            return (time * score).ToString("0");

        return $"<line-height=50%>{time * score:0}\n<size=50%>+{time * secondaryScore:0}";
    }

    [HarmonyPatch(typeof(XDLevelCompleteMenu), nameof(XDLevelCompleteMenu.Setup)), HarmonyPostfix]
    private static void XDLevelCompleteMenu_Setup_Postfix(XDLevelCompleteMenu __instance, PlayState playState) {
        levelCompleteMenu = __instance;
        accLabel = __instance.accuracyGameObject.GetComponentsInChildren<TranslatedTextMeshPro>()[0];
        fcLabel = __instance.FcBonusGameObject.GetComponentsInChildren<TranslatedTextMeshPro>()[0];
        pfcLabel = __instance.PfcBonusGameObject.GetComponentsInChildren<TranslatedTextMeshPro>()[0];
        accLabel.translation = null;
        fcLabel.translation = null;
        pfcLabel.translation = null;
        UpdateUI(false);
    }

    [HarmonyPatch(typeof(RankAnimator), nameof(RankAnimator.Setup)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> RankAnimator_Setup_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var fullComboState = generator.DeclareLocal(typeof(FullComboState));
        var ScoreMod_get_CurrentScoreSystemInternal = typeof(ScoreMod).GetProperty(nameof(ScoreMod.CurrentScoreSystemInternal), BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var IReadOnlyScoreSystem_get_FullComboState = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.FullComboState)).GetGetMethod();
        var PlayState_get_fullComboState = typeof(PlayState).GetProperty(nameof(PlayState.fullComboState)).GetGetMethod();
        
        operations.Insert(0, new CodeInstruction[] {
            new (OpCodes.Call, ScoreMod_get_CurrentScoreSystemInternal),
            new (OpCodes.Callvirt, IReadOnlyScoreSystem_get_FullComboState),
            new (OpCodes.Stloc_S, fullComboState)
        });

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_2, // playState
            instr => instr.Calls(PlayState_get_fullComboState)
        });

        foreach (var match in matches) {
            int start = match[0].Start;
            var labels = instructionsList[start].labels;
            
            operations.Replace(start, 2, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_S, fullComboState).WithLabels(labels)
            });
        }
            
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(LevelCompleteCoreAnimationBehaviour), "AnimateText"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LevelCompleteCoreAnimationBehaviour_AnimateText_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var currentScoreSystem = generator.DeclareLocal(typeof(IReadOnlyScoreSystem));
        var CompleteScreenUI_GetInterpolatedScoreString = typeof(CompleteScreenUI).GetMethod(nameof(GetInterpolatedScoreString), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreMod_get_CurrentScoreSystemInternal = typeof(ScoreMod).GetProperty(nameof(ScoreMod.CurrentScoreSystemInternal), BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var IReadOnlyScoreSystem_get_Score = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.Score)).GetGetMethod();
        var IReadOnlyScoreSystem_get_SecondaryScore = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.SecondaryScore)).GetGetMethod();
        var IReadOnlyScoreSystem_get_MaxStreak = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.MaxStreak)).GetGetMethod();
        var PlayState_get_TotalScore = typeof(PlayState).GetProperty(nameof(PlayState.TotalScore)).GetGetMethod();
        var PlayState_get_maxCombo = typeof(PlayState).GetProperty(nameof(PlayState.maxCombo)).GetGetMethod();
        var Single_ToString = typeof(Single).GetMethod(nameof(Single.ToString), new [] { typeof(string) });
        
        operations.Insert(0, new CodeInstruction[] {
            new (OpCodes.Call, ScoreMod_get_CurrentScoreSystemInternal),
            new (OpCodes.Stloc_S, currentScoreSystem)
        });

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_1, // interpolatedTime
            instr => instr.opcode == OpCodes.Ldloc_0, // playState
            instr => instr.Calls(PlayState_get_TotalScore),
            instr => instr.opcode == OpCodes.Conv_R4,
            instr => instr.opcode == OpCodes.Mul,
            instr => instr.opcode == OpCodes.Stloc_2 // interpolatedScore
        }).First()[0];
        
        instructionsList[match.End].labels.AddRange(instructionsList[match.Start].labels);
            
        operations.Remove(match.Start, 6);
            
        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_0, // playState
            instr => instr.Calls(PlayState_get_maxCombo)
        }).First()[0];
            
        operations.Replace(match.Start, 2, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, currentScoreSystem),
            new (OpCodes.Callvirt, IReadOnlyScoreSystem_get_MaxStreak)
        });

        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAddressAtIndex(2), // interpolatedScore
            instr => instr.opcode == OpCodes.Ldstr, // "0"
            instr => instr.Calls(Single_ToString)
        }).First()[0];
        
        operations.Replace(match.Start, 3, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, currentScoreSystem),
            new (OpCodes.Callvirt, IReadOnlyScoreSystem_get_Score),
            new (OpCodes.Ldloc_S, currentScoreSystem),
            new (OpCodes.Callvirt, IReadOnlyScoreSystem_get_SecondaryScore),
            new (OpCodes.Ldloc_1), // interpolatedTime
            new (OpCodes.Call, CompleteScreenUI_GetInterpolatedScoreString)
        });
            
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(PerformanceGraph), nameof(PerformanceGraph.SetupPerformanceGraph)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PerformanceGraph_SetupPerformanceGraph_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var CompleteScreenUI_FillPerformanceGraphValues = typeof(CompleteScreenUI).GetMethod(nameof(FillPerformanceGraphValues), BindingFlags.NonPublic | BindingFlags.Static);
        var AnimatedGraph_elementColors = typeof(AnimatedGraph).GetField("elementColors", BindingFlags.NonPublic | BindingFlags.Instance);

        var match0 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldc_I4_0,
            instr => instr.opcode == OpCodes.Stloc_1, // index
            instr => instr.Branches(out _),
            instr => instr.opcode == OpCodes.Ldarg_0 // this
        }).First()[0];

        var startLabels = instructionsList[match0.Start + 3].labels;
        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Branches(out var label) && label.HasValue && startLabels.Contains(label.Value)
        }).Last()[0];

        int start = match0.Start;
        
        operations.Replace(start, match1.End - start, new CodeInstruction[] {
            new (OpCodes.Ldloc_0), // values
            new (OpCodes.Ldarg_0), // this
            new (OpCodes.Ldfld, AnimatedGraph_elementColors),
            new (OpCodes.Call, CompleteScreenUI_FillPerformanceGraphValues)
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(ExtendedStats), nameof(ExtendedStats.Display)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ExtendedStats_Display_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var pieGraphValue = generator.DeclareLocal(typeof(PieGraphValue));
        var CompleteScreenUI_GetPieGraphValue = typeof(CompleteScreenUI).GetMethod(nameof(GetPieGraphValue), BindingFlags.NonPublic | BindingFlags.Static);
        var PieGraphValue_get_Perfect = typeof(PieGraphValue).GetProperty(nameof(PieGraphValue.Perfect)).GetGetMethod();
        var PieGraphValue_get_Good = typeof(PieGraphValue).GetProperty(nameof(PieGraphValue.Good)).GetGetMethod();
        var PieGraphValue_get_Missed = typeof(PieGraphValue).GetProperty(nameof(PieGraphValue.Missed)).GetGetMethod();

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_2, // statsCollection
            instr => instr.opcode == OpCodes.Ldc_I4_1
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(6) // misses
        }).First();

        int start = match[0].Start;
        
        operations.Replace(start, match[1].End - start, new CodeInstruction[] {
            new (OpCodes.Ldloc_1), // index
            new (OpCodes.Call, CompleteScreenUI_GetPieGraphValue),
            new (OpCodes.Stloc_S, pieGraphValue),
            new (OpCodes.Ldloc_S, pieGraphValue),
            new (OpCodes.Call, PieGraphValue_get_Perfect),
            new (OpCodes.Stloc_S, (byte) 5), // perfect
            new (OpCodes.Ldloc_S, pieGraphValue),
            new (OpCodes.Call, PieGraphValue_get_Good),
            new (OpCodes.Stloc_S, (byte) 4), // good
            new (OpCodes.Ldloc_S, pieGraphValue),
            new (OpCodes.Call, PieGraphValue_get_Missed),
            new (OpCodes.Stloc_S, (byte) 6), // misses
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }
}