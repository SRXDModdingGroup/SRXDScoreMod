﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;

namespace SRXDScoreMod;

// Contains patch functions for receiving data from gameplay
internal static class GameplayState {
    internal static bool Playing { get; private set;  }

    private static bool trackCompleted;
    private static float tapTimingOffset;
    private static float beatTimingOffset;

    #region NoteEvents

    private static void NormalNoteHit(PlayState playState, int noteIndex, Note note, float timeOffset) {
        if (!Playing)
            return;

        switch (note.NoteType) {
            case NoteType.Match:
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitMatch(noteIndex);

                return;
            case NoteType.Tap:
                timeOffset += tapTimingOffset;
                
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitTap(noteIndex, timeOffset);
                
                GameplayUI.PlayCustomTimingFeedback(playState,
                    ScoreMod.CurrentScoreSystemInternal.GetTimingAccuracyForTap(timeOffset));

                return;
            case NoteType.DrumStart:
                timeOffset += beatTimingOffset;
                
                foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
                    scoreSystem.HitBeat(noteIndex, timeOffset);
                
                GameplayUI.PlayCustomTimingFeedback(playState,
                    ScoreMod.CurrentScoreSystemInternal.GetTimingAccuracyForBeat(timeOffset));

                return;
        }
    }

    private static void BeatReleaseHit(PlayState playState, int noteIndex, float timeOffset) {
        if (!Playing)
            return;

        timeOffset += beatTimingOffset;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitBeatRelease(noteIndex, timeOffset);
        
        GameplayUI.PlayCustomTimingFeedback(playState,
            ScoreMod.CurrentScoreSystemInternal.GetTimingAccuracyForBeatRelease(timeOffset));
    }

    private static void HoldHit(PlayState playState, int noteIndex, float timeOffset) {
        if (!Playing)
            return;

        timeOffset += tapTimingOffset;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitTap(noteIndex, timeOffset);
        
        GameplayUI.PlayCustomTimingFeedback(playState,
            ScoreMod.CurrentScoreSystemInternal.GetTimingAccuracyForTap(timeOffset));
    }

    private static void LiftoffHit(PlayState playState, int noteIndex, float timeOffset) {
        if (!Playing)
            return;

        timeOffset += tapTimingOffset;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitLiftoff(noteIndex, timeOffset);
        
        GameplayUI.PlayCustomTimingFeedback(playState,
            ScoreMod.CurrentScoreSystemInternal.GetTimingAccuracyForLiftoff(timeOffset));
    }

