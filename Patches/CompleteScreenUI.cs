﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Utilities;
using TMPro;

namespace SRXDScoreMod; 

// Contains patch functions to display modded scores on the track complete screen
internal static class CompleteScreenUI {
    private static XDLevelCompleteMenu levelCompleteMenu;
    private static TranslatedTextMeshPro accLabel;
    private static TranslatedTextMeshPro fcLabel;
    private static TranslatedTextMeshPro pfcLabel;
        
    public static void UpdateUI() {
        if (levelCompleteMenu == null || !levelCompleteMenu.gameObject.activeSelf)
            return;

        var scoreSystem = ScoreMod.CurrentScoreSystemInternal;

        levelCompleteMenu.scoreValueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            
        if (scoreSystem.SecondaryScore == 0)
            levelCompleteMenu.scoreValueText.SetText(scoreSystem.Score.ToString());
        else
            levelCompleteMenu.scoreValueText.SetText($"<line-height=50%>{scoreSystem.Score:0}\n<size=50%>+{scoreSystem.SecondaryScore}:0");
        
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
    }

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
        UpdateUI();
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
}