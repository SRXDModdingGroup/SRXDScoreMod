using System;
using System.Collections.Generic;

namespace ScoreMod {
    public class ScoreContainer {
        // public static void Main() {
        //     foreach (var window in PRESS_NOTE_WINDOWS)
        //         Console.WriteLine($"{window.Accuracy}: {window.LowerBound} {window.MaxValue} {GetValueFromTiming(window.LowerBound, PRESS_NOTE_WINDOWS, out var accuracy)} {accuracy}");
        // }
        
        private static readonly List<KeyValuePair<float, string>> RANKS = new List<KeyValuePair<float, string>> {
            new KeyValuePair<float, string>(0.98f, "S"),
            new KeyValuePair<float, string>(0.965f, "A+"),
            new KeyValuePair<float, string>(0.95f, "A"),
            new KeyValuePair<float, string>(0.925f, "B+"),
            new KeyValuePair<float, string>(0.90f, "B"),
            new KeyValuePair<float, string>(0.85f, "C+"),
            new KeyValuePair<float, string>(0.80f, "C"),
            new KeyValuePair<float, string>(0.75f, "D+")
        };

        public enum PointSource {
            Match,
            Tap,
            HoldStart,
            Liftoff,
            Beat,
            BeatRelease,
            SpinStart,
            SustainedNoteTick
        }
        
        public enum Accuracy {
            Perfect,
            Great,
            Good,
            Okay,
            Miss
        }

        public ScoreSystemProfile Profile { get; }
        public int Score { get; private set; }
        public int Multiplier { get; private set; }
        public int MaxScore { get; private set; }

        private int pointsToNextMultiplier;
        private int timedNoteScore;
        private int potentialTimedNoteScore;
        private int earlies;
        private int lates;
        private bool isPfc;
        private bool calculatingMaxScore;
        private string rank;
        private Dictionary<Accuracy, int> accuracyCounters;
        private Dictionary<Accuracy, int> detailedLossToAccuracy;

        public ScoreContainer(ScoreSystemProfile profile) {
            Profile = profile;
            Multiplier = Profile.MaxMultiplier;
            isPfc = true;

            accuracyCounters = new Dictionary<Accuracy, int> {
                { Accuracy.Perfect, 0 },
                { Accuracy.Great, 0 },
                { Accuracy.Good, 0 },
                { Accuracy.Okay, 0 },
                { Accuracy.Miss, 0 }
            };
            detailedLossToAccuracy = new Dictionary<Accuracy, int> {
                { Accuracy.Great, 0 },
                { Accuracy.Good, 0 },
                { Accuracy.Okay, 0 }
            };
        }

        public Accuracy AddPointsFromSource(PointSource source, float timingOffset = 0f) {
            switch (source) {
                case PointSource.Match:
                    AddScore(Profile.MatchNoteValue);

                    return Accuracy.Perfect;
                case PointSource.Tap:
                case PointSource.HoldStart:
                case PointSource.Beat:
                    if (calculatingMaxScore) {
                        AddScore(Profile.PressNoteWindows[0].MaxValue);

                        return Accuracy.Perfect;
                    }
                    else
                        return AddTimedNoteScore(timingOffset, Profile.PressNoteWindows);
                case PointSource.Liftoff:
                case PointSource.BeatRelease:
                    if (calculatingMaxScore) {
                        AddScore(Profile.ReleaseNoteWindows[0].MaxValue);

                        return Accuracy.Perfect;
                    }
                    else
                        return AddTimedNoteScore(timingOffset, Profile.ReleaseNoteWindows);
                case PointSource.SpinStart:
                    AddScore(Profile.SpinStartValue);

                    return Accuracy.Perfect;
            }

            return Accuracy.Perfect;
        }
        
        public void AddSustainedNoteTickScore(int amount) => AddScore(amount);

        public void Miss(bool dropMultiplier = true) {
            accuracyCounters[Accuracy.Miss]++;
            
            if (dropMultiplier)
                ResetMultiplier();
        }
        
        public void ResetMultiplier() {
            Multiplier = 1;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
        }
        
        public void PfcLost() => isPfc = false;

        public void Clear() {
            Score = 0;
            Multiplier = Profile.MaxMultiplier;
            MaxScore = 0;
            timedNoteScore = 0;
            potentialTimedNoteScore = 0;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
            earlies = 0;
            lates = 0;
            isPfc = true;
            calculatingMaxScore = false;
            rank = null;
            accuracyCounters[Accuracy.Perfect] = 0;
            accuracyCounters[Accuracy.Great] = 0;
            accuracyCounters[Accuracy.Good] = 0;
            accuracyCounters[Accuracy.Okay] = 0;
            accuracyCounters[Accuracy.Miss] = 0;
            detailedLossToAccuracy[Accuracy.Great] = 0;
            detailedLossToAccuracy[Accuracy.Good] = 0;
            detailedLossToAccuracy[Accuracy.Okay] = 0;
        }

