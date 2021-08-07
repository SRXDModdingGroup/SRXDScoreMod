﻿using System;
using System.Collections.Generic;

namespace ScoreMod {
    public class ScoreContainer {
        // public static void Main() {
        //     foreach (var window in PRESS_NOTE_WINDOWS)
        //         Console.WriteLine($"{window.Accuracy}: {window.LowerBound} {window.MaxValue} {GetValueFromTiming(window.LowerBound, PRESS_NOTE_WINDOWS, out var accuracy)} {accuracy}");
        // }

        private static readonly int S_PLUS_THRESHOLD = 96;
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

        public ScoreSystemProfile Profile { get; }
        public int Score { get; private set; }
        public int Multiplier { get; private set; }
        public int HighScore { get; private set; }
        public int MaxScore { get; private set; }
        public int MaxScoreSoFar { get; private set; }
        public Dictionary<PointSource, int> MaxScoreBySource { get; }
        public Dictionary<PointSource, int> MaxScoreSoFarBySource { get; }

        private int pointsToNextMultiplier;
        private int timedNoteScore;
        private int potentialTimedNoteScore;
        private int earlies;
        private int lates;
        private bool isPfc;
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

            MaxScoreBySource = new Dictionary<PointSource, int>() {
                { PointSource.Match, 0 },
                { PointSource.Tap, 0 },
                { PointSource.HoldStart, 0 },
                { PointSource.HoldSustain, 0 },
                { PointSource.HoldRelease, 0 },
                { PointSource.Beat, 0 },
                { PointSource.BeatSustain, 0 },
                { PointSource.BeatRelease, 0 },
                { PointSource.SpinStart, 0 },
                { PointSource.SpinSustain, 0 },
                { PointSource.ScratchStart, 0 },
                { PointSource.ScratchSustain, 0 }
            };
            
            MaxScoreSoFarBySource = new Dictionary<PointSource, int>() {
                { PointSource.Match, 0 },
                { PointSource.Tap, 0 },
                { PointSource.HoldStart, 0 },
                { PointSource.HoldSustain, 0 },
                { PointSource.HoldRelease, 0 },
                { PointSource.Beat, 0 },
                { PointSource.BeatSustain, 0 },
                { PointSource.BeatRelease, 0 },
                { PointSource.SpinStart, 0 },
                { PointSource.SpinSustain, 0 },
                { PointSource.ScratchStart, 0 },
                { PointSource.ScratchSustain, 0 }
            };
        }

        public Accuracy AddScoreFromNoteType(NoteType noteType, PointSource source, float timingOffset, bool addToMaxSoFar) {
            switch (noteType) {
                case NoteType.Match:
                    AddFlatScore(Profile.MatchNoteValue, source, addToMaxSoFar);

                    return Accuracy.Perfect;
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                    return AddTimedNoteScore(timingOffset, Profile.PressNoteWindows, source, addToMaxSoFar);
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    return AddTimedNoteScore(timingOffset, Profile.ReleaseNoteWindows, source, addToMaxSoFar);
            }

            return Accuracy.Perfect;
        }
        
        public void AddFlatScore(int amount, PointSource source, bool addToMaxSoFar) {
            AddScore(amount);
            
            if (addToMaxSoFar)
                AddMaxScoreSoFar(amount, source);
        }

        public void AddMaxScoreFromNoteType(NoteType noteType, PointSource source) {
            switch (noteType) {
                case NoteType.Match:
                    AddMaxScore(Profile.MatchNoteValue, source);

                    return;
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                    AddMaxScore(Profile.PressNoteWindows[0].MaxValue, source);
                    
                    return;
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    AddMaxScore(Profile.ReleaseNoteWindows[0].MaxValue, source);
                    
                    return;
            }
        }

        public void AddFlatMaxScore(int amount, PointSource source) => AddMaxScore(amount, source);

        public void MissScoreFromNoteType(NoteType noteType, PointSource source) {
            switch (noteType) {
                case NoteType.Match:
                    AddMaxScoreSoFar(Profile.MatchNoteValue, source);

                    return;
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                    AddMaxScoreSoFar(Profile.PressNoteWindows[0].MaxValue, source);
                    
                    return;
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    AddMaxScoreSoFar(Profile.ReleaseNoteWindows[0].MaxValue, source);
                    
                    return;
                case NoteType.SpinStart:
                case NoteType.SpinLeftStart:
                case NoteType.SpinRightStart:
                    AddMaxScoreSoFar(12, source);

                    return;
                case NoteType.ScratchStart:
                    AddMaxScoreSoFar(1, source);

                    return;
            }
        }

        public void MissFlatScore(int amount, PointSource source) => AddMaxScoreSoFar(amount, source);

