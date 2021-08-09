using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreMod {
    public class GameplayUI {
        private static bool spawnBestPossibleText;
        private static bool timingFeedbackSpawned;
        private static Color defaultScoreNumberColor;
        private static GameObject timingFeedbackObject;
        private static XDHudCanvases canvases;
        private static TextNumber scoreNumber;
        private static TextCharacter multiplierNumber;
        private static Image fcStar;
        private static Sprite fcSprite;
        private static Sprite pfcSprite;
        private static TMP_Text bestPossibleText;

        public static void UpdateUI() {
            if (!GameplayState.Playing)
                return;
            
            if (scoreNumber != null && scoreNumber.gameObject.activeSelf) {
                if (ModState.ShowModdedScore)
                    scoreNumber.color = Color.red;
                else
                    scoreNumber.color = defaultScoreNumberColor;
            }

            if (fcStar != null)
                fcStar.sprite = pfcSprite;
            
            if (bestPossibleText != null)
                bestPossibleText.gameObject.SetActive(ModState.ShowModdedScore);

            if (ModState.ShowModdedScore)
                multiplierNumber.Text = GetMultiplierAsText();
            else
                multiplierNumber.Text = $"{GameplayState.PlayState.multiplier}<size=65%>x</size>";
        }
        
        public static void UpdateMultiplierText() => multiplierNumber.Text = GetMultiplierAsText();

        public static void UpdateFcStar() => fcStar.sprite = fcSprite;

        private static string GetMultiplierAsText() => $"{ModState.CurrentContainer.Multiplier}<size=65%>x</size>";

        [HarmonyPatch(typeof(XDHudCanvases), nameof(XDHudCanvases.Start)), HarmonyPostfix]
        private static void XDHudCanvases_Start_Postfix(XDHudCanvases __instance) {
            if (scoreNumber == null)
                defaultScoreNumberColor = __instance.score.color;

            canvases = __instance;
            scoreNumber = __instance.score;
            multiplierNumber = __instance.multiplier;

            if (ModState.ShowModdedScore)
                scoreNumber.color = Color.red;
            else
                scoreNumber.color = defaultScoreNumberColor;

            fcStar = __instance.fcStar;
            fcSprite = __instance.fcStarSprite;
            pfcSprite = __instance.pfcStarSprite;
            spawnBestPossibleText = true;
        }
        
        [HarmonyPatch(typeof(TextNumber), nameof(TextNumber.Update)), HarmonyPrefix]
        private static bool TextNumber_Update_Prefix(TextNumber __instance) {
            if (!ModState.ShowModdedScore || !GameplayState.Playing || __instance != scoreNumber)
                return true;
            
            if (spawnBestPossibleText) {
                var timeLeftTextContainer = canvases.timeLeftText.transform.parent;
                var bestPossibleObject = Object.Instantiate(timeLeftTextContainer.gameObject, Vector3.zero, timeLeftTextContainer.rotation, timeLeftTextContainer.parent);
            
                bestPossibleObject.transform.localPosition = new Vector3(235f, 67f, 0f);
                bestPossibleObject.transform.localScale = timeLeftTextContainer.localScale;
                bestPossibleObject.SetActive(ModState.ShowModdedScore);
                bestPossibleText = bestPossibleObject.GetComponentInChildren<TMP_Text>();
                bestPossibleText.fontSize = 8f;
                bestPossibleText.overflowMode = TextOverflowModes.Overflow;
                bestPossibleText.horizontalAlignment = HorizontalAlignmentOptions.Right;
                spawnBestPossibleText = false;
            }

            var container = ModState.CurrentContainer;
            
            __instance.desiredNumber = container.Score;
            
            int pace = container.GetPace();
            string paceString;

            if (pace >= 0) {
                paceString = $"+{pace}";
                bestPossibleText.color = Color.cyan;
            }
            else {
                paceString = pace.ToString();
                bestPossibleText.color = Color.gray * 0.5f;
            }
            
            bestPossibleText.SetText($"Pace: {paceString,6}");

            return true;
        }
        
        [HarmonyPatch(typeof(TextCharacter), nameof(TextCharacter.Text), MethodType.Setter), HarmonyPrefix]
        private static bool TextCharacter_Text_Set_Prefix(TextCharacter __instance, ref string value) {
            if (ModState.ShowModdedScore && GameplayState.Playing && __instance == multiplierNumber)
                value = GetMultiplierAsText();

            return true;
        }

        [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter), HarmonyPrefix]
        private static bool Image_Sprite_Set_Prefix(Image __instance, ref Sprite value) {
            if (GameplayState.Playing && __instance == fcStar)
                value = GameplayState.PlayState.fullComboState == FullComboState.PerfectFullCombo && (!ModState.ShowModdedScore || ModState.CurrentContainer.GetIsPfc(false)) ? pfcSprite : fcSprite;

            return true;
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
    }
}