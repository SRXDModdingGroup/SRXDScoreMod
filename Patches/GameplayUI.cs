using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SRXDScoreMod;

// Contains patch functions to show modded scores and pace prediction on the in-game HUD
internal class GameplayUI {
    private static readonly int FACE_COLOR = Shader.PropertyToID("_FaceColor");
    
    public enum PaceType {
        Hide,
        Score,
        Delta,
        Both
    }
    
    private static bool showPace;
    private static PaceType paceType;
    private static TMP_Text systemNameText;
    private static TMP_Text bestPossibleText;
    private static CustomTimingAccuracy customTimingAccuracy;

    public static void UpdateUI() {
        if (systemNameText != null)
            systemNameText.text = ScoreMod.CurrentScoreSystem.Name;
    }

    internal static void PlayCustomTimingFeedback(PlayState playState, CustomTimingAccuracy timingAccuracy) {
        customTimingAccuracy = timingAccuracy;
        playState.PlayTimingFeedback(timingAccuracy.BaseAccuracy);
    }

    private static TMP_Text GenerateText(string name, GameObject baseObject, Vector3 position, float fontSize, Color color, string text = "") {
        var newObject = Object.Instantiate(baseObject, Vector3.zero, baseObject.transform.rotation, baseObject.transform.parent);

        newObject.name = name;
        newObject.transform.localPosition = position;
        newObject.transform.localScale = baseObject.transform.localScale;
        newObject.GetComponent<HdrMeshEffect>().colorIndex = 0;

        var textComponent = newObject.GetComponentInChildren<TMP_Text>();

        textComponent.color = color;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.horizontalAlignment = HorizontalAlignmentOptions.Right;
        textComponent.verticalAlignment = VerticalAlignmentOptions.Top;

        return textComponent;
    }

    private static AccuracyLogInstance ModifyAccuracyLogInstance(AccuracyLogInstance instance) {
        foreach (var material in instance.materials)
            material.SetColor(FACE_COLOR, 4f * customTimingAccuracy.Color);

        return instance;
    }

    [HarmonyPatch(typeof(DomeHud), "Update"), HarmonyPrefix]
    private static bool DomeHud_Update_Prefix(DomeHud __instance) {
        var scoreSystem = ScoreMod.CurrentScoreSystemInternal;
        
        __instance.number.desiredNumber = scoreSystem.Score;
        __instance.multiplier.desiredNumber = scoreSystem.Multiplier;
        __instance.streak.desiredNumber = scoreSystem.Streak;

        int fullComboState = (int) scoreSystem.FullComboState;

        foreach (var animator in __instance.animators)
            animator.SetIntegerIfNeeded(Wheel.Hashes.PFCStar, fullComboState);

        return false;
    }

    [HarmonyPatch(typeof(DomeHud), nameof(DomeHud.Init)), HarmonyPostfix]
    private static void DomeHid_Init_Postfix(DomeHud __instance) {
        paceType = Plugin.PaceType.Value;
        showPace = paceType != PaceType.Hide;
        
        if (systemNameText != null)
            Object.Destroy(systemNameText.gameObject);
        
        if (bestPossibleText != null)
            Object.Destroy(bestPossibleText.gameObject);

        if (!showPace)
            return;

        var baseText = __instance.trackTitleText.gameObject;

        systemNameText = GenerateText("System Name", baseText, new Vector3(180f, 390f, 0f), 4f, Color.white);
        bestPossibleText = GenerateText("Best Possible", baseText, new Vector3(180f, 360f, 0f), 8f, 4f * Color.cyan);
        bestPossibleText.gameObject.SetActive(ScoreMod.CurrentScoreSystemInternal.ImplementsScorePrediction);
        UpdateUI();
    }

    [HarmonyPatch(typeof(Track), nameof(Track.UpdateUI)), HarmonyPostfix]
    private static void Track_UpdateUI_Postfix(Track __instance) {
        if (!showPace)
            return;

        var playState = __instance.playStateFirst;

        if (playState.isInPracticeMode || __instance.IsInEditMode) {
            bestPossibleText.gameObject.SetActive(false);
            
            return;
        }

        var scoreSystem = ScoreMod.CurrentScoreSystemInternal;

        if (!scoreSystem.ImplementsScorePrediction) {
            bestPossibleText.gameObject.SetActive(false);

            return;
        }
            
        bestPossibleText.gameObject.SetActive(true);

        int bestPossible = scoreSystem.Score + scoreSystem.MaxPossibleScore - scoreSystem.MaxPossibleScoreSoFar;
        int delta = bestPossible - scoreSystem.HighScore;
            
        if (delta >= 0)
            bestPossibleText.color = 4f * Color.cyan;
        else
            bestPossibleText.color = Color.gray;

        if (paceType == PaceType.Delta || paceType == PaceType.Both) {
            string paceString;
                
            if (delta >= 0)
                paceString = $"+{delta}";
            else
                paceString = delta.ToString();

            if (paceType == PaceType.Both)
                bestPossibleText.SetText($"Pace: {bestPossible.ToString(),7}\n{paceString}");
            else
                bestPossibleText.SetText($"Pace: {paceString,7}");
        }
        else
            bestPossibleText.SetText($"Pace: {bestPossible.ToString(),7}");
    }

    [HarmonyPatch(typeof(DomeHud), nameof(DomeHud.AddToAccuracyLog)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DomeHid_AddToAccuracyLog_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = instructions.ToList();
        var operations = new EnumerableOperation<CodeInstruction>();
        var AccuracyLogType_Get = typeof(AccuracyLogType).GetMethod(nameof(AccuracyLogType.Get));
        var GameplayUI_ModifyAccuracyLogInstance = typeof(GameplayUI).GetMethod(nameof(ModifyAccuracyLogInstance), BindingFlags.NonPublic | BindingFlags.Static);

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(AccuracyLogType_Get)
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new(OpCodes.Call, GameplayUI_ModifyAccuracyLogInstance)
        });

        return operations.Enumerate(instructionsList);
    }
}