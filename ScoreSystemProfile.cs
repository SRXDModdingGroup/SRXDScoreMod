using System.Collections.ObjectModel;

namespace ScoreMod {
    // Contains information about note point values, timing windows, and points per multiplier for each score container
    public class ScoreSystemProfile {
        private const uint HASH_BIAS = 2166136261u;
        private const int HASH_COEFF = 486187739;

        public static ReadOnlyCollection<ScoreSystemProfile> Profiles { get; } = new ReadOnlyCollection<ScoreSystemProfile>(new[] {
            // Default and recommended score profile. Low cost Greats, high cost Goods and Okays, and a very fast building multiplier
            new ScoreSystemProfile("Lenient (PPM 16)", 4, 16, 4,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 14, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 12, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 1, 0.065f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 10, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 1, 0.075f)
                }),
            // Same as above, but with a much slower building multiplier
            new ScoreSystemProfile("Lenient (PPM 32)", 4, 32, 4,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 14, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 12, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 1, 0.065f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 10, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 1, 0.075f)
                }),
            // Less lenient, likely obsolete score profile. Has less costly Goods and Okays, but much more costly Greats
            new ScoreSystemProfile("Strict (PPM 16)", 4, 16, 4,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 13, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 4, 0.07f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 9, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 6, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 3, 0.08f)
                }),
            // Same as above, but with a much slower building multiplier
            new ScoreSystemProfile("Strict (PPM 32)", 4, 32, 4,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 13, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 4, 0.07f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 9, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 6, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 3, 0.08f)
                })
        });
        
        // The name of the score profile
        public string Name { get; }
        // The maximum multiplier for the profile
        public int MaxMultiplier { get; }
        // The amount of (unscaled) points needed to gain one multiplier
        public int PointsPerMultiplier { get; }
        // The point value of a match note
        public int MatchNoteValue { get; }
        // The accuracy type, point value, and lower bounds for all timing windows for press notes
        public ReadOnlyCollection<TimedNoteWindow> PressNoteWindows { get; }
        // The accuracy type, point value, and lower bounds for all timing windows for release notes
        public ReadOnlyCollection<TimedNoteWindow> ReleaseNoteWindows { get; }

        private readonly int hash;
        private readonly string uniqueId;

        public string GetUniqueId() => uniqueId;

        private ScoreSystemProfile(string name, int maxMultiplier, int pointsPerMultiplier, int matchNoteValue, TimedNoteWindow[] pressNoteWindows, TimedNoteWindow[] releaseNoteWindows) {
            Name = name;
            MaxMultiplier = maxMultiplier;
            PointsPerMultiplier = pointsPerMultiplier;
            MatchNoteValue = matchNoteValue;
            PressNoteWindows = new ReadOnlyCollection<TimedNoteWindow>(pressNoteWindows);
            ReleaseNoteWindows = new ReadOnlyCollection<TimedNoteWindow>(releaseNoteWindows);
            
            unchecked {
                hash = (int) HASH_BIAS * HASH_COEFF ^ MaxMultiplier.GetHashCode();
                hash = hash * HASH_COEFF ^ PointsPerMultiplier.GetHashCode();
                hash = hash * HASH_COEFF ^ MatchNoteValue.GetHashCode();

                foreach (var window in PressNoteWindows)
                    hash = hash * HASH_COEFF ^ window.GetHashCode();
                
                foreach (var window in ReleaseNoteWindows)
                    hash = hash * HASH_COEFF ^ window.GetHashCode();
                
                uniqueId = ((uint) hash).ToString("x8");
            }
        }

        public override int GetHashCode() => hash;

        // Stores data about a single timing window
        public class TimedNoteWindow {
            // The accuracy type of this window
            public Accuracy Accuracy { get; }
            // The maximum points to be gained at the lower bound of this window
            public int MaxValue { get; }
            // The minimum timing offset that falls within this window
            public float LowerBound { get; }

            private readonly int hash;

            public TimedNoteWindow(Accuracy accuracy, int maxValue, float lowerBound) {
                Accuracy = accuracy;
                MaxValue = maxValue;
                LowerBound = lowerBound;
                
                unchecked {
                    hash = (int) HASH_BIAS * HASH_COEFF ^ Accuracy.GetHashCode();
                    hash = hash * HASH_COEFF ^ MaxValue.GetHashCode();
                    hash = hash * HASH_COEFF ^ LowerBound.GetHashCode();
                }
            }

            public override int GetHashCode() => hash;
        }
    }
}