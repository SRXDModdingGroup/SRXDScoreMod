using System;
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
        private static Dictionary<int, KeyValuePair<int, float>> remainingNotes;
        private static Dictionary<int, KeyValuePair<int, float>> remainingNoteTicks;
        private static Dictionary<int, int> releaseIndicesFromStart;
        private static HashSet<int> trackedMisses;

        public static void Initialize(string trackId) {
            if (scoreContainers == null) {
                InitScoreContainers();
                CurrentContainer = scoreContainers[0];
            }
            else {
                foreach (var container in scoreContainers)
                    container.Clear();
            }

            if (remainingNoteTicks == null)
                remainingNoteTicks = new Dictionary<int, KeyValuePair<int, float>>();
            else
                remainingNoteTicks.Clear();

            if (releaseIndicesFromStart == null)
                releaseIndicesFromStart = new Dictionary<int, int>();
            else
                releaseIndicesFromStart.Clear();

            if (remainingNotes == null)
                remainingNotes = new Dictionary<int, KeyValuePair<int, float>>();
            else
                remainingNotes.Clear();

            if (trackedMisses == null)
                trackedMisses = new HashSet<int>();
            else
                trackedMisses.Clear();

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

        public static void AddScore(int amount, float offset, bool isSustainedNoteTick, NoteType noteType, int noteIndex) {
            int oldMultiplier = CurrentContainer.Multiplier;
            bool oldIsPfc = CurrentContainer.GetIsPfc(false);
            var source = GetSourceFromNoteType(noteType, isSustainedNoteTick);

            if (isSustainedNoteTick) {
                bool ticksRemain = remainingNoteTicks.TryGetValue(noteIndex, out var pair);
                
                foreach (var container in scoreContainers)
                    container.AddFlatScore(amount, source, ticksRemain);

                if (ticksRemain) {
                    int currentTicks = pair.Key;

                    if (amount >= currentTicks)
                        remainingNoteTicks.Remove(noteIndex);
                    else
                        remainingNoteTicks[noteIndex] = new KeyValuePair<int, float>(currentTicks - amount, pair.Value);
                }
            }
            else {
                bool noteRemains = remainingNotes.Remove(noteIndex);
                
                switch (noteType) {
                    case NoteType.Match: {
                        foreach (var container in scoreContainers)
                            container.AddScoreFromNoteType(NoteType.Match, source, 0f, noteRemains);

                        break;
                    }
                    case NoteType.Tap:
                    case NoteType.HoldStart:
                    case NoteType.DrumStart:
                    case NoteType.SectionContinuationOrEnd:
                    case NoteType.DrumEnd:
                        foreach (var container in scoreContainers) {
                            var accuracyForContainer = container.AddScoreFromNoteType(noteType, source, offset, noteRemains);

                            if (container == CurrentContainer)
                                LastAccuracy = accuracyForContainer;
                        }

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatScore(amount, source, noteRemains);

                        break;
                }
            }

            if (!ShowModdedScore)
                return;

            if (CurrentContainer.Multiplier != oldMultiplier)
                GameplayUI.UpdateMultiplierText();

            if (CurrentContainer.GetIsPfc(false) != oldIsPfc)
                GameplayUI.UpdateFcStar();
        }

        public static void AddMaxScore(int amount, bool isSustainedNoteTick, NoteType noteType, int noteIndex, float noteTime) {
            var source = GetSourceFromNoteType(noteType, isSustainedNoteTick);
            
            if (isSustainedNoteTick) {
                foreach (var container in scoreContainers)
                    container.AddFlatMaxScore(amount, source);

                if (remainingNoteTicks.TryGetValue(noteIndex, out var pair))
                    remainingNoteTicks[noteIndex] = new KeyValuePair<int, float>(pair.Key + amount, pair.Value);
                else
                    remainingNoteTicks.Add(noteIndex, new KeyValuePair<int, float>(amount, noteTime));
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
                            container.AddMaxScoreFromNoteType(noteType, source);

                        break;
                    default:
                        foreach (var container in scoreContainers)
                            container.AddFlatMaxScore(amount, source);

                        break;
                }

                remainingNotes.Add(noteIndex, new KeyValuePair<int, float>(noteIndex, noteTime));
            }
        }

        public static void AddReleaseNotePairing(int startIndex, int endIndex) => releaseIndicesFromStart.Add(startIndex, endIndex);

        public static void Miss(NoteType noteType, int noteIndex, bool countMiss, bool trackMiss) {
            if (remainingNotes.Remove(noteIndex)) {
                var source = GetSourceFromNoteType(noteType, false);
                    
                foreach (var container in scoreContainers)
                    container.MissScoreFromNoteType(noteType, source);
            }

            AddMiss(noteIndex, countMiss, trackMiss);
        }

        public static void MissRemainingNoteTicks(NoteType noteType, int noteIndex) {
            if (!remainingNoteTicks.TryGetValue(noteIndex, out var pair))
                return;

            var source = GetSourceFromNoteType(noteType, true);
            
            foreach (var container in scoreContainers)
                container.MissFlatScore(pair.Key, source);

            remainingNoteTicks.Remove(noteIndex);
        }
        
        public static void MissReleaseNoteFromStart(NoteType startNoteType, int startNoteIndex) {
            if (!releaseIndicesFromStart.TryGetValue(startNoteIndex, out int endIndex))
                return;
            
            if (startNoteType == NoteType.HoldStart)
                Miss(NoteType.SectionContinuationOrEnd, endIndex, true, true);
            else
                Miss(NoteType.DrumEnd, endIndex, true, true);
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

            if (!logDiscrepancies)
                return;
            
            bool anyDiscrepancies = remainingNotes.Count > 0 || remainingNoteTicks.Count > 0;
            
            foreach (object obj in Enum.GetValues(typeof(PointSource))) {
                var source = (PointSource)obj;
                int max = CurrentContainer.MaxScoreBySource[source];
                int maxSoFar = CurrentContainer.MaxScoreSoFarBySource[source];

                if (maxSoFar == max)
                    continue;

                anyDiscrepancies = true;

                break;
            }
            
            if (!anyDiscrepancies)
                return;
            
            Main.Logger.LogWarning("WARNING: Some discrepancies were found during score prediction");
            
            foreach (object obj in Enum.GetValues(typeof(PointSource))) {
                var source = (PointSource)obj;
                int max = CurrentContainer.MaxScoreBySource[source];
                int maxSoFar = CurrentContainer.MaxScoreSoFarBySource[source];
                
                if (maxSoFar != max)
                    Main.Logger.LogWarning($"{source}: {maxSoFar} - {max} = {maxSoFar - max}");
            }

            if (remainingNotes.Count > 0) {
                Main.Logger.LogWarning("");

                foreach (var pair in remainingNotes)
                    Main.Logger.LogWarning($"{pair.Key} {pair.Value.Key} {pair.Value.Value}");
            }

            if (remainingNoteTicks.Count > 0) {
                Main.Logger.LogWarning("");

                foreach (var pair in remainingNoteTicks)
                    Main.Logger.LogWarning($"{pair.Key} {pair.Value.Key} {pair.Value.Value}");
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
        
        private static PointSource GetSourceFromNoteType(NoteType noteType, bool isSustainedNoteTick) {
            switch (noteType) {
                case NoteType.Match:
                    return PointSource.Match;
                case NoteType.Tap:
                    return PointSource.Tap;
                case NoteType.HoldStart:
                    if (isSustainedNoteTick)
                        return PointSource.HoldSustain;

                    return PointSource.HoldStart;
                case NoteType.SectionContinuationOrEnd:
                    return PointSource.HoldRelease;
                case NoteType.DrumStart:
                    if (isSustainedNoteTick)
                        return PointSource.BeatSustain;

                    return PointSource.Beat;
                case NoteType.DrumEnd:
                    return PointSource.BeatRelease;
                case NoteType.SpinStart:
                case NoteType.SpinRightStart:
                case NoteType.SpinLeftStart:
                    if (isSustainedNoteTick)
                        return PointSource.SpinSustain;

                    return PointSource.SpinStart;
                case NoteType.ScratchStart:
                    if (isSustainedNoteTick)
                        return PointSource.ScratchSustain;

                    return PointSource.ScratchStart;
            }

            return PointSource.ScratchStart;
        }
    }
}