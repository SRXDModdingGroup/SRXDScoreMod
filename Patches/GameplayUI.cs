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
#pragma warning disable CS0649
    private static Animation timingFeedbackAnimation;
#pragma warning restore CS0649
    private static TMP_Text systemNameText;
    private static TMP_Text bestPossibleText;
    private static Dictionary<TextCharacter, Color> colorOverrides = new();

    public static void UpdateUI() {
        if (systemNameText != null)
            systemNameText.text = ScoreMod.CurrentScoreSystem.Name;
    }

    internal static void PlayCustomTimingFeedback(PlayState playState, CustomTimingAccuracy timingAccuracy) {
        if (!GameplayState.Playing)
            return;
        
        var baseAccuracy = timingAccuracy.BaseAccuracy;
            
        TrackGameplayFeedbackObjects.PlayTimingFeedback(playState, baseAccuracy);

        int target = baseAccuracy switch {
            NoteTimingAccuracy.Perfect => 3,
            NoteTimingAccuracy.Early => 1,
            NoteTimingAccuracy.Late => 2,
            _ => 3
        };

        var transform = timingFeedbackAnimation.transform.GetChild(target);
        var feedbackText = transform.GetComponent<TextCharacter>();

        feedbackText.Text = timingAccuracy.DisplayName;
        colorOverrides[feedbackText] = timingAccuracy.Color;
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

    [HarmonyPatch(typeof(TextCharacter), "LateUpdate"), HarmonyPrefix]
    private static void TextCharacter_LateUpdate_Prefix(TextCharacter __instance) {
        if (colorOverrides.TryGetValue(__instance, out var color))
            __instance.color = color.WithA(__instance.color.a);
    }

    [HarmonyPatch(typeof(XDHudCanvases), nameof(XDHudCanvases.Start)), HarmonyPostfix]
    private static void XDHudCanvases_Start_Postfix(XDHudCanvases __instance) {
        paceType = Plugin.PaceType.Value;
        showPace = paceType != PaceType.Hide;
        
        if (systemNameText != null)
            Object.Destroy(systemNameText);
        
        if (bestPossibleText != null)
            Object.Destroy(bestPossibleText);

        if (!showPace)
            return;

        var baseText = __instance.timeLeftText.transform.parent.gameObject;

        systemNameText = GenerateText(baseText, new Vector3(235f, 102f, 0f), 4f, Color.white);
        bestPossibleText = GenerateText(baseText, new Vector3(235f, 67f, 0f), 8f, Color.cyan);
        bestPossibleText.gameObject.SetActive(ScoreMod.CurrentScoreSystemInternal.ImplementsScorePrediction);
        UpdateUI();
    }

    [HarmonyPatch(typeof(TrackGameplayFeedbackObjects), nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayFeedbackObjects_PlayTimingFeedback_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var TrackGameplayFeedbackObjects_PlayFeedbackAnimation = typeof(TrackGameplayFeedbackObjects).GetMethod(nameof(TrackGameplayFeedbackObjects.PlayFeedbackAnimation));
        var GameplayUI_timingFeedbackAnimation = typeof(GameplayUI).GetField(nameof(timingFeedbackAnimation), BindingFlags.NonPublic | BindingFlags.Static);
            
        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayFeedbackObjects_PlayFeedbackAnimation),
            instr => instr.IsStloc()
        }).First()[0];
            
        instructionsList.InsertRange(match.End, new [] {
            new CodeInstruction(OpCodes.Ldloc_S, (byte) 8), // animation
            new CodeInstruction(OpCodes.Stsfld, GameplayUI_timingFeedbackAnimation)
        });

        return instructionsList;
    }

    [HarmonyPatch(typeof(Track), nameof(Track.UpdateUI)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Track_UpdateUI_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var currentScoreSystem = generator.DeclareLocal(typeof(IScoreSystem));
        var PlayState_get_TotalNoteScore = typeof(PlayState).GetProperty(nameof(PlayState.TotalNoteScore)).GetGetMethod();
        var PlayState_get_combo = typeof(PlayState).GetProperty(nameof(PlayState.combo)).GetGetMethod();
        var PlayState_get_multiplier = typeof(PlayState).GetProperty(nameof(PlayState.multiplier)).GetGetMethod();
        var PlayState_get_fullComboState = typeof(PlayState).GetProperty(nameof(PlayState.fullComboState)).GetGetMethod();
        var XDHudCanvases_fcStar = typeof(XDHudCanvases).GetField(nameof(XDHudCanvases.fcStar));
        var Graphic_set_color = typeof(Graphic).GetProperty(nameof(Graphic.color)).GetSetMethod();
        var ScoreMod_get_CurrentScoreSystemInternal = typeof(ScoreMod).GetProperty(nameof(ScoreMod.CurrentScoreSystemInternal), BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var IScoreSystem_get_Score = typeof(IScoreSystem).GetProperty(nameof(IScoreSystem.Score)).GetGetMethod();
        var IScoreSystem_get_Streak = typeof(IScoreSystem).GetProperty(nameof(IScoreSystem.Streak)).GetGetMethod();
        var IScoreSystem_get_Multiplier = typeof(IScoreSystem).GetProperty(nameof(IScoreSystem.Multiplier)).GetGetMethod();
        var IScoreSystemInternal_get_StarState = typeof(IScoreSystemInternal).GetProperty(nameof(IScoreSystemInternal.StarState)).GetGetMethod();
        var IScoreSystemInternal_get_StarColor = typeof(IScoreSystemInternal).GetProperty(nameof(IScoreSystemInternal.StarColor)).GetGetMethod();
        
        operations.Insert(0, new CodeInstruction[] {
            new(OpCodes.Call, ScoreMod_get_CurrentScoreSystemInternal),
            new(OpCodes.Stloc_S, currentScoreSystem)
        });

        var match0 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsField(XDHudCanvases_fcStar)
        }).Then( new Func<CodeInstruction, bool>[] {
            instr => instr.Branches(out _)
        }).First()[1];
            
        operations.Insert(match0.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 15), // hudCanvases
            new (OpCodes.Ldfld, XDHudCanvases_fcStar),
            new (OpCodes.Ldloc_S, currentScoreSystem),
            new (OpCodes.Callvirt, IScoreSystemInternal_get_StarColor),
            new (OpCodes.Callvirt, Graphic_set_color)
        });
            
        ReplaceGetter(PlayState_get_TotalNoteScore, IScoreSystem_get_Score);
        ReplaceGetter(PlayState_get_combo, IScoreSystem_get_Streak);
        ReplaceGetter(PlayState_get_multiplier, IScoreSystem_get_Multiplier);
        ReplaceGetter(PlayState_get_fullComboState, IScoreSystemInternal_get_StarState);
            
        operations.Execute(instructionsList);

        return instructionsList;

        void ReplaceGetter(MethodInfo fromMethod, MethodInfo toMethod) {
            var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
                instr => instr.IsLdloc(),
                instr => instr.Calls(fromMethod)
            });

            foreach (var match1 in matches) {
                int start = match1[0].Start;

                instructionsList[start].operand = currentScoreSystem;
                instructionsList[start + 1] = new CodeInstruction(OpCodes.Callvirt, toMethod);
            }
        }
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