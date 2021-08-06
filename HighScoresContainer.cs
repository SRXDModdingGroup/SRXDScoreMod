using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ScoreMod {
    public class HighScoresContainer {
        private const uint HASH_BIAS = 2166136261u;
        private const int HASH_COEFF = 486187739;

        private static readonly bool SAVE_HIGH_SCORES = true;

        private static string filePath;
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
                    
                    if (split.Length < 4 || !int.TryParse(split[1], out int score) || !int.TryParse(split[2], out int maxScore))
                        continue;

                    string id = split[0];
                    var newItem = new HighScoreItem(id, score, maxScore);
                    
                    if (newItem.SecurityKey == split[3])
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
                    
                    writer.WriteLine($"{pair.Key} {item.Score} {item.MaxScore} {item.SecurityKey}");
                }
            }
        }

        public static bool IsNewBest(string trackId, ScoreContainer container) => !highScores.TryGetValue($"{trackId}_{container.Profile.GetUniqueId()}", out var item) || container.Score > item.Score;

        public static bool TrySetHighScore(string trackId, ScoreContainer container) {
            var profile = container.Profile;
            string id = $"{trackId}_{profile.GetUniqueId()}";
            int score = container.Score;
            int maxScore = container.MaxScore;

            if (highScores.TryGetValue(id, out var item)) {
                if (maxScore != item.MaxScore) {
                    Main.Logger.LogWarning($"WARNING: Max Score for profile \"{profile.Name}\" does not match saved Max Score. Score will not be saved");

                    return false;
                }
                
                if (score > maxScore) {
                    Main.Logger.LogWarning($"WARNING: Score for profile \"{profile.Name}\" is greater than saved Max Score. Score will not be saved");

                    return false;
                }

                if (score <= item.Score)
                    return false;
            }

            highScores[id] = new HighScoreItem(id, score, maxScore);

            return true;

        }

        public static int GetHighScore(string trackId, string scoreProfileId, out string rank) {
            if (highScores.TryGetValue($"{trackId}_{scoreProfileId}", out var item)) {
                rank = ScoreContainer.GetRank(item.Score, item.MaxScore);

                return item.Score;
            }

            rank = "D";

            return 0;
        }

        private static bool TryGetFilePath() {
            if (!string.IsNullOrWhiteSpace(filePath))
                return true;

            if (!Main.TryGetFileDirectory(out string fileDirectory))
                return false;

            filePath = Path.Combine(fileDirectory, "Highscores.txt");

            return true;
        }

        private readonly struct HighScoreItem {
            public int Score { get; }
            public int MaxScore { get; }
            public string SecurityKey { get; }
            
            public HighScoreItem(string id, int score, int maxScore) {
                Score = score;
                MaxScore = maxScore;
                
                unchecked {
                    int hash = (int) HASH_BIAS * HASH_COEFF ^ score.GetHashCode();

                    hash = hash * HASH_COEFF ^ maxScore.GetHashCode();
                    
                    SecurityKey = ((uint) (hash * HASH_COEFF ^ id.GetHashCode())).ToString("x8");
                }
            }
        }
    }
}