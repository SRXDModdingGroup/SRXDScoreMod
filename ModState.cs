using System;
using System.IO;

namespace ScoreMod {
    public class ModState {
        public static bool ShowModdedScore { get; private set; }
        public static ScoreContainer CurrentContainer { get; private set; }
        public static Accuracy LastAccuracy { get; private set; }

        private static string logFilePath;
        private static ScoreContainer[] scoreContainers;
        private static StringTable outputTable;

        public static void Initialize(string trackId) {
            if (scoreContainers == null) {
                InitScoreContainers();
                CurrentContainer = scoreContainers[0];
            }
            else {
                foreach (var container in scoreContainers)
                    container.Clear();
            }
            
            if (string.IsNullOrWhiteSpace(trackId))
                return;

            foreach (var container in scoreContainers)
                container.SetTrackId(trackId);
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

        public static void AddScore(int amount, float offset, bool isSustainedNoteTick, NoteType noteType) {
            int oldMultiplier = CurrentContainer.Multiplier;
            bool oldIsPfc = CurrentContainer.GetIsPfc();

            if (isSustainedNoteTick) {
                foreach (var container in scoreContainers)
                    container.AddFlatScore(amount);
            }
            else {
                switch (noteType) {
                    case NoteType.Match: {
                        foreach (var container in scoreContainers)
                            container.AddScoreFromSource(NoteType.Match);

                        break;
                    }
                    case NoteType.Tap:
                    case NoteType.HoldStart:
                    case NoteType.DrumStart:
                    case NoteType.SectionContinuationOrEnd:
                    case NoteType.DrumEnd:
                        foreach (var container in scoreContainers) {
                            var accuracyForContainer = container.AddScoreFromSource(noteType, offset);

                            if (container == CurrentContainer)
                                LastAccuracy = accuracyForContainer;
                        }

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatScore(amount);

                        break;
                }
            }

            if (!ShowModdedScore)
                return;

            if (CurrentContainer.Multiplier != oldMultiplier)
                GameplayUI.UpdateMultiplierText();

            if (CurrentContainer.GetIsPfc() != oldIsPfc)
                GameplayUI.UpdateFcStar();
        }

        public static void AddMaxScore(int amount, bool isSustainedNoteTick, NoteType noteType) {
            if (isSustainedNoteTick) {
                foreach (var container in scoreContainers)
                    container.AddFlatMaxScore(amount);
            }
            else {
                switch (noteType) {
                    case NoteType.Match:
                    case NoteType.Tap:
                    case NoteType.HoldStart:
                    case NoteType.DrumStart:
                    case NoteType.SectionContinuationOrEnd:
                    case NoteType.DrumEnd:
                        foreach (var container in scoreContainers)
                            container.AddMaxScoreFromSource(noteType);

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatMaxScore(amount);

                        break;
                }
            }
        }
        
        public static void Miss() {
            foreach (var container in scoreContainers)
                container.Miss();
        }

        public static void PfcLost() {
            foreach (var container in scoreContainers)
                container.PfcLost();
        }

        public static void LogPlayData(string trackName) {
            if (!TryGetLogFilePath())
                return;
            
            if (outputTable == null) {
                outputTable = new StringTable(19, scoreContainers.Length + 1);

                outputTable.SetHeader(
                    "Profile",
                    "Score",
                    "Best",
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
                    container.HighScore.ToString(),
                    container.MaxScore.ToString(),
                    container.GetRank(),
                    $"({(float) container.Score / container.MaxScore:P})",
                    container.GetAccuracyRating().ToString("P"),
                    $"{early} :",
                    late.ToString(),
                    container.GetAccuracyCount(Accuracy.Perfect, out _).ToString(),
                    container.GetAccuracyCount(Accuracy.Great, out int loss0).ToString(),
                    $"(-{loss0})",
                    container.GetAccuracyCount(Accuracy.Good, out int loss1).ToString(),
                    $"(-{loss1})",
                    container.GetAccuracyCount(Accuracy.Okay, out int loss2).ToString(),
                    $"(-{loss2})",
                    container.GetAccuracyCount(Accuracy.Miss, out _).ToString(),
                    lossToMisses.ToString(),
                    lossToAccuracy.ToString());
            }

            using (var writer = File.AppendText(logFilePath)) {
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
                if (HighScoresContainer.TrySetHighScore(trackId, container))
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
        
        private static bool TryGetLogFilePath() {
            if (!string.IsNullOrWhiteSpace(logFilePath))
                return true;

            if (!Main.TryGetFileDirectory(out string fileDirectory))
                return false;

            logFilePath = Path.Combine(fileDirectory, "History.txt");

            return true;
        }
    }
}