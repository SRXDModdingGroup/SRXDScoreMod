using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SRXDScoreMod; 

// Contains a set of modded high scores and manages the high score file
internal class HighScoresContainer {
    private const uint HASH_BIAS = 2166136261u;
    private const int HASH_COEFF = 486187739;

    private static readonly bool SAVE_HIGH_SCORES = true;
    private static readonly Regex MATCH_BASE_ID = new(@"(.+?)_Stats");
    private static readonly Regex MATCH_CUSTOM_ID = new(@"CUSTOM_(.+?)_(\-?\d+)_Stats");
    private static readonly HashSet<string> FORBIDDEN_NAMES = new() {
        "CreateCustomTrack_Stats",
        "Tutorial XD",
        "RandomizeTrack_Stats"
    };

    private static string filePath;
    private static string lastTrackId;
    private static string lastStatString;
    private static TrackData.DifficultyType lastDifficulty;
    private static Dictionary<string, HighScoreItem> highScores;

    public static void LoadHighScores() {
        highScores = new Dictionary<string, HighScoreItem>();
            
        if (!TryGetFilePath() || !File.Exists(filePath))
            return;

        using (var reader = new StreamReader(filePath)) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                    
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var split = line.Split(' ');
                    
                if (split.Length < 5
                    || !int.TryParse(split[1], out int score)
                    || !int.TryParse(split[2], out int maxScore)
                    || !int.TryParse(split[3], out int superPerfectCount))
                    continue;

                string id = split[0];
                var newItem = new HighScoreItem(id, score, maxScore, superPerfectCount);
                    
                if (newItem.Hash == split[4])
                    highScores.Add(id, newItem);
            }
        }
    }

    public static void SaveHighScores() {
        if (!SAVE_HIGH_SCORES || !TryGetFilePath())
            return;

        using (var writer = new StreamWriter(filePath)) {
            foreach (var pair in highScores) {
                var item = pair.Value;
                    
                writer.WriteLine($"{pair.Key} {item.Score} {item.MaxScore} {item.SuperPerfectCount} {item.Hash}");
            }
        }
    }

    public static bool TrySetHighScore(string trackId, ScoreContainer container) {
        var profile = container.Profile;
        string id = $"{trackId}_{profile.GetUniqueId()}";
        int score = container.Score;
        int maxScore = container.MaxScore;
        int superPerfects = container.SuperPerfects;

        if (highScores.TryGetValue(id, out var item)) {
            if (maxScore != item.MaxScore) {
                ScoreMod.Logger.LogWarning($"WARNING: Max Score for profile \"{profile.Name}\" does not match saved Max Score. Score will not be saved");

                return false;
            }
                
            if (score > maxScore) {
                ScoreMod.Logger.LogWarning($"WARNING: Score for profile \"{profile.Name}\" is greater than saved Max Score. Score will not be saved");

                return false;
            }

            if (score < item.Score || score == item.Score && superPerfects <= item.SuperPerfectCount)
                return false;
        }

        highScores[id] = new HighScoreItem(id, score, maxScore, superPerfects);

        return true;

    }

    public static int GetHighScore(string trackId, string scoreProfileId, out int superPerfectCount, out string rank) {
        if (highScores.TryGetValue($"{trackId}_{scoreProfileId}", out var item)) {
            superPerfectCount = item.SuperPerfectCount;
            rank = ScoreContainer.GetRank(item.Score, item.MaxScore);
                
            return item.Score;
        }

        superPerfectCount = 0;
        rank = "-";

        return 0;
    }

    public static string GetTrackId(PlayableTrackData trackData) {
        string statString = trackData.TrackInfoRef.StatsUniqueString;
        var difficulty = trackData.Difficulty;
        
        if (statString == lastStatString && difficulty == lastDifficulty)
            return lastTrackId;

        lastStatString = statString;
        lastDifficulty = difficulty;
            
        if (FORBIDDEN_NAMES.Contains(statString))
            return lastTrackId = string.Empty;
            
        var match = MATCH_CUSTOM_ID.Match(statString);
            
        if (match.Success) {
            var groups = match.Groups;
            uint fileHash;

            unchecked {
                fileHash = (uint) int.Parse(groups[2].Value);
            }

            return lastTrackId = $"{groups[1].Value.Replace(' ', '_')}_{fileHash:x8}_{difficulty}";
        }

        match = MATCH_BASE_ID.Match(statString);

        if (match.Success)
            return lastTrackId = $"{match.Groups[1].Value.Replace(' ', '_')}_{difficulty}";

        return lastTrackId = string.Empty;
    }

    private static bool TryGetFilePath() {
        if (!string.IsNullOrWhiteSpace(filePath))
            return true;

        if (!ScoreMod.TryGetFileDirectory(out string fileDirectory))
            return false;

        filePath = Path.Combine(fileDirectory, "Highscores.txt");

        return true;
    }

    private readonly struct HighScoreItem {
        public int Score { get; }
        public int MaxScore { get; }
        public int SuperPerfectCount { get; }
        public string Hash { get; }
            
        public HighScoreItem(string id, int score, int maxScore, int superPerfectCount) {
            Score = score;
            MaxScore = maxScore;
            SuperPerfectCount = superPerfectCount;
                
            unchecked {
                int hash = (int) HASH_BIAS * HASH_COEFF ^ score.GetHashCode();

                hash = hash * HASH_COEFF ^ maxScore.GetHashCode();
                hash = hash * HASH_COEFF ^ superPerfectCount.GetHashCode();
                    
                Hash = ((uint) (hash * HASH_COEFF ^ id.GetHashCode())).ToString("x8");
            }
        }
    }
}