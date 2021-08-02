using System;
using System.Collections.Generic;
using System.IO;

namespace ScoreMod {
    public class HighScoresContainer {
        private const uint HASH_BIAS = 2166136261u;
        private const int HASH_COEFF = 486187739;
        
        private static Dictionary<string, HighScoreItem> highScores;

        public static void LoadHighScores() {
            highScores = new Dictionary<string, HighScoreItem>();
            
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"ScoreMod HighScores.txt");
            
            if (!File.Exists(path))
                return;

            using (var reader = new StreamReader(path)) {
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine();
                    
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    
                    var split = line.Split(' ');
                    
                    if (split.Length < 4 || !int.TryParse(split[1], out int score))
                        continue;

                    string id = split[0];
                    var newItem = new HighScoreItem(id, score, split[2]);
                    
                    if (newItem.SecurityKey == split[3])
                        highScores.Add(id, newItem);
                }
            }
        }

        public static void SaveHighScores() {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"ScoreMod HighScores.txt");

            using (var writer = new StreamWriter(path)) {
                foreach (var pair in highScores) {
                    var item = pair.Value;
                    
                    writer.WriteLine($"{pair.Key} {item.Score} {item.Rank} {item.SecurityKey}");
                }
            }
        }

        public static bool SetHighScore(string trackId, string scoreProfileId, int score, string rank) {
            string id = $"{trackId}_{scoreProfileId}";

            if (score <= highScores[id].Score)
                return false;

            highScores[id] = new HighScoreItem(id, score, rank);

            return true;

        }
        
        public static int GetHighScore(string trackId, string scoreProfileId, out string rank) {
            if (highScores.TryGetValue($"{trackId}_{scoreProfileId}", out var item)) {
                rank = item.Rank;

                return item.Score;
            }

            rank = "D";

            return 0;
        }

        private readonly struct HighScoreItem {
            public int Score { get; }
            public string Rank { get; }
            public string SecurityKey { get; }

            public HighScoreItem(string id, int score, string rank) {
                Score = score;
                Rank = rank;
                
                unchecked {
                    int hash = (int) HASH_BIAS * HASH_COEFF ^ score.GetHashCode();

                    hash = hash * HASH_COEFF ^ rank.GetHashCode();
                    
                    SecurityKey = ((uint) (hash * HASH_COEFF ^ id.GetHashCode())).ToString("x8");
                }
            }
        }
    }
}