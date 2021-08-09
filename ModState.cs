using System.Collections.Generic;
using System.IO;

namespace ScoreMod {
    public class ModState {
        public static bool ShowModdedScore { get; private set; }
        public static ScoreContainer CurrentContainer { get; private set; }
        public static Accuracy LastAccuracy { get; private set; }

        private static string logFilePath;
        private static ScoreContainer[] scoreContainers;
        private static StringTable outputTable;
        private static Dictionary<int, int> releaseIndicesFromStart;
        private static HashSet<int> trackedMisses;

        public static void Initialize(string trackId, int noteCount) {
            if (scoreContainers == null) {
                scoreContainers = new ScoreContainer[ScoreSystemProfile.Profiles.Count];

                for (int i = 0; i < scoreContainers.Length; i++)
                    scoreContainers[i] = new ScoreContainer(ScoreSystemProfile.Profiles[i]);
                
                CurrentContainer = scoreContainers[0];
            }
            
            foreach (var container in scoreContainers)
                container.Initialize(trackId, noteCount);

            if (releaseIndicesFromStart == null)
                releaseIndicesFromStart = new Dictionary<int, int>();
            else
                releaseIndicesFromStart.Clear();

            if (trackedMisses == null)
                trackedMisses = new HashSet<int>();
            else
                trackedMisses.Clear();
        }

        public static void ToggleModdedScoring() {
            ShowModdedScore = !ShowModdedScore;
            GameplayUI.UpdateUI();
            CompleteScreenUI.UpdateUI();
            LevelSelectUI.UpdateModScore();
            LevelSelectUI.UpdateUI();
        }

        public static bool PickScoringSystem(int index) {
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

        public static void AddScore(int amount, float offset, bool isSustainedNoteTick, NoteType noteType, int noteIndex) {
            int oldMultiplier = CurrentContainer.Multiplier;
            bool oldIsPfc = CurrentContainer.GetIsPfc(false);

            if (isSustainedNoteTick) {
                foreach (var container in scoreContainers) {
                    container.AddFlatScore(amount);
                    container.PopMaxScoreSingleTick(noteIndex);
                }
            }
            else {
                switch (noteType) {
                    case NoteType.Match: {
                        foreach (var container in scoreContainers)
                            container.AddScoreFromNoteType(NoteType.Match, 0f);

                        break;
                    }
                    case NoteType.Tap:
                    case NoteType.HoldStart:
                    case NoteType.DrumStart:
                    case NoteType.SectionContinuationOrEnd:
                    case NoteType.DrumEnd:
                        foreach (var container in scoreContainers) {
                            var accuracyForContainer = container.AddScoreFromNoteType(noteType, offset);

                            if (container == CurrentContainer)
                                LastAccuracy = accuracyForContainer;
                        }

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatScore(amount);

                        break;
                }

                foreach (var container in scoreContainers)
                    container.PopMaxScoreNote(noteIndex);
            }

            if (!ShowModdedScore)
                return;

            if (CurrentContainer.Multiplier != oldMultiplier)
                GameplayUI.UpdateMultiplierText();

            if (CurrentContainer.GetIsPfc(false) != oldIsPfc)
                GameplayUI.UpdateFcStar();
        }

        public static void AddMaxScore(int amount, bool isSustainedNoteTick, NoteType noteType, int noteIndex) {
            if (isSustainedNoteTick) {
                foreach (var container in scoreContainers)
                    container.AddFlatMaxScore(amount, noteIndex, true);
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
                            container.AddMaxScoreFromNoteType(noteType, noteIndex);

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatMaxScore(amount, noteIndex, false);

                        break;
                }
            }
        }

        public static void AddReleaseNotePairing(int startIndex, int endIndex) => releaseIndicesFromStart.Add(startIndex, endIndex);

        public static void Miss(int noteIndex, bool countMiss, bool trackMiss) {
            foreach (var container in scoreContainers)
                container.PopMaxScoreNote(noteIndex);

            AddMiss(noteIndex, countMiss, trackMiss);
        }

        public static void MissRemainingNoteTicks(int noteIndex) {
            foreach (var container in scoreContainers)
                container.PopMaxScoreAllTicks(noteIndex);
        }
        
        public static void MissReleaseNoteFromStart(NoteType startNoteType, int startNoteIndex) {
            if (!releaseIndicesFromStart.TryGetValue(startNoteIndex, out int endIndex))
                return;
            
            if (startNoteType == NoteType.HoldStart)
                Miss(endIndex, true, true);
            else
                Miss(endIndex, true, true);
        }

        public static void ResetMultiplier() {
            foreach (var container in scoreContainers)
                container.ResetMultiplier();
        }

        public static void PfcLost() {
            foreach (var container in scoreContainers)
                container.PfcLost();
        }

        public static void LogPlayData(string trackName, bool logDiscrepancies) {
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

            if (logDiscrepancies && (CurrentContainer.MaxScoreSoFar != CurrentContainer.MaxScore || CurrentContainer.GetAnyMaxScoreSoFarUnchecked()))
                Main.Logger.LogWarning("WARNING: Some discrepancies were found during score prediction");
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

        private static void AddMiss(int noteIndex, bool countMiss, bool trackMiss) {
            if (trackMiss) {
                if (trackedMisses.Contains(noteIndex))
                    return;
                
                trackedMisses.Add(noteIndex);
            }
            
            if (!countMiss)
                return;

            foreach (var container in scoreContainers)
                container.AddMiss();
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