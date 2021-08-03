using System.Collections.ObjectModel;
using static ScoreMod.ScoreContainer;

namespace ScoreMod {
    public class ScoreSystemProfile {
        private const uint HASH_BIAS = 2166136261u;
        private const int HASH_COEFF = 486187739;

        public static ReadOnlyCollection<ScoreSystemProfile> Profiles { get; } = new ReadOnlyCollection<ScoreSystemProfile>(new[] {
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
        
        public string Name { get; }
        public int MaxMultiplier { get; }
        public int PointsPerMultiplier { get; }
        public int MatchNoteValue { get; }
        public ReadOnlyCollection<TimedNoteWindow> PressNoteWindows { get; }
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

        public class TimedNoteWindow {
            public Accuracy Accuracy { get; }
            public int MaxValue { get; }
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