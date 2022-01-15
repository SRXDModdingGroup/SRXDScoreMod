﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using SMU.Utilities;
using TMPro;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains patch functions to make the level select menu show modded scores
internal class LevelSelectUI {
    // private static Action<XDLevelSelectMenuBase> XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty
    //     = ReflectionUtils.MethodToAction<XDLevelSelectMenuBase>(typeof(XDLevelSelectMenuBase), "FillOutCurrentTrackAndDifficulty");

    private static XDLevelSelectMenuBase currentLevelSelectMenu;
    
    public static void UpdateUI() {
        // if (currentLevelSelectMenu != null && currentLevelSelectMenu.isActiveAndEnabled)
        //     XDLevelSelectMenuBase_FillOutCurrentTrackAndDifficulty(currentLevelSelectMenu);
    }
        
    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix(XDLevelSelectMenuBase __instance) {
        currentLevelSelectMenu = __instance;
        
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
        var scoreString = generator.DeclareLocal(typeof(string));
        var ScoreMod_get_CurrentScoreSystemInternal = typeof(ScoreMod).GetProperty(nameof(ScoreMod.CurrentScoreSystemInternal), BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var IScoreSystem_GetHighScoreInfoForTrack = typeof(IScoreSystem).GetMethod(nameof(IScoreSystem.GetHighScoreInfoForTrack), new [] { typeof(TrackInfoAssetReference), typeof(TrackDataMetadata) });
        var HighScoreInfo_GetScoreString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetScoreString));
        var HighScoreInfo_GetStreakString = typeof(HighScoreInfo).GetMethod(nameof(HighScoreInfo.GetStreakString));
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
            new (OpCodes.Callvirt, IScoreSystem_GetHighScoreInfoForTrack),
            new (OpCodes.Stloc_S, highScoreInfo)
        });

        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(29) // stats
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
        
        operations.Execute(instructionsList);

        return instructionsList;
    }
}