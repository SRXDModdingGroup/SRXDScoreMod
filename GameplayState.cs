using HarmonyLib;
using UnityEngine;

namespace ScoreMod {
    public class GameplayState {
        public static bool Playing { get; private set; }
        public static float? LastOffset { get; private set; }
        public static PlayState PlayState { get; private set; }

        private static bool pickedNewScoreSystem;
        private static bool calculatingMaxScore;
        private static int lastHoldIndex;
        private static int lastBeatIndex;
        private static Note[] notes;

        private static void EndPlay() {
            Playing = false;
            calculatingMaxScore = false;
            ModState.FinishCalculatingMaxScore();
            ModState.LogPlayData(PlayState.TrackInfoRef.asset.title);
            CompleteScreenUI.UpdateUI();
        }
        
        [HarmonyPatch(typeof(Game), nameof(Game.Update)), HarmonyPostfix]
        private static void Game_Update_Postfix() {
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
        
        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.FinaliseScore)), HarmonyPostfix]
        private static void ScoreState_FinaliseScore_Postfix() {
            if (!Playing || calculatingMaxScore)
                return;

            ModState.BeginCalculatingMaxScore();

            Playing = false;
            calculatingMaxScore = true;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.PerfectFullComboLost)), HarmonyPrefix]
        private static bool ScoreState_PerfectFullComboLost_PreFix() {
            if (Playing)
                ModState.PfcLost();

            return true;
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.AddScore)), HarmonyPostfix]
        private static void ScoreState_AddScore_Postfix(PlayState.ScoreState __instance, int amount, int noteIndex) {
            if (!Playing && !calculatingMaxScore || notes == null || noteIndex >= notes.Length || noteIndex < 0)
                return;
            
            var note = notes[noteIndex];
            bool isSustainedNoteTick = false;
            
            if (note.NoteType == NoteType.HoldStart) {
                if (noteIndex == lastHoldIndex)
                    isSustainedNoteTick = true;
                else
                    lastHoldIndex = noteIndex;
            }
            else if (note.NoteType == NoteType.DrumStart) {
                if (noteIndex == lastBeatIndex)
                    isSustainedNoteTick = true;
                else
                    lastBeatIndex = noteIndex;
            }
            
            ModState.AddPoints(amount, LastOffset ?? 0f, isSustainedNoteTick, note);
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.DropMultiplier)), HarmonyPostfix]
        private static void ScoreState_DropMultiplier_Postfix() {
            if (!Playing)
                return;

            ModState.Miss();
            GameplayUI.UpdateMultiplierText();
        }

        [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPostfix]
        private static void Track_FailSong_Postfix() {
            EndPlay();
        }

        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPostfix]
        private static void Track_CompleteSong_Postfix() {
            EndPlay();
        }
        
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
        private static void Track_PlayTrack_Postfix(Track __instance) {
            ModState.Initialize();

            PlayState = __instance.playStateFirst;
            Playing = true;
            calculatingMaxScore = false;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;

            var trackNotes = __instance.playStateFirst.trackData.Notes;

            notes = new Note[trackNotes.Count];

            int j = 0;

            foreach (var note in trackNotes) {
                notes[j] = note;
                j++;
            }
        }

        [HarmonyPatch(typeof(XDPauseMenu), nameof(XDPauseMenu.ExitButtonPressed)), HarmonyPostfix]
        private static void XDPauseMenu_ExitButtonPressed_Postfix() {
            Playing = false;
            calculatingMaxScore = false;
        }
        
        [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracy)), HarmonyPostfix]
        private static void GameplayVariables_GetTimingAccuracy_Postfix(float timeOffset) {
            if (Playing)
                LastOffset = timeOffset;
        }

        [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracyForBeat)), HarmonyPostfix]
        private static void GameplayVariables_GetTimingAccuracyForBeat_Postfix(float timeOffset) {
            if (Playing)
                LastOffset = timeOffset;
        }
    }
}