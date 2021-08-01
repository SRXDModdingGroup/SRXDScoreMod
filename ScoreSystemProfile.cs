using System.Collections.ObjectModel;
using static ScoreMod.ScoreContainer;

namespace ScoreMod {
    public class ScoreSystemProfile {
        public static ReadOnlyCollection<ScoreSystemProfile> Profiles { get; } = new ReadOnlyCollection<ScoreSystemProfile>(new[] {
            new ScoreSystemProfile("Standard", 4, 32, 4, 12,
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
            new ScoreSystemProfile("Steep Good", 4, 32, 4, 12,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 14, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 12, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 2, 0.07f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 10, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 2, 0.08f)
                }),
            new ScoreSystemProfile("Steeper Good", 4, 32, 4, 12,
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
            new ScoreSystemProfile("Steep Good (PPM 16)", 4, 16, 4, 12,
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 16, 0f),
                    new TimedNoteWindow(Accuracy.Great, 15, 0.035f),
                    new TimedNoteWindow(Accuracy.Great, 14, 0.0425f),
                    new TimedNoteWindow(Accuracy.Good, 12, 0.05f),
                    new TimedNoteWindow(Accuracy.Okay, 2, 0.07f)
                },
                new [] {
                    new TimedNoteWindow(Accuracy.Perfect, 12, 0f),
                    new TimedNoteWindow(Accuracy.Great, 11, 0.05f),
                    new TimedNoteWindow(Accuracy.Great, 10, 0.0575f),
                    new TimedNoteWindow(Accuracy.Good, 8, 0.065f),
                    new TimedNoteWindow(Accuracy.Okay, 2, 0.08f)
                }),
            new ScoreSystemProfile("Steeper Good (PPM 16)", 4, 16, 4, 12,
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
        });
        
        public string Name { get; }
        public int MaxMultiplier { get; }
        public int PointsPerMultiplier { get; }
        public int MatchNoteValue { get; }
        public int SpinStartValue { get; }
        public ReadOnlyCollection<TimedNoteWindow> PressNoteWindows { get; }
        public ReadOnlyCollection<TimedNoteWindow> ReleaseNoteWindows { get; }

        public ScoreSystemProfile(string name, int maxMultiplier, int pointsPerMultiplier, int matchNoteValue, int spinStartValue, TimedNoteWindow[] pressNoteWindows, TimedNoteWindow[] releaseNoteWindows) {
            Name = name;
            MaxMultiplier = maxMultiplier;
            PointsPerMultiplier = pointsPerMultiplier;
            MatchNoteValue = matchNoteValue;
            SpinStartValue = spinStartValue;
            PressNoteWindows = new ReadOnlyCollection<TimedNoteWindow>(pressNoteWindows);
            ReleaseNoteWindows = new ReadOnlyCollection<TimedNoteWindow>(releaseNoteWindows);
        }

        public class TimedNoteWindow {
            public Accuracy Accuracy { get; }
            public int MaxValue { get; }
            public float LowerBound { get; }

            public TimedNoteWindow(Accuracy accuracy, int maxValue, float lowerBound) {
                Accuracy = accuracy;
                MaxValue = maxValue;
                LowerBound = lowerBound;
            }
        }
    }
}