    private static void SpinHit(int noteIndex) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.HitSpin(noteIndex);
    }

    private static void Overbeat(float time) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.Overbeat(time);
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

    private static void UpdateBeatHoldTime(int noteIndex, float heldTime) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateBeatHold(noteIndex, heldTime);
    }
    
    private static void UpdateHoldTime(int noteIndex, float heldTime) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateHold(noteIndex, heldTime);
    }
    
    private static void UpdateSpinTime(int noteIndex, float heldTime) {
        if (!Playing)
            return;

        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateSpin(noteIndex, heldTime);
    }
    
    private static void UpdateScratchTime(int noteIndex, float heldTime) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateScratch(noteIndex, heldTime);
    }

    private static int GetPointValueForSustain(int baseValue, int noteIndex) {
        if (!Playing)
            return baseValue;
        
        return ScoreMod.CurrentScoreSystemInternal.GetPointValueForSustain(baseValue, noteIndex);
    }

    #endregion

    #region Patches

    [HarmonyPatch(typeof(PlayState), nameof(PlayState.Complete)), HarmonyPostfix]
    private static void PlayState_Complete_Postfix(PlayState __instance) {
        Playing = false;
        
        if (!trackCompleted)
            return;

        trackCompleted = false;
        
        if (__instance.isInPracticeMode || __instance.SetupParameters.editMode)
            return;
        
        foreach (var scoreSystem in ScoreMod.ScoreSystems)
            scoreSystem.Complete(__instance);
        
        HighScoresContainer.SaveHighScores();
    }

    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
    private static void Track_PlayTrack_Postfix(Track __instance) {
        if (__instance.IsInEditMode)
            return;
        
        Playing = true;
        tapTimingOffset = 0.001f * Plugin.TapTimingOffset.Value;
        beatTimingOffset = 0.001f * Plugin.BeatTimingOffset.Value;

        var playState = __instance.playStateFirst;

        if (playState.isInPracticeMode)
            return;

        int endIndex = playState.trackData.NoteCount;
        
        foreach (var scoreSystem in ScoreMod.ScoreSystems)
            scoreSystem.Init(playState, 0, endIndex);
    }

    [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPrefix]
    private static void Track_CompleteSong_Prefix(Track __instance) => trackCompleted = true;

    [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPrefix]
    private static void Track_FailSong_Prefix(Track __instance) => trackCompleted = true;

    [HarmonyPatch(typeof(TrackEditorGUI), nameof(TrackEditorGUI.SetTimeToCuePoint)), HarmonyPostfix]
    private static void TrackEditorGUI_SetTimeToCuePoint_Postfix() {
        var playState = Track.Instance.playStateFirst;
        
        if (!playState.isInPracticeMode)
            return;

        var trackData = playState.trackData;
        int startIndex = 0;
        float ignoreBefore = playState.invalidUntilTime;
        
        for (int i = 0; i < trackData.NoteCount; i++) {
            if (trackData.GetNote(i).time <= ignoreBefore)
                continue;
            
            startIndex = i;
                
            break;
        }
        
        int endIndex = trackData.NoteCount;
        float ignoreAfter = playState.dontDrawNotesBeyondTime;

        for (int i = startIndex; i < trackData.NoteCount; i++) {
            if (trackData.GetNote(i).time < ignoreAfter)
                continue;

            endIndex = i;

            break;
        }
        
        foreach (var scoreSystem in ScoreMod.ScoreSystems)
            scoreSystem.Init(playState, startIndex, endIndex);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyPostfix]
    private static void TrackGameplayLogic_UpdateNoteState_Postfix(int noteIndex, bool __result) {
        if (!Playing || !__result)
            return;

        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.CompleteNote(noteIndex);
    }

    [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Track_Update_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var GameplayState_Overbeat = typeof(GameplayState).GetMethod(nameof(Overbeat), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var Track_currentAudioTrackTime = typeof(Track).GetField(nameof(Track.currentAudioTrackTime));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).First()[0];
        
        instructionsList.InsertRange(match.End, new CodeInstruction[] {
            new (OpCodes.Ldarg_0), // track
            new (OpCodes.Ldfld, Track_currentAudioTrackTime),
            new (OpCodes.Call, GameplayState_Overbeat)
        });

        return instructionsList;
    }
    
    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateNoteState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerationOperation<CodeInstruction>();
        var GameplayState_NormalNoteHit = typeof(GameplayState).GetMethod(nameof(NormalNoteHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseHit = typeof(GameplayState).GetMethod(nameof(BeatReleaseHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_NormalNoteMiss = typeof(GameplayState).GetMethod(nameof(NormalNoteMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseMiss = typeof(GameplayState).GetMethod(nameof(BeatReleaseMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatHoldMiss = typeof(GameplayState).GetMethod(nameof(BeatHoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateBeatHoldTime = typeof(GameplayState).GetMethod(nameof(UpdateBeatHoldTime), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var TrackGameplayFeedbackObjects_PlayTimingFeedback = typeof(TrackGameplayFeedbackObjects).GetMethod(nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback));
        var Note_endNoteIndex = typeof(Note).GetField(nameof(Note.endNoteIndex));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        });

        foreach (var match0 in matches) {
            var opcode = instructionsList[match0[0].Start + 1].opcode;

            if (opcode == OpCodes.Ldloc_S) { // pointsToAdd
                operations.Insert(match0[1].End, new CodeInstruction[] {
                    new (OpCodes.Ldarg_0), // playState
                    new (OpCodes.Ldarg_2), // noteIndex
                    new (OpCodes.Ldloc_1), // note
                    new (OpCodes.Ldloc_S, (byte) 7), // timeOffset
                    new (OpCodes.Call, GameplayState_NormalNoteHit)
                });
            }
            else if (opcode == OpCodes.Ldloc_3) { // gameplayVariables
                operations.Insert(match0[1].End, new CodeInstruction[] {
                    new (OpCodes.Ldarg_0), // playState
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

        foreach (var match0 in matches) {
            var result = match0[0];

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

        foreach (var match0 in matches) {
            operations.Insert(match0[0].End, new CodeInstruction[] {
                new (OpCodes.Ldarg_2), // noteIndex
                new (OpCodes.Ldloc_1), // note
                new (OpCodes.Call, GameplayState_NormalNoteMiss)
            });
        }

        int match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(44) && instr.labels.Count > 0 // heldTime
        }).First()[0].Start;

        var labels = instructionsList[match1].labels;
        
        operations.Replace(match1, 1, new CodeInstruction[] {
            new CodeInstruction(OpCodes.Ldarg_2).WithLabels(labels), // noteIndex
            new (OpCodes.Ldloc_S, (byte) 44), // heldTime
            new (OpCodes.Call, GameplayState_UpdateBeatHoldTime),
            new (OpCodes.Ldloc_S, (byte) 44) // heldTime
        });
        
        var match2 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0, // playState
            instr => instr.opcode == OpCodes.Ldarg_3, // state
            instr => instr.opcode == OpCodes.Ldfld, // timingAccuracy
            instr => instr.Calls(TrackGameplayFeedbackObjects_PlayTimingFeedback)
        }).First()[0];
        
        instructionsList[match2.End].labels.AddRange(instructionsList[match2.Start].labels);
        operations.Remove(match2.Start, match2.Length);
        
        match2 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0, // playState
            instr => instr.opcode == OpCodes.Ldloc_S, // timingAccuracy
            instr => instr.Calls(TrackGameplayFeedbackObjects_PlayTimingFeedback)
        }).First()[0];
        
        operations.Remove(match2.Start, match2.Length);

        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateFreestyleSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateFreestyleSectionState_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerationOperation<CodeInstruction>();
        var pointValue = generator.DeclareLocal(typeof(int));
        var GameplayState_HoldHit = typeof(GameplayState).GetMethod(nameof(HoldHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffHit = typeof(GameplayState).GetMethod(nameof(LiftoffHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_HoldMiss = typeof(GameplayState).GetMethod(nameof(HoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffMiss = typeof(GameplayState).GetMethod(nameof(LiftoffMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateHoldTime = typeof(GameplayState).GetMethod(nameof(UpdateHoldTime), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_GetPointValueForSustain = typeof(GameplayState).GetMethod(nameof(GetPointValueForSustain), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var TrackGameplayFeedbackObjects_PlayTimingFeedback = typeof(TrackGameplayFeedbackObjects).GetMethod(nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback));
        var ScoreOverTimeEffect_SetCount = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetCount));
        var ScoreOverTimeEffect_SetScore = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetScore));
        var FreestyleSection_firstNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.firstNoteIndex));
        var FreestyleSection_endNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.endNoteIndex));
        var FreestyleSectionState_hasEntered = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.hasEntered));
        var FreestyleSectionState_releaseState = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.releaseState));
        var Worms_tapScore = typeof(GameplayVariables.Worms).GetField(nameof(GameplayVariables.Worms.tapScore));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0, // playState
            instr => instr.IsLdarg(),
            instr => instr.opcode == OpCodes.Ldfld,
            instr => instr.Calls(TrackGameplayFeedbackObjects_PlayTimingFeedback)
        });

        foreach (var match0 in matches) {
            int start = match0[0].Start;
            
            if (instructionsList[start - 1].Calls(ScoreState_AddScoreIfPossible))
                operations.Remove(start, 4);
        }
        
        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        });

        foreach (var match0 in matches) {
            int start = match0[0].Start;
            
            if (instructionsList[start + 1].opcode == OpCodes.Ldloc_S) { // worms
                if (instructionsList[start + 2].LoadsField(Worms_tapScore)) {
                    operations.Insert(match0[1].End, new CodeInstruction[] {
                        new (OpCodes.Ldarg_0), // playState
                        new (OpCodes.Ldloc_S, (byte) 6), // section
                        new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
                        new (OpCodes.Ldloc_S, (byte) 50), // timeOffset
                        new (OpCodes.Call, GameplayState_HoldHit)
                    });
                }
                else {
                    operations.Insert(match0[1].End, new CodeInstruction[] {
                        new (OpCodes.Ldarg_0), // playState
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
        
        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(15) // heldTime
        }).First()[0];
        
        operations.Insert(match1.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 6), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Ldloc_S, (byte) 15), // heldTime
            new (OpCodes.Call, GameplayState_UpdateHoldTime)
        });

        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetCount)
        }).First()[0];
        
        operations.Insert(match1.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 6), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Call, GameplayState_GetPointValueForSustain),
            new (OpCodes.Stloc, pointValue),
            new (OpCodes.Ldloc, pointValue)
        });
        
        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetScore)
        }).First()[0];
        
        operations.Replace(match1.Start - 2, 2, new CodeInstruction[] {
            new (OpCodes.Ldloc, pointValue)
        });

        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateSpinSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateSpinSectionState_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerationOperation<CodeInstruction>();
        var heldTime = generator.DeclareLocal(typeof(float));
        var pointValue = generator.DeclareLocal(typeof(int));
        var callLabel = generator.DefineLabel();
        var GameplayState_SpinHit = typeof(GameplayState).GetMethod(nameof(SpinHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_SpinMiss = typeof(GameplayState).GetMethod(nameof(SpinMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateSpinTime = typeof(GameplayState).GetMethod(nameof(UpdateSpinTime), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_GetPointValueForSustain = typeof(GameplayState).GetMethod(nameof(GetPointValueForSustain), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var ScoreOverTimeEffect_SetCount = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetCount));
        var ScoreOverTimeEffect_SetScore = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetScore));
        var SpinnerSection_noteIndex = typeof(SpinnerSection).GetField(nameof(SpinnerSection.noteIndex));
        var SpinnerSection_startsAtTime = typeof(SpinnerSection).GetField(nameof(SpinnerSection.startsAtTime));
        var SpinSectionState_failedInitialSpin = typeof(SpinSectionState).GetField(nameof(SpinSectionState.failedInitialSpin));
        var SpinSectionState_state = typeof(SpinSectionState).GetField(nameof(SpinSectionState.state));
        
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

        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Is(OpCodes.Ldarg_S, (byte) 4), // state
            instr => instr.LoadsField(SpinSectionState_state),
            instr => instr.opcode == OpCodes.Ldc_I4_4, // SpinSectionState.State.Passed
            instr => instr.Branches(out _),
            instr => instr.LoadsLocalAtIndex(28), // max
            instr => instr.StoresLocalAtIndex(30) // value
        }).First()[0];

        int start = match1.Start;
        var startLabels = new List<Label>(instructionsList[start].labels);

        instructionsList[start].labels.Clear();
        instructionsList[start + 3].operand = callLabel;
        
        operations.Insert(start, new CodeInstruction[] {
            new CodeInstruction(OpCodes.Ldloc_S, (byte) 5).WithLabels(startLabels), // trackTime
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_startsAtTime),
            new (OpCodes.Sub),
            new (OpCodes.Stloc_S, heldTime)
        });
        
        operations.Insert(match1.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, (byte) 27), // length
            new (OpCodes.Stloc_S, heldTime),
            new CodeInstruction(OpCodes.Ldloc_3).WithLabels(callLabel), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Ldloc_S, heldTime),
            new (OpCodes.Call, GameplayState_UpdateSpinTime)
        });
        
        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetCount)
        }).First()[0];
        
        operations.Insert(match1.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Call, GameplayState_GetPointValueForSustain),
            new (OpCodes.Stloc, pointValue),
            new (OpCodes.Ldloc, pointValue)
        });
        
        match1 = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetScore)
        }).First()[0];
        
        operations.Replace(match1.Start - 2, 2, new CodeInstruction[] {
            new (OpCodes.Ldloc, pointValue)
        });
        
        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateScratchSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateScratchSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerationOperation<CodeInstruction>();
        var GameplayState_ScratchMiss = typeof(GameplayState).GetMethod(nameof(ScratchMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateScratchTime = typeof(GameplayState).GetMethod(nameof(UpdateScratchTime), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_GetPointValueForSustain = typeof(GameplayState).GetMethod(nameof(GetPointValueForSustain), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var ScoreOverTimeEffect_SetCount = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetCount));
        var ScoreOverTimeEffect_SetScore = typeof(ScoreOverTimeEffect).GetMethod(nameof(ScoreOverTimeEffect.SetScore));
        var ScratchSection_noteIndex = typeof(ScratchSection).GetField(nameof(ScratchSection.noteIndex));
        var ScratchSection_startsAtTime = typeof(ScratchSection).GetField(nameof(ScratchSection.startsAtTime));
        var ScratchSectionState_totalScore = typeof(ScratchSectionState).GetField(nameof(ScratchSectionState.totalScore));
        var PlayState_currentTrackTime = typeof(PlayState).GetField(nameof(PlayState.currentTrackTime));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).First()[0];
            
        operations.Insert(match.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Call, GameplayState_ScratchMiss)
        });
        
        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresField(ScratchSectionState_totalScore)
        }).First()[0];
        
        operations.Insert(match.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldfld, PlayState_currentTrackTime),
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_startsAtTime),
            new (OpCodes.Sub),
            new (OpCodes.Call, GameplayState_UpdateScratchTime)
        });
        
        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetCount)
        }).First()[0];
        
        operations.Insert(match.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Call, GameplayState_GetPointValueForSustain)
        });
        
        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreOverTimeEffect_SetScore)
        }).First()[0];
        
        operations.Insert(match.Start, new CodeInstruction[] {
            new (OpCodes.Ldloc_2), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Call, GameplayState_GetPointValueForSustain)
        });

        return operations.Enumerate(instructionsList);
    }

    #endregion
}