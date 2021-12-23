using HarmonyLib;
using TMPro;

namespace SRXDScoreMod {
    // Contains patch functions to display modded scores on the track complete screen
    public class CompleteScreenUI {
        private static bool completeMenuLoaded;
        private static string realRank;
        private static XDLevelCompleteMenu levelCompleteMenu;
        private static TMP_Text pfcLabel;
        
        public static void UpdateUI() {
            if (levelCompleteMenu == null || !levelCompleteMenu.gameObject.activeSelf)
                return;

            if (ModState.ShowModdedScore) {
                var container = ModState.CurrentContainer;
                
                pfcLabel.SetText("Current Profile");
                levelCompleteMenu.PfcBonusGameObject.SetActive(true);
                levelCompleteMenu.pfcBonusText.SetText(container.Profile.Name);
                levelCompleteMenu.accuracyGameObject.SetActive(true);
                levelCompleteMenu.accuracyBonusText.SetText($"{container.GetAccuracyRating():P} +{container.SuperPerfects}");
                levelCompleteMenu.pfcStatusText.SetText(container.GetIsPfc(true) ? "PFC" : "FC");
                levelCompleteMenu.scoreValueText.SetText(container.Score.ToString());
                levelCompleteMenu.rankAnimator.SetText(container.GetRank());
                levelCompleteMenu.newBestGameObject.SetActive(container.GetIsHighScore());
                levelCompleteMenu.extendedStats.translatedRhythmHeader.text.SetText("Rhythm ( : )");
            }
            else {
                bool realIsPfc = GameplayState.PlayState.fullComboState == FullComboState.PerfectFullCombo;
                var scoreState = GameplayState.PlayState.scoreState;
                
                pfcLabel.SetText("PFC");
                levelCompleteMenu.pfcBonusText.SetText(scoreState.PfcBonus.ToString());
                levelCompleteMenu.accuracyBonusText.SetText(scoreState.AccuracyBonus.ToString());
                levelCompleteMenu.PfcBonusGameObject.SetActive(realIsPfc);
                levelCompleteMenu.accuracyGameObject.SetActive(scoreState.AccuracyBonus > 0);
                levelCompleteMenu.pfcStatusText.SetText(realIsPfc ? "PFC" : "FC");
                levelCompleteMenu.scoreValueText.SetText(GameplayState.PlayState.TotalScore.ToString());
                levelCompleteMenu.rankAnimator.SetText(realRank);
                levelCompleteMenu.newBestGameObject.SetActive(levelCompleteMenu.newBest);
                levelCompleteMenu.extendedStats.translatedRhythmHeader.ChangeText();
            }
        }

        [HarmonyPatch(typeof(XDLevelCompleteMenu), nameof(XDLevelCompleteMenu.Setup)), HarmonyPostfix]
        private static void XDLevelCompleteMenu_Setup_Postfix(XDLevelCompleteMenu __instance, PlayState playState) {
            levelCompleteMenu = __instance;
            completeMenuLoaded = true;
            realRank = __instance.rankAnimator.rankText.text;
            pfcLabel = __instance.PfcBonusGameObject.GetComponentsInChildren<TMP_Text>()[0];
            UpdateUI();
        }

        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter), HarmonyPrefix]
        private static bool TMP_Text_SetTextInternal_Prefix(TMP_Text __instance, ref string value) {
            if (!ModState.ShowModdedScore || !completeMenuLoaded || !levelCompleteMenu.gameObject.activeSelf)
                return true;
                
            if (__instance == levelCompleteMenu.scoreValueText)
                value = ModState.CurrentContainer.Score.ToString();
            else if (__instance == pfcLabel)
                value = "Current Profile";
            else if (__instance == levelCompleteMenu.extendedStats.translatedRhythmHeader.text) {
                ModState.CurrentContainer.GetEarlyLateBalance(out int early, out int late);
                value = $"Rhythm ({early} : {late})";
            }
                

            return true;
        }
    }
}