﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Utilities;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains patch functions for receiving data from gameplay
internal class GameplayState {
    public static bool Playing { get; private set; }

    private static PlayableTrackData trackData;

    private static void NormalNoteHit(int noteIndex, Note note, float timeOffset) {
        if (!Playing)
            return;

        switch (note.NoteType) {
            case NoteType.Match:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitMatch(noteIndex);

                return;
            case NoteType.Tap:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitTap(noteIndex, timeOffset);

                return;
            case NoteType.DrumStart:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitBeat(noteIndex, timeOffset);

                return;
        }
    }

    private static void BeatReleaseHit(int noteIndex, float timeOffset) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitBeatRelease(noteIndex, timeOffset);
    }

    private static void HoldHit(int noteIndex, float timeOffset) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitTap(noteIndex, timeOffset);
    }

    private static void LiftoffHit(int noteIndex, float timeOffset) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitLiftoff(noteIndex, timeOffset);
    }

    private static void SpinHit(int noteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitSpin(noteIndex);
    }

    private static void Overbeat() {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.Overbeat();
    }

    private static void NormalNoteMiss(int noteIndex, Note note) {
        if (!Playing)
            return;
        
        switch (note.NoteType) {
            case NoteType.Match:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.MissMatch(noteIndex);

                return;
            case NoteType.Tap:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.MissTap(noteIndex);

                return;
            case NoteType.DrumStart:
                if (note.length > 0f) {
                    int endNoteIndex = note.endNoteIndex;
                    
                    foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                        scoreSystem.MissBeatHold(noteIndex, endNoteIndex);

                    return;
                }
                
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.MissBeat(noteIndex);

                return;
        }
    }

    private static void BeatReleaseMiss(int noteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.MissBeatRelease(noteIndex);
    }
    
    private static void BeatHoldMiss(int noteIndex, int endNoteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.BreakBeatHold(noteIndex, endNoteIndex);
    }

    private static void HoldMiss(int noteIndex, int endNoteIndex, bool hasEntered) {
        if (!Playing)
            return;

        if (hasEntered) {
            foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                scoreSystem.BreakHold(noteIndex, endNoteIndex);
        }
        else {
            foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                scoreSystem.MissHold(noteIndex, endNoteIndex);
        }
    }

    private static void LiftoffMiss(int noteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.MissLiftoff(noteIndex);
    }

    private static void SpinMiss(int noteIndex, bool failedInitialSpin) {
        if (!Playing)
            return;

        if (failedInitialSpin) {
            foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                scoreSystem.MissSpin(noteIndex);
        }
        else {
            foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                scoreSystem.BreakSpin(noteIndex);
        }
    }

    private static void ScratchMiss(int noteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.BreakScratch(noteIndex);
    }
    
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
    private static void Track_PlayTrack_Postfix(Track __instance) {
        ScoreMod.InitializeScoreSystems(__instance.playStateFirst);
        trackData = __instance.playStateFirst.trackData;
        Playing = true;
    }
        
    [HarmonyPatch(typeof(Track), nameof(Track.PracticeTrack)), HarmonyPostfix]
    private static void Track_PracticeTrack_Postfix(Track __instance) {
        ScoreMod.InitializeScoreSystems(__instance.playStateFirst);
        trackData = __instance.playStateFirst.trackData;
        Playing = true;
    }

    [HarmonyPatch(typeof(Track), nameof(Track.ReturnToPickTrack)), HarmonyPostfix]
    private static void Track_ReturnToPickTrack_Postfix() {
        Playing = false;
    }

    [HarmonyPatch(typeof(PlayState), nameof(PlayState.Complete))]
    private static void PlayState_Complete_Postfix() {
        Playing = false;
    }

    [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Track_Update_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var GameplayState_Overbeat = typeof(GameplayState).GetMethod(nameof(Overbeat), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).First()[0];
        
        instructionsList.Insert(match.End, new CodeInstruction(OpCodes.Call, GameplayState_Overbeat));

        return instructionsList;
    }
    
    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateNoteState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var GameplayState_NormalNoteHit = typeof(GameplayState).GetMethod(nameof(NormalNoteHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseHit = typeof(GameplayState).GetMethod(nameof(BeatReleaseHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_NormalNoteMiss = typeof(GameplayState).GetMethod(nameof(NormalNoteMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseMiss = typeof(GameplayState).GetMethod(nameof(BeatReleaseMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatHoldMiss = typeof(GameplayState).GetMethod(nameof(BeatHoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var Note_endNoteIndex = typeof(Note).GetField(nameof(Note.endNoteIndex));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        });

        foreach (var match in matches) {
            var opcode = instructionsList[match[0].Start + 1].opcode;

            if (opcode == OpCodes.Ldloc_S) { // pointsToAdd
                operations.Insert(match[1].End, new CodeInstruction[] {
                    new (OpCodes.Ldarg_2), // noteIndex
                    new (OpCodes.Ldloc_1), // note
                    new (OpCodes.Ldloc_S, (byte) 7), // timeOffset
                    new (OpCodes.Call, GameplayState_NormalNoteHit)
                });
            }
            else if (opcode == OpCodes.Ldloc_3) { // gameplayVariables
                operations.Insert(match[1].End, new CodeInstruction[] {
                    new (OpCodes.Ldloc_1), // note
                    new (OpCodes.Ldfld, Note_endNoteIndex),
                    new (OpCodes.Ldloc_S, (byte) 46), // beatTimeOffset
                    new (OpCodes.Call, GameplayState_BeatReleaseHit)
                });
            }
        }

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        });

        foreach (var match in matches) {
            var result = match[0];

            if (instructionsList[result.Start - 4].Branches(out _)) {
                operations.Insert(result.End, new CodeInstruction[] {
                    new (OpCodes.Ldloc_1), // note
                    new (OpCodes.Ldfld, Note_endNoteIndex),
                    new (OpCodes.Call, GameplayState_BeatReleaseMiss)
                });
            }
            else {
                operations.Insert(result.End, new CodeInstruction[] {
                    new (OpCodes.Ldarg_2), // noteIndex
                    new (OpCodes.Ldloc_1), // note
                    new (OpCodes.Ldfld, Note_endNoteIndex),
                    new (OpCodes.Call, GameplayState_BeatHoldMiss)
                });
            }
        }
        
        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        });

        foreach (var match in matches) {
            operations.Insert(match[0].End, new CodeInstruction[] {
                new (OpCodes.Ldarg_2), // noteIndex
                new (OpCodes.Ldloc_1), // note
                new (OpCodes.Call, GameplayState_NormalNoteMiss)
            });
        }
        
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateFreestyleSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateFreestyleSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var GameplayState_HoldHit = typeof(GameplayState).GetMethod(nameof(HoldHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffHit = typeof(GameplayState).GetMethod(nameof(LiftoffHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_HoldMiss = typeof(GameplayState).GetMethod(nameof(HoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffMiss = typeof(GameplayState).GetMethod(nameof(LiftoffMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var FreestyleSection_firstNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.firstNoteIndex));
        var FreestyleSection_endNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.endNoteIndex));
        var FreestyleSectionState_hasEntered = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.hasEntered));
        var FreestyleSectionState_releaseState = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.releaseState));
        var Worms_tapScore = typeof(GameplayVariables.Worms).GetField(nameof(GameplayVariables.Worms.tapScore));
        
        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        });

        foreach (var match0 in matches) {
            int start = match0[0].Start;
            
            if (instructionsList[start + 1].opcode == OpCodes.Ldloc_S) { // worms
                if (instructionsList[start + 2].LoadsField(Worms_tapScore)) {
                    operations.Insert(match0[1].End, new CodeInstruction[] {
                        new (OpCodes.Ldloc_S, (byte) 6), // section
                        new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
                        new (OpCodes.Ldloc_S, (byte) 50), // timeOffset
                        new (OpCodes.Call, GameplayState_HoldHit)
                    });
                }
                else {
                    operations.Insert(match0[1].End, new CodeInstruction[] {
                        new (OpCodes.Ldloc_S, (byte) 6), // section
                        new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
                        new (OpCodes.Ldloc_S, (byte) 53), // timeOffset2
                        new (OpCodes.Call, GameplayState_LiftoffHit)
                    });
                }
            }
        }
        
        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.labels.Count > 0
        }).First()[1];
        
        operations.Insert(match1.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 6), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Ldloc_S, (byte) 6), // section
            new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
            new (OpCodes.Ldarg_S, (byte) 4), // state
            new (OpCodes.Ldfld, FreestyleSectionState_hasEntered),
            new (OpCodes.Call, GameplayState_HoldMiss)
        });

        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.IsLdarg(), // state
            instr => instr.opcode == OpCodes.Ldc_I4_5, // ReleaseState.DidntLetGo
            instr => instr.StoresField(FreestyleSectionState_releaseState)
        }).First()[0];
        
        operations.Insert(match1.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 6), // section
            new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
            new (OpCodes.Call, GameplayState_LiftoffMiss)
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateSpinSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateSpinSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var GameplayState_SpinHit = typeof(GameplayState).GetMethod(nameof(SpinHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_SpinMiss = typeof(GameplayState).GetMethod(nameof(SpinMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var SpinnerSection_noteIndex = typeof(ScratchSection).GetField(nameof(SpinnerSection.noteIndex));
        var SpinSectionState_failedInitialSpin = typeof(SpinSectionState).GetField(nameof(SpinSectionState.failedInitialSpin));
        
        var match0 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0,
            instr => instr.opcode == OpCodes.Ldloc_2 // spins
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        }).First()[1];

        operations.Insert(match0.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Call, GameplayState_SpinHit)
        });
        
        var match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        }).First()[0];

        operations.Insert(match1.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Ldarg_S, (byte) 4), // state
            new (OpCodes.Ldfld, SpinSectionState_failedInitialSpin),
            new (OpCodes.Call, GameplayState_SpinMiss)
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateScratchSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateScratchSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var GameplayState_ScratchMiss = typeof(GameplayState).GetMethod(nameof(ScratchMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var ScratchSection_noteIndex = typeof(ScratchSection).GetField(nameof(ScratchSection.noteIndex));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).First()[0];
            
        operations.Insert(match.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Call, GameplayState_ScratchMiss)
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }
}