        public void AddMiss() => accuracyCounters[Accuracy.Miss]++;

        public void ResetMultiplier() {
            Multiplier = 1;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
        }
        
        public void PfcLost() => isPfc = false;

        public void SetTrackId(string trackId) => HighScore = HighScoresContainer.GetHighScore(trackId, Profile.GetUniqueId(), out _);

        public void Clear() {
            Score = 0;
            Multiplier = Profile.MaxMultiplier;
            MaxScore = 0;
            MaxScoreSoFar = 0;
            timedNoteScore = 0;
            potentialTimedNoteScore = 0;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
            earlies = 0;
            lates = 0;
            isPfc = true;
            rank = null;
            
            accuracyCounters[Accuracy.Perfect] = 0;
            accuracyCounters[Accuracy.Great] = 0;
            accuracyCounters[Accuracy.Good] = 0;
            accuracyCounters[Accuracy.Okay] = 0;
            accuracyCounters[Accuracy.Miss] = 0;
            
            detailedLossToAccuracy[Accuracy.Great] = 0;
            detailedLossToAccuracy[Accuracy.Good] = 0;
            detailedLossToAccuracy[Accuracy.Okay] = 0;
            
            MaxScoreBySource[PointSource.Match] = 0;
            MaxScoreBySource[PointSource.Tap] = 0;
            MaxScoreBySource[PointSource.HoldStart] = 0;
            MaxScoreBySource[PointSource.HoldSustain] = 0;
            MaxScoreBySource[PointSource.HoldRelease] = 0;
            MaxScoreBySource[PointSource.Beat] = 0;
            MaxScoreBySource[PointSource.BeatSustain] = 0;
            MaxScoreBySource[PointSource.BeatRelease] = 0;
            MaxScoreBySource[PointSource.SpinStart] = 0;
            MaxScoreBySource[PointSource.SpinSustain] = 0;
            MaxScoreBySource[PointSource.ScratchStart] = 0;
            MaxScoreBySource[PointSource.ScratchSustain] = 0;
            
            MaxScoreSoFarBySource[PointSource.Match] = 0;
            MaxScoreSoFarBySource[PointSource.Tap] = 0;
            MaxScoreSoFarBySource[PointSource.HoldStart] = 0;
            MaxScoreSoFarBySource[PointSource.HoldSustain] = 0;
            MaxScoreSoFarBySource[PointSource.HoldRelease] = 0;
            MaxScoreSoFarBySource[PointSource.Beat] = 0;
            MaxScoreSoFarBySource[PointSource.BeatSustain] = 0;
            MaxScoreSoFarBySource[PointSource.BeatRelease] = 0;
            MaxScoreSoFarBySource[PointSource.SpinStart] = 0;
            MaxScoreSoFarBySource[PointSource.SpinSustain] = 0;
            MaxScoreSoFarBySource[PointSource.ScratchStart] = 0;
            MaxScoreSoFarBySource[PointSource.ScratchSustain] = 0;
        }
        
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

            if (early + late > 100)
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

        public bool GetIsHighScore() => Score > HighScore;

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

        public string GetRank() => rank ?? (rank = GetRank(Score, MaxScore)) ?? string.Empty;

        public static string GetRank(int score, int maxScore) {
            if (maxScore == 0)
                return null;
            
            float ratio = (float) score / maxScore;

            if (ratio >= RANKS[0].Key) {
                if (maxScore - score < S_PLUS_THRESHOLD)
                    return "S+";

                return RANKS[0].Value;
            }

            for (int i = 1; i < RANKS.Count; i++) {
                var pair = RANKS[i];

                if (ratio >= pair.Key)
                    return pair.Value;
            }

            return "D";
        }

        private void AddScore(int amount) {
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
        
        private void AddMaxScore(int amount, PointSource source) {
            MaxScore += Profile.MaxMultiplier * amount;
            MaxScoreBySource[source] += Profile.MaxMultiplier * amount;
        }

        private void AddMaxScoreSoFar(int amount, PointSource source) {
            MaxScoreSoFar += Profile.MaxMultiplier * amount;
            MaxScoreSoFarBySource[source] += Profile.MaxMultiplier * amount;
        }

        private Accuracy AddTimedNoteScore(float timingOffset, IList<ScoreSystemProfile.TimedNoteWindow> noteWindows, PointSource source, bool addToMaxSoFar) {
            int amount = GetValueFromTiming(timingOffset, noteWindows, out var accuracy);
            int maxAmount = noteWindows[0].MaxValue;
            
            AddScore(amount);
            accuracyCounters[accuracy]++;
            timedNoteScore += amount;
            potentialTimedNoteScore += maxAmount;
            
            if (addToMaxSoFar)
                AddMaxScoreSoFar(maxAmount, source);

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