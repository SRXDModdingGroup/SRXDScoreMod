using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;
using UnityEngine;
using Input = UnityEngine.Input;

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
    
    private static void UpdateSpinTime(int noteIndex, float heldTime, float holdLength, SpinSectionState.State state) {
        if (!Playing)
            return;

        if (state == SpinSectionState.State.Passed)
            heldTime = holdLength;

        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateSpin(noteIndex, heldTime);
    }
    
    private static void UpdateScratchTime(int noteIndex, float heldTime) {
        if (!Playing)
            return;
        
        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.UpdateScratch(noteIndex, heldTime);
    }

    #endregion

    #region Patches

    [HarmonyPatch(typeof(Game), nameof(Game.Update)), HarmonyPostfix]
    private static void Game_Update_Postfix() {
        if (Input.GetKeyDown(KeyCode.F1))
            Plugin.CurrentSystem.Value = (Plugin.CurrentSystem.Value + 1) % ScoreMod.ScoreSystems.Count;
    }

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

    [HarmonyPatch(typeof(PlayableTrackData), nameof(PlayableTrackData.EndEditing)), HarmonyPostfix]
    private static void PlayableTrackData_EndEditing_Postfix(PlayableTrackData __instance) {
        var playState = Track.Instance.playStateFirst;
        
        if (__instance != playState.trackData)
            return;
        
        int endIndex = __instance.NoteCount;
        
        foreach (var scoreSystem in ScoreMod.ScoreSystems)
            scoreSystem.Init(playState, 0, endIndex);
    }

    [HarmonyPatch(typeof(PlayState), nameof(PlayState.ClearNoteStates)), HarmonyPostfix]
    private static void PlayState_ClearNoteStates_Postfix(PlayState __instance) {
        foreach (var scoreSystem in ScoreMod.ScoreSystems)
            scoreSystem.ResetScore();
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyPostfix]
    private static void TrackGameplayLogic_UpdateNoteState_Postfix(int noteIndex, bool __result) {
        if (!Playing || !__result)
            return;

        foreach (var scoreSystem in ScoreMod.CustomScoreSystems)
            scoreSystem.CompleteNote(noteIndex);
    }

    [HarmonyPatch(typeof(PlayState), nameof(PlayState.UpdateGameplay)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PlayState_UpdateGameplay_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var GameplayState_Overbeat = typeof(GameplayState).GetMethod(nameof(Overbeat), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var PlayState_currentTrackTime = typeof(PlayState).GetField(nameof(PlayState.currentTrackTime));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).First()[0];
        
        instructionsList.InsertRange(match.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_1), // playState
            new (OpCodes.Ldfld, PlayState_currentTrackTime),
            new (OpCodes.Call, GameplayState_Overbeat)
        });

        return instructionsList;
    }
    
    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateNoteState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var GameplayState_NormalNoteHit = typeof(GameplayState).GetMethod(nameof(NormalNoteHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseHit = typeof(GameplayState).GetMethod(nameof(BeatReleaseHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_NormalNoteMiss = typeof(GameplayState).GetMethod(nameof(NormalNoteMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatReleaseMiss = typeof(GameplayState).GetMethod(nameof(BeatReleaseMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_BeatHoldMiss = typeof(GameplayState).GetMethod(nameof(BeatHoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateBeatHoldTime = typeof(GameplayState).GetMethod(nameof(UpdateBeatHoldTime), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var PlayState_PlayTimingFeedback = typeof(PlayStateExtensions).GetMethod(nameof(PlayStateExtensions.PlayTimingFeedback));
        var Note_endNoteIndex = typeof(Note).GetField(nameof(Note.endNoteIndex));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldarg_2), // noteIndex
            new (OpCodes.Ldloc_1), // note
            new (OpCodes.Ldloc_S, 7), // timeOffset
            new (OpCodes.Call, GameplayState_NormalNoteHit)
        });
        
        operations.Insert(matches[1][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldloc_1), // note
            new (OpCodes.Ldfld, Note_endNoteIndex),
            new (OpCodes.Ldloc_S, 42), // beatTimeOffset
            new (OpCodes.Call, GameplayState_BeatReleaseHit)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_2), // noteIndex
            new (OpCodes.Ldloc_1), // note
            new (OpCodes.Ldfld, Note_endNoteIndex),
            new (OpCodes.Call, GameplayState_BeatHoldMiss)
        });
        
        operations.Insert(matches[1][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_1), // note
            new (OpCodes.Ldfld, Note_endNoteIndex),
            new (OpCodes.Call, GameplayState_BeatReleaseMiss)
        });
        
        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        }).ToList();

        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_2), // noteIndex
            new (OpCodes.Ldloc_1), // note
            new (OpCodes.Call, GameplayState_NormalNoteMiss)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(40) // heldTime
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Branches(out _)
        }).ToList();
        
        operations.Insert(matches[0][2].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_2), // noteIndex
            new (OpCodes.Ldloc_S, 40), // heldTime
            new (OpCodes.Call, GameplayState_UpdateBeatHoldTime),
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0 // playState
        }).Then(new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(PlayState_PlayTimingFeedback)
        }).ToList();

        foreach (var match in matches) {
            instructionsList[match[1].End].labels.AddRange(instructionsList[match[0].Start].labels);
            operations.Remove(match[0].Start, match[1].End - match[0].Start);
        }

        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateFreestyleSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateFreestyleSectionState_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var GameplayState_HoldHit = typeof(GameplayState).GetMethod(nameof(HoldHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffHit = typeof(GameplayState).GetMethod(nameof(LiftoffHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_HoldMiss = typeof(GameplayState).GetMethod(nameof(HoldMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_LiftoffMiss = typeof(GameplayState).GetMethod(nameof(LiftoffMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateHoldTime = typeof(GameplayState).GetMethod(nameof(UpdateHoldTime), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var PlayState_PlayTimingFeedback = typeof(PlayStateExtensions).GetMethod(nameof(PlayStateExtensions.PlayTimingFeedback));
        var FreestyleSection_firstNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.firstNoteIndex));
        var FreestyleSection_endNoteIndex = typeof(FreestyleSection).GetField(nameof(FreestyleSection.endNoteIndex));
        var FreestyleSectionState_hasEntered = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.hasEntered));
        var FreestyleSectionState_releaseState = typeof(FreestyleSectionState).GetField(nameof(FreestyleSectionState.releaseState));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0, // playState
            instr => instr.IsLdarg(),
            instr => instr.opcode == OpCodes.Ldfld,
            instr => instr.Calls(PlayState_PlayTimingFeedback)
        }).ToList();

        operations.Remove(matches[0][0].Start, 4);
        operations.Remove(matches[1][0].Start, 4);

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Ldloc_S, 48), // timeOffset
            new (OpCodes.Call, GameplayState_HoldHit)
        });
        
        operations.Insert(matches[1][0].End, new CodeInstruction[] {
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
            new (OpCodes.Ldloc_S, 51), // timeOffset2
            new (OpCodes.Call, GameplayState_LiftoffHit)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
            new (OpCodes.Ldarg_S, 4), // state
            new (OpCodes.Ldfld, FreestyleSectionState_hasEntered),
            new (OpCodes.Call, GameplayState_HoldMiss)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.IsLdarg(4), // state
            instr => instr.opcode == OpCodes.Ldc_I4_5, // ReleaseState.DidntLetGo
            instr => instr.StoresField(FreestyleSectionState_releaseState)
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_endNoteIndex),
            new (OpCodes.Call, GameplayState_LiftoffMiss)
        });
        
        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(55) // pointsForHold
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, 4), // section
            new (OpCodes.Ldfld, FreestyleSection_firstNoteIndex),
            new (OpCodes.Ldloc_S, 13), // heldTime
            new (OpCodes.Call, GameplayState_UpdateHoldTime)
        });

        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateSpinSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateSpinSectionState_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var GameplayState_SpinHit = typeof(GameplayState).GetMethod(nameof(SpinHit), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_SpinMiss = typeof(GameplayState).GetMethod(nameof(SpinMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateSpinTime = typeof(GameplayState).GetMethod(nameof(UpdateSpinTime), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_AddScoreIfPossible = typeof(TrackGameplayLogic).GetMethod("AddScoreIfPossible", BindingFlags.NonPublic | BindingFlags.Static);
        var TrackGameplayLogic_AllowErrorToOccur = typeof(TrackGameplayLogic).GetMethod(nameof(TrackGameplayLogic.AllowErrorToOccur));
        var SpinnerSection_noteIndex = typeof(SpinnerSection).GetField(nameof(SpinnerSection.noteIndex));
        var SpinnerSection_startsAtTime = typeof(SpinnerSection).GetField(nameof(SpinnerSection.startsAtTime));
        var SpinSectionState_failedInitialSpin = typeof(SpinSectionState).GetField(nameof(SpinSectionState.failedInitialSpin));
        var SpinSectionState_state = typeof(SpinSectionState).GetField(nameof(SpinSectionState.state));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_AddScoreIfPossible)
        }).ToList();

        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Call, GameplayState_SpinHit)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(TrackGameplayLogic_AllowErrorToOccur),
            instr => instr.opcode == OpCodes.Pop
        }).ToList();

        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Ldarg_S, 4), // state
            new (OpCodes.Ldfld, SpinSectionState_failedInitialSpin),
            new (OpCodes.Call, GameplayState_SpinMiss)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(26) // spinLength
        }).ToList();
        
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_noteIndex),
            new (OpCodes.Ldloc_S, 5), // trackTime
            new (OpCodes.Ldloc_3), // section
            new (OpCodes.Ldfld, SpinnerSection_startsAtTime),
            new (OpCodes.Sub),
            new (OpCodes.Ldloc_S, 26), // spinLength
            new (OpCodes.Ldarg_S, 4), // state
            new (OpCodes.Ldfld, SpinSectionState_state),
            new (OpCodes.Call, GameplayState_UpdateSpinTime)
        });
        
        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateScratchSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateScratchSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var GameplayState_ScratchMiss = typeof(GameplayState).GetMethod(nameof(ScratchMiss), BindingFlags.NonPublic | BindingFlags.Static);
        var GameplayState_UpdateScratchTime = typeof(GameplayState).GetMethod(nameof(UpdateScratchTime), BindingFlags.NonPublic | BindingFlags.Static);
        var ScoreState_DropMultiplier = typeof(PlayState.ScoreState).GetMethod(nameof(PlayState.ScoreState.DropMultiplier));
        var ScratchSection_noteIndex = typeof(ScratchSection).GetField(nameof(ScratchSection.noteIndex));
        var ScratchSection_startsAtTime = typeof(ScratchSection).GetField(nameof(ScratchSection.startsAtTime));
        var ScratchSectionState_totalScore = typeof(ScratchSectionState).GetField(nameof(ScratchSectionState.totalScore));
        var PlayState_currentTrackTime = typeof(PlayState).GetField(nameof(PlayState.currentTrackTime));

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(ScoreState_DropMultiplier)
        }).ToList();
            
        operations.Insert(matches[0][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_1), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Call, GameplayState_ScratchMiss)
        });

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresField(ScratchSectionState_totalScore)
        }).ToList();
        
        operations.Insert(matches[1][0].End, new CodeInstruction[] {
            new (OpCodes.Ldloc_1), // section
            new (OpCodes.Ldfld, ScratchSection_noteIndex),
            new (OpCodes.Ldarg_0), // playState
            new (OpCodes.Ldfld, PlayState_currentTrackTime),
            new (OpCodes.Ldloc_1), // section
            new (OpCodes.Ldfld, ScratchSection_startsAtTime),
            new (OpCodes.Sub),
            new (OpCodes.Call, GameplayState_UpdateScratchTime)
        });

        return operations.Enumerate(instructionsList);
    }

    #endregion
}