        public void BeginCalculatingMaxScore() {
            MaxScore = 0;
            calculatingMaxScore = true;
        }

        public void FinishCalculatingMaxScore() => calculatingMaxScore = false;
        
        public void GetLoss(out int lossToMisses, out int lossToAccuracy) {
            int totalLoss = MaxScore - Score;
            
            lossToAccuracy = Profile.MaxMultiplier * (potentialTimedNoteScore - timedNoteScore);

            if (lossToAccuracy > totalLoss)
                lossToAccuracy = totalLoss;

            lossToMisses = totalLoss - lossToAccuracy;
        }
        
        public void GetEarlyLateBalance(out int early, out int late) {
            if (earlies == 0 && lates == 0) {
                early = 0;
                late = 0;

                return;
            }
            
            int sum = earlies + lates;
            
            early = (int) Math.Round((float) earlies / sum * 100f);
            late = (int) Math.Round((float) lates / sum * 100f);

            if (early + late >= 100)
                late = 100 - early;

            if (early == 0 && earlies > 0) {
                early++;
                late--;
            }
            
            if (late == 0 && lates > 0) {
                late++;
                early--;
            }
        }

        public bool GetIsPfc() => isPfc && (MaxScore == 0 || Score == MaxScore);

        public int GetAccuracyCount(Accuracy accuracy, out int loss) {
            if (accuracy == Accuracy.Perfect || accuracy == Accuracy.Miss)
                loss = 0;
            else
                loss = Profile.MaxMultiplier * detailedLossToAccuracy[accuracy];
            
            return accuracyCounters[accuracy];
        }

        public float GetAccuracyRating() {
            if (potentialTimedNoteScore == 0)
                return 1f;
            
            return (float) timedNoteScore / potentialTimedNoteScore;
        }

        public string GetRank() => rank ?? (rank = GetRankInternal()) ?? string.Empty;

        private void AddScore(int amount) {
            if (calculatingMaxScore) {
                MaxScore += Profile.MaxMultiplier * amount;
                
                return;
            }
            
            int acc = amount;
            
            while (Multiplier < Profile.MaxMultiplier && acc >= pointsToNextMultiplier) {
                Score += Multiplier * pointsToNextMultiplier;
                acc -= pointsToNextMultiplier;
                Multiplier++;
                pointsToNextMultiplier = Profile.PointsPerMultiplier;
            }

            if (Multiplier < Profile.MaxMultiplier)
                pointsToNextMultiplier -= acc;

            Score += Multiplier * acc;
        }

        private string GetRankInternal() {
            if (MaxScore == 0)
                return null;
            
            float ratio = (float) Score / MaxScore;

            if (ratio > RANKS[0].Key) {
                if (MaxScore - Score < Profile.PointsPerMultiplier * Profile.MaxMultiplier * (Profile.MaxMultiplier - 1) / 2)
                    return "S+";

                return RANKS[0].Value;
            }

            for (int i = 1; i < RANKS.Count; i++) {
                var pair = RANKS[i];

                if (ratio > pair.Key)
                    return pair.Value;
            }

            return "D";
        }
        
        private Accuracy AddTimedNoteScore(float timingOffset, IList<ScoreSystemProfile.TimedNoteWindow> noteWindows) {
            int amount = GetValueFromTiming(timingOffset, noteWindows, out var accuracy);
            int maxAmount = noteWindows[0].MaxValue;
            
            AddScore(amount);
            accuracyCounters[accuracy]++;
            timedNoteScore += amount;
            potentialTimedNoteScore += maxAmount;

            if (accuracy == Accuracy.Perfect)
                return accuracy;

            if (timingOffset > 0f)
                lates++;
            else
                earlies++;
            
            PfcLost();
            detailedLossToAccuracy[accuracy] += maxAmount - amount;

            return accuracy;
        }

        private static int GetValueFromTiming(float timingOffset, IList<ScoreSystemProfile.TimedNoteWindow> noteWindows, out Accuracy accuracy) {
            timingOffset = Math.Abs(timingOffset);

            if (timingOffset < noteWindows[1].LowerBound) {
                accuracy = noteWindows[0].Accuracy;

                return noteWindows[0].MaxValue;
            }

            for (int i = 1; i < noteWindows.Count - 1; i++) {
                var start = noteWindows[i];
                var end = noteWindows[i + 1];

                if (timingOffset >= end.LowerBound)
                    continue;

                accuracy = start.Accuracy;

                return IntMap(timingOffset,
                    start.LowerBound,
                    end.LowerBound,
                    start.MaxValue,
                    end.MaxValue);
            }

            var last = noteWindows[noteWindows.Count - 1];

            accuracy = last.Accuracy;

            return last.MaxValue;
        }

        private static int IntMap(float x, float a, float b, int y, int z) => (int) Math.Ceiling((z - y) * (x - a) / (b - a) + y);


    }
}