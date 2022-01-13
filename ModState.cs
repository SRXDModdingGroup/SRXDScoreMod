using System.Collections.Generic;
using System.IO;

namespace SRXDScoreMod; 

// Contains code to manage the state of all mod score containers
public static class ModState {
    // Whether modded score or normal score should be displayed
    public static bool ShowModdedScore { get; private set; }
    // The score container to be displayed
    public static ScoreContainer CurrentContainer { get; private set; }

    private static string logFilePath;
    private static ScoreContainer[] scoreContainers;
    private static StringTable outputTable;

    // Creates or clears all score containers
    public static void Initialize(string trackId, int noteCount, int holdCount, int spinCount, int scratchCount) {
        var profiles = ScoreSystemProfile.Profiles;
            
        if (scoreContainers == null) {
            scoreContainers = new ScoreContainer[profiles.Count];

            for (int i = 0; i < scoreContainers.Length; i++)
                scoreContainers[i] = new ScoreContainer(profiles[i]);

            string defaultProfile = ScoreMod.DefaultProfile.Value;
                
            if (int.TryParse(defaultProfile, out int index)) {
                if (index >= 0 && index < profiles.Count)
                    CurrentContainer = scoreContainers[index];
            }
            else {
                for (int i = 0; i < profiles.Count; i++) {
                    if (profiles[i].Name != defaultProfile)
                        continue;

                    CurrentContainer = scoreContainers[i];

                    break;
                }
            }

            if (CurrentContainer == null) {
                CurrentContainer = scoreContainers[0];
                ScoreMod.DefaultProfile.SetSerializedValue("0");
            }
        }
            
        foreach (var container in scoreContainers)
            container.Initialize(trackId, noteCount, holdCount, spinCount, scratchCount);
    }

    // Toggles modded scoring visibility and updates UI
    public static void ToggleModdedScoring() {
        ShowModdedScore = !ShowModdedScore;
        CompleteScreenUI.UpdateUI();
        LevelSelectUI.UpdateUI();
    }

    // Sets the active scoring profile and updates UI
    public static bool PickScoringSystem(int index) {
        if (index >= scoreContainers.Length)
            return false;

        CurrentContainer = scoreContainers[index];
        ShowModdedScore = true;
        CompleteScreenUI.UpdateUI();
        LevelSelectUI.UpdateUI();

        return true;
    }

    public static Accuracy HitNote(int noteIndex, NoteType noteType, float offset) {
        var accuracy = Accuracy.Perfect;
            
        foreach (var container in scoreContainers) {
            var accuracyForContainer = container.HitNote(noteType, offset);

            if (container == CurrentContainer)
                accuracy = accuracyForContainer;
        }

        return accuracy;
    }

    public static void HitSustainedNoteTicks(int noteIndex, int amount) {
            
    }

    public static void ClearNote(int noteIndex) {
        foreach (var container in scoreContainers)
            container.ClearNote(noteIndex);
    }

    // Adds points to all score containers
    public static void AddScore(int amount, float offset, bool isSustainedNoteTick, NoteType noteType, int noteIndex) {
        int oldMultiplier = CurrentContainer.Multiplier;
        bool oldIsPfc = CurrentContainer.GetIsPfc(false);
        bool oldIsSPlus = CurrentContainer.GetIsSPlus();

        // Process sustained note ticks and regular note hits differently
        if (isSustainedNoteTick) {
            foreach (var container in scoreContainers) {
                container.AddFlatScore(amount);
            }
        }
        else {
            switch (noteType) {
                // Use modded match point value for match notes
                case NoteType.Match: {
                    foreach (var container in scoreContainers)
                        container.AddScoreFromNoteType(NoteType.Match, 0f);

                    break;
                }
                // Only use timing offset for timed note types
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    foreach (var container in scoreContainers) {
                        var accuracyForContainer = container.AddScoreFromNoteType(noteType, offset);
                    }

                    break;
                // Use regular point values for spins and scratches
                default:
                    foreach (var container in scoreContainers)
                        container.AddFlatScore(amount);

                    break;
            }
        }

        if (!ShowModdedScore)
            return;
    }

