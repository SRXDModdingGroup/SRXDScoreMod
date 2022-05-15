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

    public static void UpdateUI() {
        if (systemNameText != null)
            systemNameText.text = ScoreMod.CurrentScoreSystem.Name;
    }

    internal static void PlayCustomTimingFeedback(PlayState playState, CustomTimingAccuracy timingAccuracy) {
        if (!GameplayState.Playing)
            return;
        
        
    }

    private static TMP_Text GenerateText(GameObject baseObject, Vector3 position, float fontSize, Color color, string text = "") {
        var newObject = Object.Instantiate(baseObject, Vector3.zero, baseObject.transform.rotation, baseObject.transform.parent);

        newObject.transform.localPosition = position;
        newObject.transform.localScale = baseObject.transform.localScale;

        var textComponent = newObject.GetComponentInChildren<TMP_Text>();

        textComponent.color = color;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.horizontalAlignment = HorizontalAlignmentOptions.Right;
        textComponent.verticalAlignment = VerticalAlignmentOptions.Top;

        return textComponent;
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
            Object.Destroy(systemNameText);
        
        if (bestPossibleText != null)
            Object.Destroy(bestPossibleText);

        if (!showPace)
            return;

        var baseText = __instance.trackTitleText.gameObject;

        systemNameText = GenerateText(baseText, new Vector3(235f, 102f, 0f), 4f, Color.white);
        bestPossibleText = GenerateText(baseText, new Vector3(235f, 67f, 0f), 8f, Color.cyan);
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
            bestPossibleText.color = Color.cyan;
        else
            bestPossibleText.color = Color.gray * 0.75f;

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
}