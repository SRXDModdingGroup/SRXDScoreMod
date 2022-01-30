using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SMU.Utilities;

namespace SRXDScoreMod; 

// Contains a set of modded high scores and manages the high score file
internal static class HighScoresContainer {
    private static readonly bool SAVE_HIGH_SCORES = true;

    private static bool anyUnsaved;
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
                    
            if (split.Length < 9
                || !int.TryParse(split[1], out int score)
                || !int.TryParse(split[2], out int streak)
                || !int.TryParse(split[3], out int maxScore)
                || !int.TryParse(split[4], out int maxStreak)
                || !int.TryParse(split[5], out int secondaryScore)
                || !uint.TryParse(split[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint modifierFlags)
                || !int.TryParse(split[7], out int modifierMultiplier))
                continue;

            string key = split[0];
            var newInfo = new SavedHighScoreInfo(key, score, streak, maxScore, maxStreak, secondaryScore, modifierFlags, modifierMultiplier);

            if (newInfo.Hash == split[8])
                highScores.Add(key, newInfo);
            else
                ScoreMod.Logger.LogWarning($"WARNING: Did not load score {key}");
        }
    }

    public static void SaveHighScores() {
        if (!SAVE_HIGH_SCORES || !TryGetFilePath() || !anyUnsaved)
            return;

        using var writer = new StreamWriter(filePath);
        
        foreach (var pair in highScores) {
            var item = pair.Value;

            writer.WriteLine($"{pair.Key} {item.Score} {item.Streak} {item.MaxScore} {item.MaxStreak} {item.SecondaryScore} {item.ModifierFlags:x8} {item.ModifierMultiplier} {item.Hash}");
        }
    }

    public static void RemoveInvalidHighScoresForModifierSet(ModifierSet modifierSet) {
        if (highScores.Count == 0)
            return;
        
        string[] keys = new string[highScores.Count];
        
        highScores.Keys.CopyTo(keys, 0);

        foreach (string key in keys) {
            var info = highScores[key];
            uint flags = info.ModifierFlags;

            if (info.ModifierMultiplier == modifierSet.GetMultiplierGivenActiveFlags(flags)
                && !modifierSet.GetBlocksSubmissionGivenActiveFlags(flags))
                continue;
            
            highScores.Remove(key);
            ScoreMod.Logger.LogWarning($"WARNING: Removed score {key}");
        }
    }

    public static bool TrySetHighScore(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType,
        IScoreSystem scoreSystem, ModifierSet modifierSet, SavedHighScoreInfo info) {
        if (modifierSet != null && modifierSet.GetAnyBlocksSubmission())
            return false;
        
        string key = GetKey(trackInfoRef, difficultyType, scoreSystem, modifierSet);

        if (!highScores.TryGetValue(key, out var oldInfo)) {
            highScores.Add(key, new SavedHighScoreInfo(key, info.Score, info.Streak, info.MaxScore, info.MaxStreak, info.SecondaryScore, modifierSet));
            anyUnsaved = true;

            return true;
        }

        if (info.MaxScore != oldInfo.MaxScore || info.MaxStreak != oldInfo.MaxStreak) {
            ScoreMod.Logger.LogWarning($"WARNING: Max Score for \"{key}\" does not match saved Max Score. Score will not be saved");

            return false;
        }
            
        int score = oldInfo.Score;
        int streak = oldInfo.Streak;
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

        if (!anyChanged)
            return false;
        
        highScores[key] = new SavedHighScoreInfo(key, score, streak, info.MaxScore, info.MaxStreak, secondaryScore, modifierSet);
        anyUnsaved = true;

        return isNewBest;
    }

    public static SavedHighScoreInfo GetHighScore(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType,
        IScoreSystem scoreSystem, ModifierSet modifierSet) {
        string key = GetKey(trackInfoRef, difficultyType, scoreSystem, modifierSet);

        if (highScores.TryGetValue(key, out var info))
            return info;

        return new SavedHighScoreInfo(string.Empty, 0, 0, 0, 0, 0, modifierSet);
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
        if (!trackInfoRef.IsCustomFile)
            return $"{trackInfoRef.AssetName.Replace(' ', '_')}_{difficultyType}";
        
        var customFile = trackInfoRef.customFile;

        unchecked {
            return $"CUSTOM_{customFile.FileNameNoExtension.Replace(' ', '_')}_{(uint) customFile.FileHash:x8}_{difficultyType}";
        }
    }

    private static string GetKey(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType, IScoreSystem scoreSystem, ModifierSet modifierSet) {
        if (modifierSet != null && modifierSet.GetAnyEnabled())
            return $"{GetTrackId(trackInfoRef, difficultyType)}_{scoreSystem.Key}_{modifierSet.Id}";
        
        return $"{GetTrackId(trackInfoRef, difficultyType)}_{scoreSystem.Key}";
    }
}