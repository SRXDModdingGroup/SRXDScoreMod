using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SRXDScoreMod; 

// Contains patch functions to make the level select menu show modded scores
internal static class LevelSelectUI {
    internal static bool MenuOpen => currentLevelSelectMenu != null && currentLevelSelectMenu.isActiveAndEnabled;
    
    private static XDLevelSelectMenuBase currentLevelSelectMenu;
    
    public static void UpdateUI() {
        if (currentLevelSelectMenu == null || !currentLevelSelectMenu.isActiveAndEnabled)
            return;
        
        var handle = currentLevelSelectMenu.CurrentMetaDataHandle;
        
        if (handle == null)
            return;

        var scoreSystem = ScoreMod.CurrentScoreSystemInternal;
        var metadataSet = handle.TrackDataMetadata;
        int index = metadataSet.GetClosestActiveIndexForDifficulty(PlayerSettingsData.Instance.SelectedDifficulty);
        var metadata = metadataSet.GetMetadataForActiveIndex(index);
        var highScoreInfo = scoreSystem.GetHighScoreInfoForTrack(handle.TrackInfoRef, metadata);

        string score = highScoreInfo.GetScoreString();
        string rank = highScoreInfo.Rank;
        string streak = highScoreInfo.GetStreakString();

        foreach (var text in currentLevelSelectMenu.score)
            text.text = score;
        
        foreach (var text in currentLevelSelectMenu.rank)
            text.text = rank;
        
        foreach (var text in currentLevelSelectMenu.streak)
            text.text = streak;
    }
        
    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix(XDLevelSelectMenuBase __instance) {
        currentLevelSelectMenu = __instance;

        var scoreValueText = __instance.score[0];
        var parent = scoreValueText.transform.parent;

        if (parent.localScale.x >= 0.95f) {
            parent.localScale = 0.9f * Vector3.one;

            for (int i = 2; i < 7; i++) {
                var child = parent.GetChild(i);

                child.localPosition += 10f * Vector3.down;
            }
        }
        
        if (__instance.transform.Find("Score System"))
            return;

        var scoreSystemText = Object.Instantiate(scoreValueText.gameObject, __instance.transform, false).GetComponent<TMP_Text>();

        scoreSystemText.gameObject.name = "Score System";
        scoreSystemText.fontSize = 14f;
        scoreSystemText.alignment = TextAlignmentOptions.MidlineLeft;
        scoreSystemText.overflowMode = TextOverflowModes.Overflow;
        scoreSystemText.GetComponent<RectTransform>().offsetMax += new Vector2(500f, 0);
        scoreSystemText.transform.localPosition = new Vector3(1160f, 200f, 0f);
        scoreSystemText.raycastTarget = false;

        Plugin.CurrentSystem.BindAndInvoke(value => scoreSystemText.SetText($"(F1) {ScoreMod.ScoreSystems[value].Name}"));
    }

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), "FillOutCurrentTrackAndDifficulty"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var highScoreInfo = generator.DeclareLocal(typeof(HighScoreInfo));
        var scoreString = generator.DeclareLocal(typeof(string));
        var ScoreMod_get_CurrentScoreSystemInternal = typeof(ScoreMod).GetProperty(nameof(ScoreMod.CurrentScoreSystemInternal), BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var IScoreSystemInternal_GetHighScoreInfoForTrack = typeof(IScoreSystemInternal).GetMethod(nameof(IScoreSystemInternal.GetHighScoreInfoForTrack), new [] { typeof(TrackInfoAssetReference), typeof(TrackDataMetadata) });
        var HighScoreInfo_GetScoreString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetScoreString), BindingFlags.NonPublic | BindingFlags.Instance);
        var HighScoreInfo_GetStreakString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetStreakString), BindingFlags.NonPublic | BindingFlags.Instance);
        var HighScoreInfo_get_Rank = typeof(HighScoreInfo).GetProperty(nameof(HighScoreInfo.Rank)).GetGetMethod();
        var MetadataHandle_get_TrackInfoRef = typeof(MetadataHandle).GetProperty(nameof(MetadataHandle.TrackInfoRef)).GetGetMethod();
        
        var match0 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_0, // handle
            instr => instr.opcode == OpCodes.Callvirt, // trackInfoRef
            instr => instr.opcode == OpCodes.Callvirt,
            instr => instr.StoresLocalAtIndex(29) // stats
        }).First()[0];

        operations.Insert(match0.End, new CodeInstruction[] {
            new (OpCodes.Call, ScoreMod_get_CurrentScoreSystemInternal),
            new (OpCodes.Ldloc_0), // handle
            new (OpCodes.Callvirt, MetadataHandle_get_TrackInfoRef),
            new (OpCodes.Ldloc_S, (byte) 28), // metadata
            new (OpCodes.Callvirt, IScoreSystemInternal_GetHighScoreInfoForTrack),
            new (OpCodes.Stloc_S, highScoreInfo)
        });

        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(29), // stats
            instr => instr.Branches(out _)
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(14) // score
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(16) // rank
        }).First();

        int start = match1[0].Start;
        
        operations.Replace(start, match1[2].End - start, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_GetScoreString),
            new (OpCodes.Stloc_S, scoreString),
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_GetStreakString),
            new (OpCodes.Stloc_S, (byte) 15), // streak
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_get_Rank),
            new (OpCodes.Stloc_S, (byte) 16), // rank
        });

        var match2 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAddressAtIndex(14), // score
            instr => instr.opcode == OpCodes.Call // int.ToString
        }).First()[0];
        
        operations.Replace(match2.Start, 2, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, scoreString)
        });

        return operations.Enumerate(instructionsList);
    }
}