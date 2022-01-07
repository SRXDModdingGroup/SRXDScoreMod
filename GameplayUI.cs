using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SRXDScoreMod {
    // Contains patch functions to show modded scores and pace prediction on the in-game HUD
    public class GameplayUI {
        private enum PaceType {
            Score,
            Delta,
            Both
        }
        
        private static bool spawnedBestPossibleText;
        private static bool timingFeedbackSpawned;
        private static bool showPace;
        private static PaceType paceType;
        private static GameObject timingFeedbackObject;
        private static TMP_Text bestPossibleText;

        public static void UpdateUI() {
            if (!GameplayState.Playing)
                return;

            if (bestPossibleText != null)
                bestPossibleText.gameObject.SetActive(ModState.ShowModdedScore && showPace);
        }

        [HarmonyPatch(typeof(XDHudCanvases), nameof(XDHudCanvases.Start)), HarmonyPostfix]
        private static void XDHudCanvases_Start_Postfix(XDHudCanvases __instance) {
            showPace = Main.PaceType.Value != "Hide";

            if (!showPace || spawnedBestPossibleText)
                return;

            var timeLeftTextContainer = __instance.timeLeftText.transform.parent;
            var bestPossibleObject = Object.Instantiate(timeLeftTextContainer.gameObject, Vector3.zero, timeLeftTextContainer.rotation, timeLeftTextContainer.parent);
            
            bestPossibleObject.transform.localPosition = new Vector3(235f, 67f, 0f);
            bestPossibleObject.transform.localScale = timeLeftTextContainer.localScale;
            bestPossibleObject.SetActive(ModState.ShowModdedScore);
            bestPossibleText = bestPossibleObject.GetComponentInChildren<TMP_Text>();
            bestPossibleText.fontSize = 8f;
            bestPossibleText.overflowMode = TextOverflowModes.Overflow;
            bestPossibleText.horizontalAlignment = HorizontalAlignmentOptions.Right;
            bestPossibleText.verticalAlignment = VerticalAlignmentOptions.Top;
            spawnedBestPossibleText = true;

            switch (Main.PaceType.Value) {
                case "Score":
                    paceType = PaceType.Score;

                    break;
                case "Delta":
                    paceType = PaceType.Delta;

                    break;
                case "Both":
                    paceType = PaceType.Both;

                    break;
            }
        }

        [HarmonyPatch(typeof(TrackGameplayFeedbackObjects), nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback)), HarmonyPrefix]
        private static bool TrackGameplayFeedbackObjects_PlayTimingFeedback_Prefix(ref NoteTimingAccuracy noteTimingAccuracy) {
            if (noteTimingAccuracy == NoteTimingAccuracy.Pending ||
                noteTimingAccuracy == NoteTimingAccuracy.Valid ||
                noteTimingAccuracy == NoteTimingAccuracy.Failed ||
                noteTimingAccuracy == NoteTimingAccuracy.Invalidated)
                return true;

            timingFeedbackSpawned = true;

            if (!ModState.ShowModdedScore)
                return true;

            switch (ModState.LastAccuracy) {
                case Accuracy.Perfect:
                case Accuracy.Great:
                    noteTimingAccuracy = NoteTimingAccuracy.Perfect;

                    break;
                case Accuracy.Good:
                case Accuracy.Okay: 
                    if (GameplayState.LastOffset > 0f)
                        noteTimingAccuracy = NoteTimingAccuracy.Late;
                    else
                        noteTimingAccuracy = NoteTimingAccuracy.Early;

                    break;
            }

            return true;
        }
        
        [HarmonyPatch(typeof(TrackGameplayFeedbackObjects), nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback)), HarmonyPostfix]
        private static void TrackGameplayFeedbackObjects_PlayTimingFeedback_Postfix(NoteTimingAccuracy noteTimingAccuracy) {
            if (noteTimingAccuracy == NoteTimingAccuracy.Pending ||
                noteTimingAccuracy == NoteTimingAccuracy.Valid ||
                noteTimingAccuracy == NoteTimingAccuracy.Failed ||
                noteTimingAccuracy == NoteTimingAccuracy.Invalidated ||
                timingFeedbackObject == null)
                return;

            string newText;
            int target;
            
            if (ModState.ShowModdedScore) {
                switch (ModState.LastAccuracy) {
                    case Accuracy.Perfect:
                        newText = "Perfect";
                        target = 3;
                    
                        break;
                    case Accuracy.Great:
                        newText = "Great";
                        target = 3;

                        break;
                    case Accuracy.Good:
                        newText = "Good";

                        if (GameplayState.LastOffset > 0f)
                            target = 2;
                        else
                            target = 1;
                    
                        break;
                    default:
                        newText = "Okay";
                        
                        if (GameplayState.LastOffset > 0f)
                            target = 2;
                        else
                            target = 1;
                    
                        break;
                }
            }
            else {
                switch (noteTimingAccuracy) {
                    case NoteTimingAccuracy.Perfect:
                        newText = "Perfect";
                        target = 3;
                        
                        break;
                    case NoteTimingAccuracy.Early:
                        newText = "Early";
                        target = 1;
                        
                        break;
                    default:
                        newText = "Late";
                        target = 2;
                        
                        break;
                }
            }

            var transform = timingFeedbackObject.transform.GetChild(target);
            var feedbackText = transform.GetComponent<TextCharacter>();

            feedbackText.Text = newText;
            timingFeedbackObject = null;
        }
        
        [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive)), HarmonyPostfix]
        private static void GameObject_SetActive_Postfix(GameObject __instance, bool value) {
            if (!timingFeedbackSpawned || !value || __instance.name != "AccuracyEffectXD(Clone)")
                return;

            timingFeedbackObject = __instance;
            timingFeedbackSpawned = false;
        }

        [HarmonyPatch(typeof(Track), nameof(Track.UpdateUI)), HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Track_UpdateUI_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionsList = new List<CodeInstruction>(instructions);
            var currentScoreSystem = generator.DeclareLocal(typeof(IReadOnlyScoreSystem));
            var PlayState_get_TotalNoteScore = typeof(PlayState).GetProperty(nameof(PlayState.TotalNoteScore)).GetGetMethod();
            var PlayState_get_combo = typeof(PlayState).GetProperty(nameof(PlayState.combo)).GetGetMethod();
            var PlayState_get_multiplier = typeof(PlayState).GetProperty(nameof(PlayState.multiplier)).GetGetMethod();
            var PlayState_get_fullComboState = typeof(PlayState).GetProperty(nameof(PlayState.fullComboState)).GetGetMethod();
            var Main_get_CurrentScoreSystem = typeof(Main).GetProperty(nameof(Main.CurrentScoreSystem)).GetGetMethod();
            var IReadOnlyScoreSystem_get_Score = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.Score)).GetGetMethod();
            var IReadOnlyScoreSystem_get_Streak = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.Streak)).GetGetMethod();
            var IReadOnlyScoreSystem_get_Multiplier = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.Multiplier)).GetGetMethod();
            var IReadOnlyScoreSystem_get_StarState = typeof(IReadOnlyScoreSystem).GetProperty(nameof(IReadOnlyScoreSystem.StarState)).GetGetMethod();
            
            instructionsList.InsertRange(0, new CodeInstruction[] {
                new(OpCodes.Call, Main_get_CurrentScoreSystem),
                new(OpCodes.Stloc_S, currentScoreSystem)
            });
            
            ReplaceGetter(PlayState_get_TotalNoteScore, currentScoreSystem, IReadOnlyScoreSystem_get_Score);
            ReplaceGetter(PlayState_get_combo, currentScoreSystem, IReadOnlyScoreSystem_get_Streak);
            ReplaceGetter(PlayState_get_multiplier, currentScoreSystem, IReadOnlyScoreSystem_get_Multiplier);
            ReplaceGetter(PlayState_get_fullComboState, currentScoreSystem, IReadOnlyScoreSystem_get_StarState);

            return instructionsList;

            void ReplaceGetter(MethodInfo fromMethod, LocalBuilder toLocal, MethodInfo toMethod) {
                var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
                    instr => instr.IsLdloc(),
                    instr => instr.Calls(fromMethod)
                });

                foreach (var match in matches) {
                    int start = match.Start;

                    instructionsList[start].operand = toLocal;
                    instructionsList[start + 1] = new CodeInstruction(OpCodes.Call, toMethod);
                }
            }
        }

        [HarmonyPatch(typeof(Track), nameof(Track.UpdateUI)), HarmonyPostfix]
        private static void Track_UpdateUI_Postfix() {
            if (!showPace || !spawnedBestPossibleText)
                return;
            
            var currentScoreSystem = Main.CurrentScoreSystem;

            if (GameplayState.PlayState.isInPracticeMode || !currentScoreSystem.ImplementsScorePrediction) {
                bestPossibleText.gameObject.SetActive(false);

                return;
            }
            
            bestPossibleText.gameObject.SetActive(true);

            int bestPossible = currentScoreSystem.MaxScore + currentScoreSystem.Score - currentScoreSystem.MaxScoreSoFar;
            int delta = bestPossible - currentScoreSystem.HighScore;
            
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
}