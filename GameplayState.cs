using System.Collections.Generic;
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
        private static int lastSpinIndex;
        private static int lastScratchIndex;
        private static PlayableNoteData noteData;
        private static Dictionary<int, int> holdStates;
        private static Dictionary<int, int> spinStates;
        private static Dictionary<int, int> scratchStates;
        private static List<KeyValuePair<int, int>> activeHoldStates;
        private static List<KeyValuePair<int, int>> activeSpinStates;
        private static List<KeyValuePair<int, int>> activeScratchStates;

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
            if ((!Playing || __instance.isMaxPossibleCalculation) && !calculatingMaxScore)
                return;

            var note = noteData.GetNote(noteIndex);
            var noteType = note.NoteType;
            bool isSustainedNoteTick = false;

            switch (noteType) {
                case NoteType.HoldStart:
                    if (amount == 1 || noteIndex == lastHoldIndex)
                        isSustainedNoteTick = true;
                    else {
                        lastHoldIndex = noteIndex;

                        if (calculatingMaxScore)
                            holdStates.Add(noteIndex, holdStates.Count);
                        else
                            activeHoldStates.Add(new KeyValuePair<int, int>(noteIndex, holdStates[noteIndex]));
                    }

                    break;
                case NoteType.DrumStart:
                    if (amount == 1 || noteIndex == lastBeatIndex)
                        isSustainedNoteTick = true;
                    else
                        lastBeatIndex = noteIndex;

                    break;
                case NoteType.SpinStart:
                case NoteType.SpinLeftStart:
                case NoteType.SpinRightStart:
                    if (amount == 1 || noteIndex == lastSpinIndex)
                        isSustainedNoteTick = true;
                    else {
                        lastSpinIndex = noteIndex;
                        
                        if (calculatingMaxScore)
                            spinStates.Add(noteIndex, spinStates.Count);
                        else
                            activeSpinStates.Add(new KeyValuePair<int, int>(noteIndex, spinStates[noteIndex]));
                    }

                    break;
                case NoteType.ScratchStart:
                    if (noteIndex == lastScratchIndex)
                        isSustainedNoteTick = true;
                    else {
                        lastScratchIndex = noteIndex;
                        
                        if (calculatingMaxScore)
                            scratchStates.Add(noteIndex, scratchStates.Count);
                        else
                            activeScratchStates.Add(new KeyValuePair<int, int>(noteIndex, scratchStates[noteIndex]));
                    }

                    break;
                case NoteType.SectionContinuationOrEnd when calculatingMaxScore:
                    ModState.AddReleaseNotePairing(lastHoldIndex, noteIndex);

                    break;
                case NoteType.DrumEnd when calculatingMaxScore:
                    ModState.AddReleaseNotePairing(lastBeatIndex, noteIndex);

                    break;
            }

            if (calculatingMaxScore)
                ModState.AddMaxScore(amount, isSustainedNoteTick, noteType, noteIndex, note.time);
            else
                ModState.AddScore(amount, LastOffset ?? 0f, isSustainedNoteTick, noteType, noteIndex);
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.DropMultiplier)), HarmonyPostfix]
        private static void ScoreState_DropMultiplier_Postfix(int noteIndex) {
            if (!Playing)
                return;
            
            var note = noteData.GetNote(noteIndex);
            var noteType = note.NoteType;

            switch (noteType) {
                case NoteType.DrumStart:
                    ModState.MissRemainingNoteTicks(noteType, noteIndex);
                    ModState.MissReleaseNoteFromStart(noteType, noteIndex);

                    break;
                case NoteType.HoldStart:
                    if (noteIndex != lastHoldIndex)
                        activeHoldStates.Add(new KeyValuePair<int, int>(noteIndex, holdStates[noteIndex]));
                    
                    break;
                case NoteType.SpinLeftStart:
                case NoteType.SpinRightStart:
                    if (noteIndex != lastSpinIndex)
                        activeSpinStates.Add(new KeyValuePair<int, int>(noteIndex, spinStates[noteIndex]));

                    break;
                case NoteType.ScratchStart:
                    if (noteIndex != lastScratchIndex)
                        activeScratchStates.Add(new KeyValuePair<int, int>(noteIndex, scratchStates[noteIndex]));

                    break;
            }
            
            ModState.Miss(noteType, noteIndex, true, false);
            ModState.ResetMultiplier();
            GameplayUI.UpdateMultiplierText();
        }

        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.PerfectFullComboLost)), HarmonyPrefix]
        private static bool ScoreState_PerfectFullComboLost_PreFix() {
            if (Playing)
                ModState.PfcLost();

            return true;
        }

        [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyPostfix]
        private static void Track_Update_Postfix() {
            if (!Playing)
                return;
            
            for (int i = 0; i < activeHoldStates.Count; i++) {
                var pair = activeHoldStates[i];
                int index = pair.Key;
                var state = PlayState.freestyleSectionState[pair.Value];
                var releaseState = state.releaseState;
                
                if (state.failed)
                    ModState.MissRemainingNoteTicks(NoteType.HoldStart, index);

                if (releaseState == FreestyleSectionState.ReleaseState.Failed
                    || releaseState == FreestyleSectionState.ReleaseState.DidntLetGo
                    || releaseState == FreestyleSectionState.ReleaseState.LetGoTooEarly)
                    ModState.MissReleaseNoteFromStart(NoteType.HoldStart, index);

                if (!state.IsDoneWith || releaseState == FreestyleSectionState.ReleaseState.Waiting)
                    continue;

                activeHoldStates.RemoveAt(i);
                i--;
            }

            for (int i = 0; i < activeSpinStates.Count; i++) {
                var pair = activeSpinStates[i];
                int index = pair.Key;
                var state = PlayState.spinSectionStates[pair.Value];
                
                if (state.failed) {
                    ModState.Miss(NoteType.SpinStart, index, false, false);
                    ModState.MissRemainingNoteTicks(NoteType.SpinStart, index);
                }
                
                if (!state.IsDoneWith)
                    continue;
                
                activeSpinStates.RemoveAt(i);
                i--;
            }
            
            for (int i = 0; i < activeScratchStates.Count; i++) {
                var pair = activeScratchStates[i];
                int index = pair.Key;
                var state = PlayState.scratchSectionStates[pair.Value];
                
                if (state.failed) {
                    ModState.Miss(NoteType.ScratchStart, index, false, false);
                    ModState.MissRemainingNoteTicks(NoteType.ScratchStart, index);
                }
                
                if (!state.failed && !state.hasExited)
                    continue;
                
                activeScratchStates.RemoveAt(i);
                i--;
            }
        }

        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
        private static void Track_PlayTrack_Postfix(Track __instance) {
            Playing = false;
            PlayState = __instance.playStateFirst;
            noteData = PlayState.trackData.NoteData;
            ModState.Initialize(LevelSelectUI.GetTrackId(PlayState.trackData));

            if (holdStates == null)
                holdStates = new Dictionary<int, int>();
            else
                holdStates.Clear();
            
            if (spinStates == null)
                spinStates = new Dictionary<int, int>();
            else
                spinStates.Clear();
            
            if (scratchStates == null)
                scratchStates = new Dictionary<int, int>();
            else
                scratchStates.Clear();

            if (activeHoldStates == null)
                activeHoldStates = new List<KeyValuePair<int, int>>();
            else
                activeHoldStates.Clear();

            if (activeSpinStates == null)
                activeSpinStates = new List<KeyValuePair<int, int>>();
            else
                activeSpinStates.Clear();

            if (activeScratchStates == null)
                activeScratchStates = new List<KeyValuePair<int, int>>();
            else
                activeScratchStates.Clear();

            if (__instance.IsInEditMode || PlayState.isInPracticeMode)
                return;

            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;
            calculatingMaxScore = true;
            noteData.GetMaxPossibleScoreState(new IntRange(0, noteData.noteCount));
            calculatingMaxScore = false;
            Playing = true;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;
            GameplayUI.UpdateUI();
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