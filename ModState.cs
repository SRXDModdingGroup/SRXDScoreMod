﻿using System;
using System.IO;

namespace ScoreMod {
    public class ModState {
        public static bool ShowModdedScore { get; private set; }
        public static ScoreContainer CurrentContainer { get; private set; }
        public static ScoreContainer.Accuracy LastAccuracy { get; private set; }

        private static ScoreContainer[] scoreContainers;
        private static StringTable outputTable;

        public static void Initialize() {
            if (scoreContainers == null) {
                InitScoreContainers();
                CurrentContainer = scoreContainers[0];
            }
            else {
                foreach (var container in scoreContainers)
                    container.Clear();
            }
        }

        public static void ToggleModdedScoring() {
            ShowModdedScore = !ShowModdedScore;
            
            if (scoreContainers == null) {
                InitScoreContainers();
                CurrentContainer = scoreContainers[0];
            }
            
            GameplayUI.UpdateUI();
            CompleteScreenUI.UpdateUI();
            LevelSelectUI.UpdateModScore();
            LevelSelectUI.UpdateUI();
        }

        public static bool PickScoringSystem(int index) {
            if (scoreContainers == null)
                InitScoreContainers();
                
            if (index >= scoreContainers.Length)
                return false;

            CurrentContainer = scoreContainers[index];
            ShowModdedScore = true;
            GameplayUI.UpdateUI();
            CompleteScreenUI.UpdateUI();
            LevelSelectUI.UpdateModScore();
            LevelSelectUI.UpdateUI();

            return true;
        }

        public static void AddPoints(int amount, float offset, bool isSustainedNoteTick, NoteType noteType) {
            int oldMultiplier = CurrentContainer.Multiplier;
            bool oldIsPfc = CurrentContainer.GetIsPfc();

            ScoreContainer.PointSource source;

            switch (noteType) {
                case NoteType.Match:
                    source = ScoreContainer.PointSource.Match;
                    
                    break;
                case NoteType.DrumStart when !isSustainedNoteTick:
                    source = ScoreContainer.PointSource.Beat;
                    
                    break;
                case NoteType.HoldStart when !isSustainedNoteTick:
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
                        var accuracyForContainer = container.AddPointsFromSource(source, offset);

                        if (container == CurrentContainer)
                            LastAccuracy = accuracyForContainer;
                    }

                    break;
            }

            if (!ShowModdedScore || !GameplayState.Playing)
                return;

            if (CurrentContainer.Multiplier != oldMultiplier)
                GameplayUI.UpdateMultiplierText();

            if (CurrentContainer.GetIsPfc() != oldIsPfc)
                GameplayUI.UpdateFcStar();
        }
        
        public static void Miss() {
            foreach (var container in scoreContainers)
                container.Miss();
        }

        public static void PfcLost() {
            foreach (var container in scoreContainers)
                container.PfcLost();
        }

        public static void BeginCalculatingMaxScore() {
            foreach (var container in scoreContainers)
                container.BeginCalculatingMaxScore();
        }

        public static void FinishCalculatingMaxScore() {
            foreach (var container in scoreContainers)
                container.FinishCalculatingMaxScore();
        }

        public static void LogPlayData(string trackName) {
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
        }

        public static void SavePlayData(string trackId) {
            bool anyChanged = false;
            
            foreach (var container in scoreContainers) {
                if (HighScoresContainer.SetHighScore(trackId, container.Profile.GetUniqueId(), container.Score, container.GetRank()))
                    anyChanged = true;
            }
            
            if (anyChanged)
                HighScoresContainer.SaveHighScores();
        }
        
        private static void InitScoreContainers() {
            scoreContainers = new ScoreContainer[ScoreSystemProfile.Profiles.Count];

            for (int i = 0; i < scoreContainers.Length; i++)
                scoreContainers[i] = new ScoreContainer(ScoreSystemProfile.Profiles[i]);
        }

        private static void LogToFile(StreamWriter writer, string text) {
            Main.Logger.LogMessage(text);
            writer.WriteLine(text);
        }
        private static void LogToFile(StreamWriter writer) {
            Main.Logger.LogMessage("");
            writer.WriteLine();
        }
    }
}