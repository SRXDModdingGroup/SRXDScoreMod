using HarmonyLib;
using UnityEngine;

namespace ScoreMod {
    public class UnityObjects {
        [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive)), HarmonyPostfix]
        private static void GameObject_SetActive_Postfix(GameObject __instance, bool value) {
            if (CompleteScreenUI.CheckCompleteMenuClosed(value, __instance))
                return;

            GameplayUI.CheckTimingFeedbackSpawned(value, __instance);
        }
    }
}