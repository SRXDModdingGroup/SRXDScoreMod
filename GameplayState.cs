using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SRXDScoreMod {
    // Contains patch functions for receiving data from gameplay
    public class GameplayState {
        public static PlayState PlayState { get; private set; }

        private static bool pickedNewScoreSystem;
        private static PlayableNoteData noteData;
        
        // Log play results, save high scores, and update UI after completing or failing a track
        private static void EndPlay(bool success) {
            ModState.LogPlayData(TrackLoadingSystem.Instance.BorrowHandle(PlayState.TrackInfoRef).TrackInfoMetadata.title, success);
            ModState.SavePlayData(LevelSelectUI.GetTrackId(PlayState.trackData));
            CompleteScreenUI.UpdateUI();
        }
        
        // Used to handle inputs for toggling mod score and selecting different scoring profiles
        [HarmonyPatch(typeof(Game), nameof(Game.Update)), HarmonyPostfix]
        private static void Game_Update_Postfix() {
            if (PlayState != null && PlayState.isInPracticeMode)
                return;

            if (Input.GetKeyDown(KeyCode.P))
                pickedNewScoreSystem = false;

            if (Input.GetKey(KeyCode.P)) {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    pickedNewScoreSystem = ModState.PickScoringSystem(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    pickedNewScoreSystem = ModState.PickScoringSystem(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    pickedNewScoreSystem = ModState.PickScoringSystem(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    pickedNewScoreSystem = ModState.PickScoringSystem(3);
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                    pickedNewScoreSystem = ModState.PickScoringSystem(4);
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                    pickedNewScoreSystem = ModState.PickScoringSystem(5);
                else if (Input.GetKeyDown(KeyCode.Alpha7))
                    pickedNewScoreSystem = ModState.PickScoringSystem(6);
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                    pickedNewScoreSystem = ModState.PickScoringSystem(7);
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                    pickedNewScoreSystem = ModState.PickScoringSystem(8);
            }

            if (Input.GetKeyUp(KeyCode.P) && !pickedNewScoreSystem)
                ModState.ToggleModdedScoring();
        }

        // Initialize mod values when starting a track
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
        private static void Track_PlayTrack_Postfix(Track __instance) {
            PlayState = __instance.playStateFirst;

            if (__instance.IsInEditMode)
                return;
            
            noteData = PlayState.trackData.NoteData;
            ModState.Initialize(
                LevelSelectUI.GetTrackId(PlayState.trackData),
                noteData.noteCount,
                PlayState.freestyleSectionState.Length,
                PlayState.spinSectionStates.Length,
                PlayState.scratchSectionStates.Length);
        }
        
        // Reset score and multiplier when looping in Practice mode
        [HarmonyPatch(typeof(Track), nameof(Track.PracticeTrack)), HarmonyPostfix]
        private static void Track_PracticeTrack_Postfix(Track __instance) {
            PlayState = __instance.playStateFirst;
            noteData = PlayState.trackData.NoteData;
            ModState.Initialize(
                LevelSelectUI.GetTrackId(PlayState.trackData),
                noteData.noteCount,
                PlayState.freestyleSectionState.Length,
                PlayState.spinSectionStates.Length,
                PlayState.scratchSectionStates.Length);
        }
        
        // Check when a track is completed
        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPostfix]
        private static void Track_CompleteSong_Postfix() {
            EndPlay(true);
        }
        
        // Check when a track is failed
        [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPostfix]
        private static void Track_FailSong_Postfix() {
            EndPlay(false);
        }
    }
}