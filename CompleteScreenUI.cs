using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ScoreMod {
    public class CompleteScreenUI {
        private static bool completeMenuLoaded;
        private static string realRank;
        private static XDLevelCompleteMenu levelCompleteMenu;
        private static TMP_Text pfcLabel;
        
        public static void UpdateUI() {
            if (levelCompleteMenu == null || !levelCompleteMenu.gameObject.activeSelf)
                return;

            if (ModState.ShowModdedScore) {
                levelCompleteMenu.pfcBonusText.SetText(ModState.CurrentContainer.Profile.Name);
                levelCompleteMenu.accuracyBonusText.SetText(ModState.CurrentContainer.GetAccuracyRating().ToString("P"));
                levelCompleteMenu.PfcBonusGameObject.SetActive(true);
                levelCompleteMenu.accuracyGameObject.SetActive(true);
                levelCompleteMenu.pfcStatusText.SetText(ModState.CurrentContainer.GetIsPfc() ? "PFC" : "FC");
                levelCompleteMenu.scoreValueText.SetText(ModState.CurrentContainer.Score.ToString());
                levelCompleteMenu.rankAnimator.SetText(ModState.CurrentContainer.GetRank());
                levelCompleteMenu.newBestGameObject.SetActive(ModState.CurrentContainer.GetIsHighScore());
                pfcLabel.SetText("Current Profile");
            }
            else {
                bool realIsPfc = GameplayState.PlayState.fullComboState == FullComboState.PerfectFullCombo;

                levelCompleteMenu.pfcBonusText.SetText(GameplayState.PlayState.scoreState.PfcBonus.ToString());
                levelCompleteMenu.accuracyBonusText.SetText(GameplayState.PlayState.scoreState.AccuracyBonus.ToString());
                levelCompleteMenu.PfcBonusGameObject.SetActive(realIsPfc);
                levelCompleteMenu.accuracyGameObject.SetActive(GameplayState.PlayState.scoreState.AccuracyBonus > 0);
                levelCompleteMenu.pfcStatusText.SetText(realIsPfc ? "PFC" : "FC");
                levelCompleteMenu.scoreValueText.SetText(GameplayState.PlayState.TotalScore.ToString());
                levelCompleteMenu.rankAnimator.SetText(realRank);
                levelCompleteMenu.newBestGameObject.SetActive(levelCompleteMenu.newBest);
                pfcLabel.SetText("PFC");
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
                

            return true;
        }
    }
}