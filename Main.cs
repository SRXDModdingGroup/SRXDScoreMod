using System;
using System.IO;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.UI;
using Input = UnityEngine.Input;
using KeyCode = UnityEngine.KeyCode;

namespace ScoreMod {
    [BepInPlugin("ScoreMod", "ScoreMod", "1.0.0.0")]
    public class Main : BasePlugin {
        
        public static ManualLogSource Logger { get; private set; }

        public override void Load() {
            Logger = Log;
            Harmony.CreateAndPatchAll(typeof(Run));
        }

        private class Run {
            private static ScoreContainer[] scoreContainers;
            private static ScoreContainer currentContainer;
            private static Note[] notes;
            private static string trackName;
            private static bool playing;
            private static bool calculatingMaxScore;
            private static float? lastOffset;
            private static int lastHoldIndex;
            private static int lastBeatIndex;
            private static TextNumber scoreNumber;
            private static TextCharacter multiplierNumber;
            private static bool showModdedScore;
            private static string realRank;
            private static XDLevelCompleteMenu xdLevelCompleteMenu;
            private static Color defaultScoreNumberColor;
            private static bool timingFeedbackSpawned;
            private static GameObject timingFeedbackObject;
            private static ScoreContainer.Accuracy lastAccuracy;
            private static Image fcStar;
            private static Sprite fcSprite;
            private static Sprite pfcSprite;
            private static PlayState playState;
            private static bool levelCompleteMenuOpen;
            private static bool pickedNewScoreSystem;
            private static StringTable outputTable;
            private static TMP_Text accuracyLabel;

            [HarmonyPatch(typeof(Game), nameof(Game.Update)), HarmonyPostfix]
            private static void Game_Update_Postfix() {
                if (Input.GetKeyDown(KeyCode.P))
                    pickedNewScoreSystem = false;

                if (Input.GetKey(KeyCode.P)) {
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                        PickNewSystem(0);
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                        PickNewSystem(1);
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                        PickNewSystem(2);
                    else if (Input.GetKeyDown(KeyCode.Alpha4))
                        PickNewSystem(3);
                    else if (Input.GetKeyDown(KeyCode.Alpha5))
                        PickNewSystem(4);
                    else if (Input.GetKeyDown(KeyCode.Alpha6))
                        PickNewSystem(5);
                    else if (Input.GetKeyDown(KeyCode.Alpha7))
                        PickNewSystem(6);
                    else if (Input.GetKeyDown(KeyCode.Alpha8))
                        PickNewSystem(7);
                    else if (Input.GetKeyDown(KeyCode.Alpha9))
                        PickNewSystem(8);
                }

                if (Input.GetKeyUp(KeyCode.P) && !pickedNewScoreSystem) {
                    showModdedScore = !showModdedScore;

                    if (scoreContainers == null) {
                        InitScoreContainers();
                        currentContainer = scoreContainers[0];
                    }
                    
                    UpdateUI();
                }

                void PickNewSystem(int index) {
                    if (scoreContainers == null)
                        InitScoreContainers();
                    
                    if (index >= scoreContainers.Length)
                        return;

                    currentContainer = scoreContainers[index];
                    showModdedScore = true;
                    UpdateUI();
                    pickedNewScoreSystem = true;
                }
            }

