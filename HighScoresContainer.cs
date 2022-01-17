using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SRXDScoreMod; 

// Contains a set of modded high scores and manages the high score file
internal static class HighScoresContainer {
    private static readonly bool SAVE_HIGH_SCORES = true;
    private static readonly Regex MATCH_CUSTOM_ID = new(@"CUSTOM_(.+?)_(\-?\d+)");
    private static readonly HashSet<string> FORBIDDEN_NAMES = new() {
        "CreateCustomTrack",
        "Tutorial XD",
        "RandomizeTrack"
    };

    private static string filePath;
    private static Dictionary<string, SavedHighScoreInfo> highScores;

    public static void LoadHighScores() {
        highScores = new Dictionary<string, SavedHighScoreInfo>();
            
        if (!TryGetFilePath() || !File.Exists(filePath))
            return;

        using var reader = new StreamReader(filePath);
        
        while (!reader.EndOfStream) {
            string line = reader.ReadLine();
                    
            if (string.IsNullOrWhiteSpace(line))
                continue;
                    
            string[] split = line.Split(' ');
                    
            if (split.Length < 7
                || !int.TryParse(split[1], out int score)
                || !int.TryParse(split[2], out int streak)
                || !int.TryParse(split[3], out int maxScore)
                || !int.TryParse(split[4], out int maxStreak)
                || !int.TryParse(split[5], out int secondaryScore))
                continue;

            string key = split[0];
            var newInfo = new SavedHighScoreInfo(key, score, streak, maxScore, maxStreak, secondaryScore);
                    
            if (newInfo.Hash == split[6])
                highScores.Add(key, newInfo);
        }
    }

    public static void SaveHighScores() {
        if (!SAVE_HIGH_SCORES || !TryGetFilePath())
            return;

        using var writer = new StreamWriter(filePath);
        
        foreach (var pair in highScores) {
            var item = pair.Value;
                    
            writer.WriteLine($"{pair.Key} {item.Score} {item.Streak} {item.MaxScore} {item.MaxStreak} {item.SecondaryScore} {item.Hash}");
        }
    }

    public static bool TrySetHighScore(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType,
        string scoreSystemId, string modifierSetId, SavedHighScoreInfo info) {
        string key = $"{GetTrackId(trackInfoRef, difficultyType)}_{scoreSystemId}";

        if (!string.IsNullOrWhiteSpace(modifierSetId))
            key = $"{key}_{modifierSetId}";

        if (!highScores.TryGetValue(key, out var oldInfo)) {
            highScores[key] = info;

            return true;
        }

        if (info.MaxScore != oldInfo.MaxScore || info.MaxStreak != oldInfo.MaxStreak) {
            ScoreMod.Logger.LogWarning($"WARNING: Max Score for \"{key}\" does not match saved Max Score. Score will not be saved");

            return false;
        }
            
        int score = oldInfo.Score;
        int streak = oldInfo.Streak;
                
        if (score > oldInfo.MaxScore || streak > oldInfo.MaxStreak) {
            ScoreMod.Logger.LogWarning($"WARNING: Score for for \"{key}\" is greater than saved Max Score. Score will not be saved");

            return false;
        }
            
        int secondaryScore = oldInfo.SecondaryScore;
        bool isNewBest = false;
        bool anyChanged = false;

        if (info.Score > score || info.Score == oldInfo.Score && info.SecondaryScore > secondaryScore) {
            score = info.Score;
            secondaryScore = info.SecondaryScore;
            isNewBest = true;
            anyChanged = true;
        }

        if (info.Streak > streak) {
            streak = info.Streak;
            anyChanged = true;
        }

        if (anyChanged)
            highScores[key] = new SavedHighScoreInfo(key, score, streak, info.MaxScore, info.MaxStreak, secondaryScore);

        return isNewBest;
    }

    public static SavedHighScoreInfo GetHighScore(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType,
        string scoreSystemId, string modifierSetId) {
        string key = $"{GetTrackId(trackInfoRef, difficultyType)}_{scoreSystemId}";

        if (!string.IsNullOrWhiteSpace(modifierSetId))
            key = $"{key}_{modifierSetId}";

        if (highScores.TryGetValue(key, out var item))
            return item;

        return new SavedHighScoreInfo(string.Empty, 0, 0, 0, 0, 0);
    }

    private static bool TryGetFilePath() {
        if (!string.IsNullOrWhiteSpace(filePath))
            return true;

        if (!ScoreMod.TryGetFileDirectory(out string fileDirectory))
            return false;

        filePath = Path.Combine(fileDirectory, "Highscores.txt");

        return true;
    }

    private static string GetTrackId(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType) {
        string uniqueName = trackInfoRef.UniqueName;

        if (FORBIDDEN_NAMES.Contains(uniqueName))
            return string.Empty;
        
        var match = MATCH_CUSTOM_ID.Match(uniqueName);

        if (!match.Success)
            return $"{uniqueName.Replace(' ', '_')}_{difficultyType}";
        
        var groups = match.Groups;
        uint fileHash;

        unchecked {
            fileHash = (uint) int.Parse(groups[2].Value);
        }

        return $"{groups[1].Value.Replace(' ', '_')}_{fileHash:x8}_{difficultyType}";
    }
}