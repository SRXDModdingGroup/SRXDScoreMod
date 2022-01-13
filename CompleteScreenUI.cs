using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Utilities;

namespace SRXDScoreMod; 

// Contains patch functions to display modded scores on the track complete screen
internal class CompleteScreenUI {
    private static XDLevelCompleteMenu levelCompleteMenu;
    private static TranslatedTextMeshPro accLabel;
    private static TranslatedTextMeshPro fcLabel;
    private static TranslatedTextMeshPro pfcLabel;
        
    public static void UpdateUI() {
        if (levelCompleteMenu == null || !levelCompleteMenu.gameObject.activeSelf)
            return;

        var scoreSystem = ScoreMod.CurrentScoreSystem;
        var container = scoreSystem.ScoreContainer;
            
        levelCompleteMenu.scoreValueText.SetText(container.Score.ToString());
        levelCompleteMenu.streakValueText.SetText(container.Streak.ToString());
        levelCompleteMenu.rankAnimator.Setup(container.Rank, null);
        levelCompleteMenu.pfcStatusText.SetText(container.FullComboState == FullComboState.PerfectFullCombo ? "PFC" : "FC");
        levelCompleteMenu.newBestGameObject.SetActive(container.IsHighScore);
        levelCompleteMenu.accuracyGameObject.SetActive(!string.IsNullOrWhiteSpace(container.PostGameInfo1Value));
        accLabel.textToAppend = scoreSystem.PostGameInfo1Name;
        levelCompleteMenu.accuracyBonusText.SetText(container.PostGameInfo1Value);
        levelCompleteMenu.FcBonusGameObject.SetActive(!string.IsNullOrWhiteSpace(container.PostGameInfo2Value));
        fcLabel.textToAppend = scoreSystem.PostGameInfo2Name;
        levelCompleteMenu.fcBonusText.SetText(container.PostGameInfo2Value);
        levelCompleteMenu.PfcBonusGameObject.SetActive(!string.IsNullOrWhiteSpace(container.PostGameInfo3Value));
        pfcLabel.textToAppend = scoreSystem.PostGameInfo3Name;
        levelCompleteMenu.pfcBonusText.SetText(container.PostGameInfo3Value);
    }

    private static int GetScore() => ScoreMod.CurrentScoreSystem.ScoreContainer.Score;

    private static int GetStreak() => ScoreMod.CurrentScoreSystem.ScoreContainer.Streak;

    private static FullComboState GetFullComboState() => ScoreMod.CurrentScoreSystem.ScoreContainer.FullComboState;

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
    private static IEnumerable<CodeInstruction> RankAnimator_Setup_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var CompleteScreenUI_GetFullComboState = typeof(CompleteScreenUI).GetMethod(nameof(GetFullComboState), BindingFlags.NonPublic | BindingFlags.Static);
        var PlayState_get_fullComboState = typeof(PlayState).GetProperty(nameof(PlayState.fullComboState)).GetGetMethod();

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_2, // playState
            instr => instr.Calls(PlayState_get_fullComboState)
        });

        foreach (var match in matches) {
            operations.Replace(match[0].Start, 2, new CodeInstruction[] {
                new (OpCodes.Call, CompleteScreenUI_GetFullComboState)
            });
        }
            
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(LevelCompleteCoreAnimationBehaviour), "AnimateText"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LevelCompleteCoreAnimationBehaviour_AnimateText_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var CompleteScreenUI_GetScore = typeof(CompleteScreenUI).GetMethod(nameof(GetScore), BindingFlags.NonPublic | BindingFlags.Static);
        var CompleteScreenUI_GetStreak = typeof(CompleteScreenUI).GetMethod(nameof(GetStreak), BindingFlags.NonPublic | BindingFlags.Static);
        var PlayState_get_TotalScore = typeof(PlayState).GetProperty(nameof(PlayState.TotalScore)).GetGetMethod();
        var PlayState_get_maxCombo = typeof(PlayState).GetProperty(nameof(PlayState.maxCombo)).GetGetMethod();

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_0, // playState
            instr => instr.Calls(PlayState_get_TotalScore)
        }).First()[0];
            
        operations.Replace(match.Start, 2, new CodeInstruction[] {
            new (OpCodes.Call, CompleteScreenUI_GetScore)
        });
            
        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_0, // playState
            instr => instr.Calls(PlayState_get_maxCombo)
        }).First()[0];
            
        operations.Replace(match.Start, 2, new CodeInstruction[] {
            new (OpCodes.Call, CompleteScreenUI_GetStreak)
        });
            
        operations.Execute(instructionsList);

        return instructionsList;
    }
}