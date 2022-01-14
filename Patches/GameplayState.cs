using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SRXDScoreMod; 

// Contains patch functions for receiving data from gameplay
internal class GameplayState {
    public static PlayState PlayState { get; private set; }

    private static PlayableNoteData noteData;
        
    // Log play results, save high scores, and update UI after completing or failing a track
    private static void EndPlay(bool success) {
        ModState.LogPlayData(TrackLoadingSystem.Instance.BorrowHandle(PlayState.TrackInfoRef).TrackInfoMetadata.title, success);
        ModState.SavePlayData(HighScoresContainer.GetTrackId(PlayState.trackData));
        CompleteScreenUI.UpdateUI();
    }

    // Initialize mod values when starting a track
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
    private static void Track_PlayTrack_Postfix(Track __instance) {
        ScoreMod.InitializeScoreSystems();
        
        // PlayState = __instance.playStateFirst;
        //
        // if (__instance.IsInEditMode)
        //     return;
        //     
        // noteData = PlayState.trackData.NoteData;
        // ModState.Initialize(
        //     HighScoresContainer.GetTrackId(PlayState.trackData),
        //     noteData.noteCount,
        //     PlayState.freestyleSectionState.Length,
        //     PlayState.spinSectionStates.Length,
        //     PlayState.scratchSectionStates.Length);
    }
        
    // Reset score and multiplier when looping in Practice mode
    [HarmonyPatch(typeof(Track), nameof(Track.PracticeTrack)), HarmonyPostfix]
    private static void Track_PracticeTrack_Postfix(Track __instance) {
        ScoreMod.InitializeScoreSystems();
        
        // PlayState = __instance.playStateFirst;
        // noteData = PlayState.trackData.NoteData;
        // ModState.Initialize(
        //     HighScoresContainer.GetTrackId(PlayState.trackData),
        //     noteData.noteCount,
        //     PlayState.freestyleSectionState.Length,
        //     PlayState.spinSectionStates.Length,
        //     PlayState.scratchSectionStates.Length);
    }
    //     
    // // Check when a track is completed
    // [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPostfix]
    // private static void Track_CompleteSong_Postfix() {
    //     EndPlay(true);
    // }
    //     
    // // Check when a track is failed
    // [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPostfix]
    // private static void Track_FailSong_Postfix() {
    //     EndPlay(false);
    // }
}