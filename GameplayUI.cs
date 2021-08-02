using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreMod {
    public class GameplayUI {
        private static bool timingFeedbackSpawned;
        private static Color defaultScoreNumberColor;
        private static GameObject timingFeedbackObject;
        private static TextNumber scoreNumber;
        private static TextCharacter multiplierNumber;
        private static Image fcStar;
        private static Sprite fcSprite;
        private static Sprite pfcSprite;

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

            if (ModState.ShowModdedScore)
                multiplierNumber.Text = GetMultiplierAsText();
            else
                multiplierNumber.Text = $"{GameplayState.PlayState.multiplier}<size=65%>x</size>";
        }
        
        public static void UpdateMultiplierText() => multiplierNumber.Text = GetMultiplierAsText();

        public static void UpdateFcStar() => fcStar.sprite = fcSprite;

        public static void CheckTimingFeedbackSpawned(bool value, GameObject instance) {
            if (!timingFeedbackSpawned || !value || instance.name != "AccuracyEffectXD(Clone)")
                return;

            timingFeedbackObject = instance;
            timingFeedbackSpawned = false;
        }

        private static string GetMultiplierAsText() => $"{ModState.CurrentContainer.Multiplier}<size=65%>x</size>";

        [HarmonyPatch(typeof(XDHudCanvases), nameof(XDHudCanvases.Start)), HarmonyPostfix]
        private static void XDHudCanvases_Start_Postfix(XDHudCanvases __instance) {
            if (scoreNumber == null)
                defaultScoreNumberColor = __instance.score.color;

            scoreNumber = __instance.score;
            multiplierNumber = __instance.multiplier;

            if (ModState.ShowModdedScore)
                scoreNumber.color = Color.red;
            else
                scoreNumber.color = defaultScoreNumberColor;

            fcStar = __instance.fcStar;
            fcSprite = __instance.fcStarSprite;
            pfcSprite = __instance.pfcStarSprite;
        }
        
        [HarmonyPatch(typeof(TextNumber), nameof(TextNumber.Update)), HarmonyPrefix]
        private static bool TextNumber_Update_Prefix(TextNumber __instance) {
            if (ModState.ShowModdedScore && GameplayState.Playing && __instance == scoreNumber)
                __instance.desiredNumber = ModState.CurrentContainer.Score;

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
                value = GameplayState.PlayState.fullComboState == FullComboState.PerfectFullCombo && (!ModState.ShowModdedScore || ModState.CurrentContainer.GetIsPfc()) ? pfcSprite : fcSprite;

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
                case ScoreContainer.Accuracy.Perfect:
                case ScoreContainer.Accuracy.Great:
                    noteTimingAccuracy = NoteTimingAccuracy.Perfect;

                    break;
                case ScoreContainer.Accuracy.Good:
                case ScoreContainer.Accuracy.Okay: 
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
                    case ScoreContainer.Accuracy.Perfect:
                        newText = "Perfect";
                        target = 3;
                    
                        break;
                    case ScoreContainer.Accuracy.Great:
                        newText = "Great";
                        target = 3;

                        break;
                    case ScoreContainer.Accuracy.Good:
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
    }
}