    // Adds points to the max possible score, recording the added points in the score containers' max score history
    public static void AddMaxScore(int amount, bool isSustainedNoteTick, NoteType noteType, int noteIndex) {
        // Process sustained note ticks and regular note hits differently
        if (isSustainedNoteTick) {
            foreach (var container in scoreContainers)
                container.AddFlatMaxScore(amount, noteIndex, true);
        }
        else {
            switch (noteType) {
                // Use modded match point value for match notes and timed notes
                case NoteType.Match:
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    foreach (var container in scoreContainers)
                        container.AddMaxScoreFromNoteType(noteType, noteIndex);

                    break;
                // Use regular point values for spins and scratches
                default:
                    foreach (var container in scoreContainers)
                        container.AddFlatMaxScore(amount, noteIndex, false);

                    break;
            }
        }
    }

    // Mark a note as missed
    public static void Miss(int noteIndex, bool countMiss, bool trackMiss) {
        AddMiss(noteIndex, countMiss, trackMiss);
    }

    // Mark the remaining ticks of a sustained note as missed
    public static void MissRemainingNoteTicks(int noteIndex) {
    }
        
    // Miss the release note that is linked to the given start note
    public static void MissReleaseNoteFromStart(NoteType startNoteType, int startNoteIndex) {
        
    }

    // Reset the multiplier of all score containers
    public static void ResetMultiplier() {
        foreach (var container in scoreContainers)
            container.ResetMultiplier();
    }

    // Lose the PFC state for all score containers
    public static void PfcLost() {
        foreach (var container in scoreContainers)
            container.PfcLost();
    }

    // Write detailed score data to the console and to a file
    public static void LogPlayData(string trackName, bool logDiscrepancies) {
        if (!TryGetLogFilePath())
            return;
            
        if (outputTable == null) {
            outputTable = new StringTable(21, scoreContainers.Length + 1);

            outputTable.SetHeader(
                "Profile",
                "Score",
                string.Empty,
                "Best",
                string.Empty,
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
                $"+{container.SuperPerfects}",
                container.HighScore.ToString(),
                $"+{container.HighScoreSuperPerfects}",
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
            writer.Flush();
        }
            
        if (!logDiscrepancies)
            return;

        if (CurrentContainer.MaxScoreSoFar != CurrentContainer.MaxScore) {
            ScoreMod.Logger.LogWarning("WARNING: Some discrepancies were found during score prediction");
            ScoreMod.Logger.LogMessage("");
        }

        bool any = false;

        // foreach (string s in CurrentContainer.GetMaxScoreSoFarUnchecked()) {
        //     any = true;
        //         
        //     if (first) {
        //         ScoreMod.Logger.LogWarning("WARNING: Some discrepancies were found during score prediction");
        //         ScoreMod.Logger.LogMessage("");
        //         first = false;
        //     }
        //         
        //     ScoreMod.Logger.LogWarning(s);
        // }
            
        if (any)
            ScoreMod.Logger.LogMessage("");
    }

    // Check and save new high scores
    public static void SavePlayData(string trackId) {
        bool anyChanged = false;
            
        foreach (var container in scoreContainers) {
            if (HighScoresContainer.TrySetHighScore(trackId, container))
                anyChanged = true;
        }
            
        if (anyChanged)
            HighScoresContainer.SaveHighScores();
    }

    // Adds a single miss to all miss counters, making sure that a given note never gets counted twice
    private static void AddMiss(int noteIndex, bool countMiss, bool trackMiss) {
            
        if (!countMiss)
            return;

        foreach (var container in scoreContainers)
            container.AddMiss();
    }

    // Logs a string to both the console and a file
    private static void LogToFile(StreamWriter writer, string text) {
        ScoreMod.Logger.LogMessage(text);
        writer.WriteLine(text);
    }
    private static void LogToFile(StreamWriter writer) {
        ScoreMod.Logger.LogMessage("");
        writer.WriteLine();
    }
        
    // Try to get the path of the logging file
    private static bool TryGetLogFilePath() {
        if (!string.IsNullOrWhiteSpace(logFilePath))
            return true;

        if (!ScoreMod.TryGetFileDirectory(out string fileDirectory))
            return false;

        logFilePath = Path.Combine(fileDirectory, "History.txt");

        return true;
    }
}