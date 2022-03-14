using SMU.Utilities;

namespace SRXDScoreMod;

internal class SavedHighScoreInfo {
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int MaxStreak { get; }
    public int SecondaryScore { get; }
    public uint ModifierFlags { get; }
    public int ModifierMultiplier { get; }
    public string Hash { get; }
            
    public SavedHighScoreInfo(string key, int score, int streak, int maxScore, int maxStreak, int secondaryScore, uint modifierFlags, int modifierMultiplier)
        : this(score, streak, maxScore, maxStreak, secondaryScore) {
        ModifierFlags = modifierFlags;
        ModifierMultiplier = modifierMultiplier;

        unchecked {
            Hash = ((uint) HashUtility.Combine(key, score, streak, maxScore, maxStreak, secondaryScore, (int) modifierFlags, modifierMultiplier)).ToString("x8");
        }
    }

    public SavedHighScoreInfo(string key, int score, int streak, int maxScore, int maxStreak, int secondaryScore, ScoreModifierSet modifierSet)
        : this(score, streak, maxScore, maxStreak, secondaryScore) {
        if (modifierSet == null) {
            ModifierFlags = 0u;
            ModifierMultiplier = 100;
        }
        else {
            ModifierFlags = modifierSet.GetActiveModifierFlags();
            ModifierMultiplier = modifierSet.GetOverallMultiplier();
        }
        
        unchecked {
            Hash = ((uint) HashUtility.Combine(key, score, streak, maxScore, maxStreak, secondaryScore, (int) ModifierFlags, ModifierMultiplier)).ToString("x8");
        }
    }

    private SavedHighScoreInfo(int score, int streak, int maxScore, int maxStreak, int secondaryScore) {
        Score = score;
        Streak = streak;
        MaxScore = maxScore;
        MaxStreak = maxStreak;
        SecondaryScore = secondaryScore;
    }
}