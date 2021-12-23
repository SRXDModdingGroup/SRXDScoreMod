using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SRXDScoreMod {
    // Contains patch functions for receiving data from gameplay
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
        private static float tapOffset;
        private static float beatOffset;
        private static PlayableNoteData noteData;
        private static Dictionary<int, int> holdStates;
        private static Dictionary<int, int> spinStates;
        private static Dictionary<int, int> scratchStates;
        private static List<KeyValuePair<int, int>> activeHoldStates;
        private static List<KeyValuePair<int, int>> activeSpinStates;
        private static List<KeyValuePair<int, int>> activeScratchStates;

        // For debug purposes. Gets the type of a given note
        public static NoteType GetNoteType(int noteIndex) => noteData.GetNote(noteIndex).NoteType;
        
        // Log play results, save high scores, and update UI after completing or failing a track
        private static void EndPlay(bool success) {
            Playing = false;
            ModState.LogPlayData(PlayState.TrackInfoRef.asset.title, success);
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
        
        // Gets points added to regular score and adds them to modded score as well
        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.AddScore)), HarmonyPostfix]
        private static void ScoreState_AddScore_Postfix(PlayState.ScoreState __instance, int amount, int noteIndex) {
            if ((!Playing || __instance.isMaxPossibleCalculation) && !calculatingMaxScore)
                return;

            var note = noteData.GetNote(noteIndex);
            var noteType = note.NoteType;
            bool isSustainedNoteTick = false;

            // Check if the points are coming from a sustained note tick and begin tracking sustained note states
            switch (noteType) {
                case NoteType.HoldStart:
                    if (amount == 1 || noteIndex == lastHoldIndex)
                        isSustainedNoteTick = true;
                    else {
                        lastHoldIndex = noteIndex;

                        if (calculatingMaxScore)
                            holdStates.Add(noteIndex, holdStates.Count);
                        else if (!PlayState.isInPracticeMode)
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
                        else if (!PlayState.isInPracticeMode)
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
                        else if (!PlayState.isInPracticeMode)
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
                ModState.AddMaxScore(amount, isSustainedNoteTick, noteType, noteIndex);
            else
                ModState.AddScore(amount, LastOffset ?? 0f, isSustainedNoteTick, noteType, noteIndex);
        }

        // Reset mod score multiplier after missing a note or overbeating, and track the states of missed sustained notes
        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.DropMultiplier)), HarmonyPostfix]
        private static void ScoreState_DropMultiplier_Postfix(int noteIndex) {
            if (!Playing)
                return;

            if (!PlayState.isInPracticeMode) {
                var note = noteData.GetNote(noteIndex);
                var noteType = note.NoteType;

                switch (noteType) {
                    case NoteType.DrumStart:
                        ModState.MissRemainingNoteTicks(noteIndex);
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

                ModState.Miss(noteIndex, true, false);
            }
            
            ModState.ResetMultiplier();
            GameplayUI.UpdateMultiplierText();
        }

        // Unset mod PFC state when regular PFC is lost
        [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.PerfectFullComboLost)), HarmonyPrefix]
        private static bool ScoreState_PerfectFullComboLost_PreFix() {
            if (Playing)
                ModState.PfcLost();

            return true;
        }

        // Continuously track the state of active sustained notes
        [HarmonyPatch(typeof(Track), nameof(Track.Update)), HarmonyPostfix]
        private static void Track_Update_Postfix() {
            if (!Playing || PlayState.isInPracticeMode)
                return;
            
            for (int i = 0; i < activeHoldStates.Count; i++) {
                var pair = activeHoldStates[i];
                int index = pair.Key;
                var state = PlayState.freestyleSectionState[pair.Value];
                var releaseState = state.releaseState;
                
                if (state.failed)
                    ModState.MissRemainingNoteTicks(index);

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
                    ModState.Miss(index, false, false);
                    ModState.MissRemainingNoteTicks(index);
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
                    ModState.Miss(index, false, false);
                    ModState.MissRemainingNoteTicks(index);
                }
                
                if (!state.failed && !state.hasExited)
                    continue;
                
                activeScratchStates.RemoveAt(i);
                i--;
            }
        }

        // Initialize mod values when starting a track
        [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
        private static void Track_PlayTrack_Postfix(Track __instance) {
            Playing = false;
            PlayState = __instance.playStateFirst;

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

            if (__instance.IsInEditMode)
                return;
            
            noteData = PlayState.trackData.NoteData;
            ModState.Initialize(LevelSelectUI.GetTrackId(PlayState.trackData), noteData.noteCount);

            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;

            if (!PlayState.isInPracticeMode) {
                // Create a max score state early for pace prediction
                calculatingMaxScore = true;
                noteData.GetMaxPossibleScoreState(new IntRange(0, noteData.noteCount));
                calculatingMaxScore = false;
            }
            
            Playing = true;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;

            tapOffset = 0.001f * Main.TapTimingOffset.Value;
            beatOffset = 0.001f * Main.BeatTimingOffset.Value;
            
            GameplayUI.UpdateUI();
        }
        
        // Reset score and multiplier when looping in Practice mode
        [HarmonyPatch(typeof(Track), nameof(Track.PracticeTrack)), HarmonyPostfix]
        private static void Track_PracticeTrack_Postfix(Track __instance) {
            PlayState = __instance.playStateFirst;
            noteData = PlayState.trackData.NoteData;
            ModState.Initialize(LevelSelectUI.GetTrackId(PlayState.trackData), noteData.noteCount);
            
            Playing = true;
            LastOffset = null;
            lastHoldIndex = -1;
            lastBeatIndex = -1;
            lastSpinIndex = -1;
            lastScratchIndex = -1;

            tapOffset = 0.001f * Main.TapTimingOffset.Value;
            beatOffset = 0.001f * Main.BeatTimingOffset.Value;
            
            GameplayUI.UpdateUI();
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

        // Check when the player exits a track
        [HarmonyPatch(typeof(XDPauseMenu), nameof(XDPauseMenu.ExitButtonPressed)), HarmonyPostfix]
        private static void XDPauseMenu_ExitButtonPressed_Postfix() {
            Playing = false;
        }
        
        // Store the timing value passed in when the player hits a tap or liftoff
        [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracy)), HarmonyPostfix]
        private static void GameplayVariables_GetTimingAccuracy_Postfix(float timeOffset) {
            if (Playing)
                LastOffset = timeOffset + tapOffset;
        }

        // Store the timing value passed in when the player hits a beat or hard beat release
        [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracyForBeat)), HarmonyPostfix]
        private static void GameplayVariables_GetTimingAccuracyForBeat_Postfix(float timeOffset) {
            if (Playing)
                LastOffset = timeOffset + beatOffset;
        }
    }
}