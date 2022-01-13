using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using SMU.Utilities;
using TMPro;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains patch functions to make the level select menu show modded scores
internal class LevelSelectUI {
    private static Action XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty
        = ReflectionUtils.MethodToAction(typeof(XDLevelSelectMenuBase), "FillOutCurrentTrackAndDifficulty");

    public static void UpdateUI() {
        XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty();
    }
        
    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix(XDLevelSelectMenuBase __instance) {
        var parent = __instance.score[0].transform.parent;

        if (parent.localScale.x < 0.95f)
            return;
        
        parent.localScale = 0.9f * Vector3.one;
                
        for (int i = 2; i < 7; i++) {
            var child = parent.GetChild(i);

            child.localPosition += 10f * Vector3.down;
        }
    }

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), "FillOutCurrentTrackAndDifficulty"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var highScoreInfo = generator.DeclareLocal(typeof(HighScoreInfo));
        var IScoreSystem_GetHighScoreInfoForTrack = typeof(IScoreSystem).GetMethod(nameof(IScoreSystem.GetHighScoreInfoForTrack));
        var HighScoreInfo_GetScoreString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetScoreString));
        var HighScoreInfo_GetStreakString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetStreakString));
        var HighScoreInfo_get_Rank = typeof(HighScoreInfo).GetProperty(nameof(HighScoreInfo.Rank)).GetGetMethod();

        var match0 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldloc_0, // handle
            instr => instr.opcode == OpCodes.Callvirt, // trackInfoRef
            instr => instr.opcode == OpCodes.Callvirt,
            instr => instr.Is(OpCodes.Stloc_S, (byte) 29) // stats
        }).First()[0];
        
        operations.Replace(match0.Start + 2, 2, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 28), // metadata
            new (OpCodes.Callvirt, IScoreSystem_GetHighScoreInfoForTrack),
            new (OpCodes.Stloc_S, highScoreInfo)
        });

        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Is(OpCodes.Ldloc_S, (byte) 29) // stats
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Is(OpCodes.Stloc_S, (byte) 14) // score
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Is(OpCodes.Stloc_S, (byte) 16) // rank
        }).First();

        int start = match1[0].Start;
        
        operations.Replace(start, match1[2].End - start, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_GetScoreString),
            new (OpCodes.Stloc_S, (byte) 14), // score
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_GetStreakString),
            new (OpCodes.Stloc_S, (byte) 15), // streak
            new (OpCodes.Ldloc_S, highScoreInfo),
            new (OpCodes.Call, HighScoreInfo_get_Rank),
            new (OpCodes.Stloc_S, (byte) 16), // rank
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }
}