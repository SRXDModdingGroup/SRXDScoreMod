using HarmonyLib;
using UnityEngine;

namespace ScoreMod {
    public class GameplayState {
        public static bool Playing { get; private set; }
        public static float? LastOffset { get; private set; }
        public static PlayState PlayState { get; private set; }

        private static bool pickedNewScoreSystem;
        private static int lastHoldIndex;
        private static int lastBeatIndex;
        private static int lastSpinIndex;
        private static int lastScratchIndex;
        private static PlayableNoteData noteData;

        private static void EndPlay() {
            Playing = false;
            ModState.LogPlayData(PlayState.TrackInfoRef.asset.title);
            ModState.SavePlayData(LevelSelectUI.GetTrackId(PlayState.trackData));
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
        
        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.AddScore)), HarmonyPostfix]
        private static void ScoreState_AddScore_Postfix(PlayState.ScoreState __instance, int amount, int noteIndex) {
            if (!Playing)
                return;

            var note = noteData.GetNote(noteIndex);
            var noteType = note.NoteType;
            bool isSustainedNoteTick = false;

            switch (noteType) {
                case NoteType.HoldStart:
                    if (noteIndex == lastHoldIndex)
                        isSustainedNoteTick = true;
                    else
                        lastHoldIndex = noteIndex;

                    break;
                case NoteType.DrumStart:
                    if (noteIndex == lastBeatIndex)
                        isSustainedNoteTick = true;
                    else
                        lastBeatIndex = noteIndex;

                    break;
                case NoteType.SpinLeftStart:
                case NoteType.SpinRightStart:
                    if (noteIndex == lastSpinIndex)
                        isSustainedNoteTick = true;
                    else
                        lastSpinIndex = noteIndex;

                    break;
                case NoteType.ScratchStart:
                    if (noteIndex == lastScratchIndex)
                        isSustainedNoteTick = true;
                    else
                        lastScratchIndex = noteIndex;

                    break;
            }

            if (__instance.isMaxPossibleCalculation)
                ModState.AddMaxScore(amount, isSustainedNoteTick, noteType, note.time);
            else
                ModState.AddScore(amount, LastOffset ?? 0f, isSustainedNoteTick, noteType);
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.DropMultiplier)), HarmonyPostfix]
        private static void ScoreState_DropMultiplier_Postfix() {
            if (!Playing)
                return;

            ModState.Miss();
            GameplayUI.UpdateMultiplierText();
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.PerfectFullComboLost)), HarmonyPrefix]
        private static bool ScoreState_PerfectFullComboLost_PreFix() {
            if (Playing)
                ModState.PfcLost();

            return true;
        }

        [HarmonyPatch(typeof(PlayableTrackData), nameof(PlayableTrackData.GetMaxPossibleScoreState), typeof(IntRange)), HarmonyPrefix]
        private static bool PlayableTrackData_GetMaxPossibleScoreState_Prefix() {
            if (!Playing)
                return true;

            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;

            return true;
        }

        [HarmonyPatch(typeof(PlayableTrackData), nameof(PlayableTrackData.GetMaxPossibleScoreState), typeof(IntRange)), HarmonyPostfix]
        private static void PlayableTrackData_GetMaxPossibleScoreState_Postfix() {
            ModState.AddRemainingMaxScoreNoteTicks();
        }

        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
        private static void Track_PlayTrack_Postfix(Track __instance) {
            Playing = false;
            
            if (__instance.IsInEditMode)
                return;
            
            PlayState = __instance.playStateFirst;
            ModState.Initialize(LevelSelectUI.GetTrackId(PlayState.trackData));
            
            if (PlayState.isInPracticeMode)
                return;

            noteData = PlayState.trackData.NoteData;
            Playing = true;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;
        }

        [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPostfix]
        private static void Track_CompleteSong_Postfix() {
            EndPlay();
        }
        
        [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPostfix]
        private static void Track_FailSong_Postfix() {
            EndPlay();
        }

        [HarmonyPatch(typeof(XDPauseMenu), nameof(XDPauseMenu.ExitButtonPressed)), HarmonyPostfix]
        private static void XDPauseMenu_ExitButtonPressed_Postfix() {
            Playing = false;
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