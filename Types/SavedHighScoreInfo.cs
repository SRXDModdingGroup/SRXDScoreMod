using SMU.Utilities;

namespace SRXDScoreMod;

internal class SavedHighScoreInfo {
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int MaxStreak { get; }
    public int SecondaryScore { get; }
    public uint ModifierFlags { get; }
    public int ModifierMultiplierHash { get; }
    public string Hash { get; }
            
    public SavedHighScoreInfo(string key, int score, int streak, int maxScore, int maxStreak, int secondaryScore, uint modifierFlags, int modifierMultiplierHash)
        : this(score, streak, maxScore, maxStreak, secondaryScore) {
        ModifierFlags = modifierFlags;
        ModifierMultiplierHash = modifierMultiplierHash;

        unchecked {
            Hash = ((uint) HashUtility.Combine(key, score, streak, maxScore, maxStreak, secondaryScore, (int) modifierFlags, modifierMultiplierHash)).ToString("x8");
        }
    }

    public SavedHighScoreInfo(string key, int score, int streak, int maxScore, int maxStreak, int secondaryScore, ModifierSet modifierSet)
        : this(score, streak, maxScore, maxStreak, secondaryScore) {
        if (modifierSet == null) {
            ModifierFlags = 0u;
            ModifierMultiplierHash = HashUtility.GetStableHash(1f);
        }
        else {
            ModifierFlags = modifierSet.GetActiveModifierFlags();
            ModifierMultiplierHash = HashUtility.GetStableHash(modifierSet.GetOverallMultiplier());
        }
        
        unchecked {
            Hash = ((uint) HashUtility.Combine(key, score, streak, maxScore, maxStreak, secondaryScore, (int) ModifierFlags, ModifierMultiplierHash)).ToString("x8");
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