            [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack)), HarmonyPostfix]
            private static void Track_PlayTrack_Postfix(Track __instance) {
                if (scoreContainers == null) {
                    InitScoreContainers();
                    currentContainer = scoreContainers[0];
                }
                else {
                    foreach (var container in scoreContainers)
                        container.Clear();
                }

                playState = __instance.playStateFirst;
                trackName = playState.TrackInfoRef.asset.title;
                playing = true;
                calculatingMaxScore = false;
                lastOffset = null;
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
            
            [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.FinaliseScore)), HarmonyPostfix]
            public static void ScoreState_FinaliseScore_Postfix() {
                if (!playing || calculatingMaxScore)
                    return;

                foreach (var container in scoreContainers)
                    container.BeginCalculatingMaxScore();

                playing = false;
                calculatingMaxScore = true;
                lastOffset = null;
                lastHoldIndex = -1;
                lastBeatIndex = -1;
            }

            [HarmonyPatch(typeof(Track), nameof(Track.FailSong)), HarmonyPostfix]
            private static void Track_FailSong_Postfix() {
                EndPlay();
            }

            [HarmonyPatch(typeof(Track), nameof(Track.CompleteSong)), HarmonyPostfix]
            private static void Track_CompleteSong_Postfix() {
                EndPlay();
            }

            [HarmonyPatch(typeof(XDPauseMenu), nameof(XDPauseMenu.ExitButtonPressed)), HarmonyPostfix]
            private static void XDPauseMenu_ExitButtonPressed_Postfix() {
                playing = false;
                calculatingMaxScore = false;
            }
            
            [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracy)), HarmonyPostfix]
            private static void GameplayVariables_GetTimingAccuracy_Postfix(float timeOffset) {
                if (playing)
                    lastOffset = timeOffset;
            }
            
            [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTimingAccuracyForBeat)), HarmonyPostfix]
            private static void GameplayVariables_GetTimingAccuracyForBeat_Postfix(float timeOffset) {
                if (playing)
                    lastOffset = timeOffset;
            }
            
            [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.AddScore)), HarmonyPostfix]
            private static void ScoreState_AddScore_Postfix(PlayState.ScoreState __instance, int amount, int noteIndex) {
                if (!playing && !calculatingMaxScore || notes == null || noteIndex >= notes.Length || noteIndex < 0)
                    return;
                
                var note = notes[noteIndex];
                
                int oldMultiplier = currentContainer.Multiplier;
                bool oldIsPfc = currentContainer.GetIsPfc();

                ScoreContainer.PointSource source;

                switch (note.NoteType) {
                    case NoteType.Match:
                        source = ScoreContainer.PointSource.Match;
                        
                        break;
                    case NoteType.DrumStart when noteIndex != lastBeatIndex:
                        source = ScoreContainer.PointSource.Beat;
                        
                        break;
                    case NoteType.HoldStart when noteIndex != lastHoldIndex:
                        source = ScoreContainer.PointSource.HoldStart;
                        
                        break;
                    case NoteType.SectionContinuationOrEnd:
                        source = ScoreContainer.PointSource.Liftoff;

                        break;
                    case NoteType.Tap:
                        source = ScoreContainer.PointSource.Tap;
                        
                        break;
                    case NoteType.DrumEnd:
                        source = ScoreContainer.PointSource.BeatRelease;
                        
                        break;
                    default:
                        source = ScoreContainer.PointSource.SustainedNoteTick;

                        break;
                }

                switch (source) {
                    case ScoreContainer.PointSource.SustainedNoteTick:
                        foreach (var container in scoreContainers)
                            container.AddSustainedNoteTickScore(amount);

                        break;
                    case ScoreContainer.PointSource.Match:
                        foreach (var container in scoreContainers)
                            container.AddPointsFromSource(ScoreContainer.PointSource.Match);

                        break;
                    default:
                        foreach (var container in scoreContainers) {
                            var accuracy = container.AddPointsFromSource(source, lastOffset ?? 0f);

                            if (container == currentContainer)
                                lastAccuracy = accuracy;
                        }

                        break;
                }

                if (showModdedScore && playing) {
                    if (currentContainer.Multiplier != oldMultiplier)
                        multiplierNumber.Text = GetMultiplierAsText();

                    if (currentContainer.GetIsPfc() != oldIsPfc)
                        fcStar.sprite = fcSprite;
                }

                if (note.NoteType == NoteType.HoldStart)
                    lastHoldIndex = noteIndex;
                
                if (note.NoteType == NoteType.DrumStart)
                    lastBeatIndex = noteIndex;
            }

            [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.DropMultiplier)), HarmonyPostfix]
            private static void ScoreState_DropMultiplier_Postfix() {
                if (!playing)
                    return;

                foreach (var container in scoreContainers)
                    container.Miss();

                if (showModdedScore)
                    multiplierNumber.Text = GetMultiplierAsText();
            }

            [HarmonyPatch(typeof(XDHudCanvases), nameof(XDHudCanvases.Start)), HarmonyPostfix]
            private static void XDHudCanvases_Start_Postfix(XDHudCanvases __instance) {
                if (scoreNumber == null)
                    defaultScoreNumberColor = __instance.score.color;

                scoreNumber = __instance.score;
                multiplierNumber = __instance.multiplier;

                if (showModdedScore)
                    scoreNumber.color = Color.red;
                else
                    scoreNumber.color = defaultScoreNumberColor;

                fcStar = __instance.fcStar;
                fcSprite = __instance.fcStarSprite;
                pfcSprite = __instance.pfcStarSprite;
            }

            [HarmonyPatch(typeof(TextNumber), nameof(TextNumber.Update)), HarmonyPrefix]
            private static bool TextNumber_Update_Prefix(TextNumber __instance) {
                if (showModdedScore && playing && __instance == scoreNumber)
                    __instance.desiredNumber = currentContainer.Score;

                return true;
            }
            
            [HarmonyPatch(typeof(TextCharacter), nameof(TextCharacter.Text), MethodType.Setter), HarmonyPrefix]
            private static bool TextCharacter_Text_Set_Prefix(TextCharacter __instance, ref string value) {
                if (showModdedScore && playing && __instance == multiplierNumber)
                    value = GetMultiplierAsText();

                return true;
            }

            [HarmonyPatch(typeof(XDLevelCompleteMenu), nameof(XDLevelCompleteMenu.Setup)), HarmonyPostfix]
            private static void XDLevelCompleteMenu_Setup_Postfix(XDLevelCompleteMenu __instance, PlayState playState) {
                xdLevelCompleteMenu = __instance;
                realRank = __instance.rankAnimator.rankText.text;

                if (accuracyLabel == null) {
                    foreach (var text in __instance.accuracyGameObject.GetComponentsInChildren<TMP_Text>()) {
                        if (text == __instance.accuracyBonusText)
                            continue;

                        accuracyLabel = text;

                        break;
                    }
                }

                UpdateUI();
                levelCompleteMenuOpen = true;
            }

            [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter), HarmonyPrefix]
            private static bool TMP_Text_SetTextInternal_Prefix(TMP_Text __instance, ref string value) {
                if (!levelCompleteMenuOpen || !showModdedScore)
                    return true;
                
                if (__instance == xdLevelCompleteMenu.scoreValueText)
                    value = currentContainer.Score.ToString();

                return true;
            }
            
            [HarmonyPatch(typeof(TrackGameplayFeedbackObjects), nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback)), HarmonyPrefix]
            private static bool TrackGameplayFeedbackObjects_PlayTimingFeedback_Prefix(ref NoteTimingAccuracy noteTimingAccuracy) {
                if (noteTimingAccuracy == NoteTimingAccuracy.Pending ||
                    noteTimingAccuracy == NoteTimingAccuracy.Valid ||
                    noteTimingAccuracy == NoteTimingAccuracy.Failed ||
                    noteTimingAccuracy == NoteTimingAccuracy.Invalidated)
                    return true;

                timingFeedbackSpawned = true;

                if (!showModdedScore)
                    return true;

                switch (lastAccuracy) {
                    case ScoreContainer.Accuracy.Perfect:
                    case ScoreContainer.Accuracy.Great:
                        noteTimingAccuracy = NoteTimingAccuracy.Perfect;

                        break;
                    case ScoreContainer.Accuracy.Good:
                    case ScoreContainer.Accuracy.Okay: 
                        if (lastOffset > 0f)
                            noteTimingAccuracy = NoteTimingAccuracy.Late;
                        else
                            noteTimingAccuracy = NoteTimingAccuracy.Early;

                        break;
                }

                return true;
            }
            
            [HarmonyPatch(typeof(TrackGameplayFeedbackObjects), nameof(TrackGameplayFeedbackObjects.PlayTimingFeedback)), HarmonyPostfix]
            private static void TrackGameplayFeedbackObjects_PlayTimingFeedback_Postfix(NoteTimingAccuracy noteTimingAccuracy) {
                if (noteTimingAccuracy == NoteTimingAccuracy.Pending ||
                    noteTimingAccuracy == NoteTimingAccuracy.Valid ||
                    noteTimingAccuracy == NoteTimingAccuracy.Failed ||
                    noteTimingAccuracy == NoteTimingAccuracy.Invalidated ||
                    timingFeedbackObject == null)
                    return;

                string newText;
                int target;
                
                if (showModdedScore) {
                    switch (lastAccuracy) {
                        case ScoreContainer.Accuracy.Perfect:
                            newText = "Perfect";
                            target = 3;
                        
                            break;
                        case ScoreContainer.Accuracy.Great:
                            newText = "Great";
                            target = 3;

                            break;
                        case ScoreContainer.Accuracy.Good:
                            newText = "Good";

                            if (lastOffset > 0f)
                                target = 2;
                            else
                                target = 1;
                        
                            break;
                        default:
                            newText = "Okay";
                            
                            if (lastOffset > 0f)
                                target = 2;
                            else
                                target = 1;
                        
                            break;
                    }
                }
                else {
                    switch (noteTimingAccuracy) {
                        case NoteTimingAccuracy.Perfect:
                            newText = "Perfect";
                            target = 3;
                            
                            break;
                        case NoteTimingAccuracy.Early:
                            newText = "Early";
                            target = 1;
                            
                            break;
                        default:
                            newText = "Late";
                            target = 2;
                            
                            break;
                    }
                }

                var transform = timingFeedbackObject.transform.GetChild(target);
                var feedbackText = transform.GetComponent<TextCharacter>();

                feedbackText.Text = newText;
                timingFeedbackObject = null;
            }

            [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive)), HarmonyPostfix]
            private static void GameObject_SetActive_Postfix(GameObject __instance, bool value) {
                if (levelCompleteMenuOpen && !value && __instance == xdLevelCompleteMenu.gameObject) {
                    levelCompleteMenuOpen = false;

                    return;
                }

                if (!timingFeedbackSpawned || !value || __instance.name != "AccuracyEffectXD(Clone)")
                    return;

                timingFeedbackObject = __instance;
                timingFeedbackSpawned = false;
            }
            
            [HarmonyPatch(typeof(PlayState.ScoreState), nameof(PlayState.ScoreState.PerfectFullComboLost)), HarmonyPrefix]
            private static bool ScoreState_PerfectFullComboLost_PreFix() {
                if (!playing)
                    return true;

                foreach (var container in scoreContainers)
                    container.PfcLost();

                return true;
            }

            [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter), HarmonyPrefix]
            private static bool Image_Sprite_Set_Prefix(Image __instance, ref Sprite value) {
                if (playing && __instance == fcStar)
                    value = playState.fullComboState == FullComboState.PerfectFullCombo && (!showModdedScore || currentContainer.GetIsPfc()) ? pfcSprite : fcSprite;

                return true;
            }

            // [HarmonyPatch(typeof(PerformanceGraph), nameof(PerformanceGraph.Setup)), HarmonyPrefix]
            // private static bool PerformanceGraph_Setup_Prefix(ref Il2CppStructArray<float> samples) {
            //     samples[0] = 0.5f;
            //     samples[1] = 0.5f;
            //     samples[2] = 0.5f;
            //
            //     return true;
            // }

            private static void EndPlay() {
                playing = false;
                calculatingMaxScore = false;
                
                if (outputTable == null) {
                    outputTable = new StringTable(18, scoreContainers.Length + 1);
                    
                    outputTable.SetHeader(
                        "Profile",
                        "Score",
                        "Max",
                        "Rank",
                        string.Empty,
                        "Accuracy",
                        "E / L",
                        string.Empty,
                        "Perfects",
                        "Greats",
                        string.Empty,
                        "Goods",
                        string.Empty,
                        "Okays",
                        string.Empty,
                        "Misses",
                        "LT Miss",
                        "LT Acc.");
                    outputTable.SetDataAlignment(
                        StringTable.Alignment.Left,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Left,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right,
                        StringTable.Alignment.Right);
                }
                else
                    outputTable.ClearData();

                for (int i = 0; i < scoreContainers.Length; i++) {
                    var container = scoreContainers[i];
                    
                    container.FinishCalculatingMaxScore();
                    container.GetLoss(out int lossToMisses, out int lossToAccuracy);
                    container.GetEarlyLateBalance(out int early, out int late);
                    
                    outputTable.SetRow(i + 1,
                        container.Profile.Name,
                        container.Score.ToString(),
                        container.MaxScore.ToString(),
                        container.GetRank(),
                        $"({(float) container.Score / container.MaxScore:P})",
                        container.GetAccuracyRating().ToString("P"),
                        $"{early} :",
                        late.ToString(),
                        container.GetAccuracyCount(ScoreContainer.Accuracy.Perfect, out _).ToString(),
                        container.GetAccuracyCount(ScoreContainer.Accuracy.Great, out int loss0).ToString(),
                        $"(-{loss0})",
                        container.GetAccuracyCount(ScoreContainer.Accuracy.Good, out int loss1).ToString(),
                        $"(-{loss1})",
                        container.GetAccuracyCount(ScoreContainer.Accuracy.Okay, out int loss2).ToString(),
                        $"(-{loss2})",
                        container.GetAccuracyCount(ScoreContainer.Accuracy.Miss, out _).ToString(),
                        lossToMisses.ToString(),
                        lossToAccuracy.ToString());
                }
                
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"ScoreMod History.txt");

                using (var writer = File.AppendText(path)) {
                    LogToFile(writer, $"Track: {trackName}");
                    LogToFile(writer);

                    foreach (string row in outputTable.GetRows())
                        LogToFile(writer, row);

                    LogToFile(writer);
                }
                
                UpdateUI();
            }

            private static void InitScoreContainers() {
                scoreContainers = new ScoreContainer[ScoreSystemProfile.Profiles.Count];

                for (int i = 0; i < scoreContainers.Length; i++)
                    scoreContainers[i] = new ScoreContainer(ScoreSystemProfile.Profiles[i]);
            }

            private static void UpdateUI() {
                if (xdLevelCompleteMenu != null && xdLevelCompleteMenu.gameObject.activeSelf) {
                    if (showModdedScore) {
                        xdLevelCompleteMenu.accuracyBonusText.SetText(currentContainer.Profile.Name);
                        xdLevelCompleteMenu.PfcBonusGameObject.SetActive(false);
                        xdLevelCompleteMenu.pfcStatusText.SetText(currentContainer.GetIsPfc() ? "PFC" : "FC");
                        xdLevelCompleteMenu.scoreValueText.SetText(currentContainer.Score.ToString());
                        xdLevelCompleteMenu.rankAnimator.SetText(currentContainer.GetRank());
                        accuracyLabel.SetText("Current Profile");
                    }
                    else {
                        bool realIsPfc = playState.fullComboState == FullComboState.PerfectFullCombo;

                        xdLevelCompleteMenu.accuracyBonusText.SetText(showModdedScore ? currentContainer.Profile.Name : playState.scoreState.AccuracyBonus.ToString());
                        xdLevelCompleteMenu.PfcBonusGameObject.SetActive(realIsPfc);
                        xdLevelCompleteMenu.pfcStatusText.SetText(realIsPfc ? "PFC" : "FC");
                        xdLevelCompleteMenu.scoreValueText.SetText(playState.TotalScore.ToString());
                        xdLevelCompleteMenu.rankAnimator.SetText(realRank);
                        accuracyLabel.SetText("Accuracy");
                    }
                }
                
                if (!playing)
                    return;

                if (scoreNumber != null && scoreNumber.gameObject.activeSelf) {
                    if (showModdedScore)
                        scoreNumber.color = Color.red;
                    else
                        scoreNumber.color = defaultScoreNumberColor;
                }

                if (fcStar != null)
                    fcStar.sprite = pfcSprite;

                if (showModdedScore)
                    multiplierNumber.Text = GetMultiplierAsText();
                else
                    multiplierNumber.Text = $"{playState.multiplier}<size=65%>x</size>";
            }

            private static void LogToFile(StreamWriter writer, string text) {
                Logger.LogMessage(text);
                writer.WriteLine(text);
            }
            private static void LogToFile(StreamWriter writer) {
                Logger.LogMessage("");
                writer.WriteLine();
            }
            
            private static string GetMultiplierAsText() => $"{currentContainer.Multiplier}<size=65%>x</size>";
        }
    }
}