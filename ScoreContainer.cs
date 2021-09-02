using System;
using System.Collections.Generic;

namespace ScoreMod {
    public class ScoreContainer {
        // The player must lose fewer than this amount of points to get an S+
        private static readonly int S_PLUS_THRESHOLD = 96;
        // The lower score threshold for each possible rank
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

        // The scoring profile used by this score container
        public ScoreSystemProfile Profile { get; }
        // The score currently stored by this container
        public int Score { get; private set; }
        // The total number of Super Perfects hit
        public int SuperPerfects { get; private set; }
        // The current multiplier for this container
        public int Multiplier { get; private set; }
        // The high score for the current track with this container's scoring profile
        public int HighScore { get; private set; }
        // The number of Super Perfects hit in the high score
        public int HighScoreSuperPerfects { get; private set; }
        // The maximum possible score for the current track with this container's scoring profile
        public int MaxScore { get; private set; }
        // The maximum possible score that could be gained by the notes the player has hit or missed so far
        public int MaxScoreSoFar { get; private set; }

        private int pointsToNextMultiplier;
        private int timedNoteScore;
        private int potentialTimedNoteScore;
        private int earlies;
        private int lates;
        private bool isPfc;
        private string rank;
        private Dictionary<Accuracy, int> accuracyCounters;
        private Dictionary<Accuracy, int> detailedLossToAccuracy;
        private PointHistoryItem[] maxScoreHistory;

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

        // Reset all values and initialize max score history
        public void Initialize(string trackId, int noteCount) {
            Score = 0;
            Multiplier = Profile.MaxMultiplier;
            HighScore = HighScoresContainer.GetHighScore(trackId, Profile.GetUniqueId(), out int highScoreSuperPerfects, out _);
            HighScoreSuperPerfects = highScoreSuperPerfects;
            MaxScore = 0;
            MaxScoreSoFar = 0;
            timedNoteScore = 0;
            potentialTimedNoteScore = 0;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
            earlies = 0;
            lates = 0;
            SuperPerfects = 0;
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
            
            if (noteCount == 0)
                return;
            
            maxScoreHistory = new PointHistoryItem[noteCount];
        }

        // Adds points for a given note type and timing offset. Returns the accuracy type for the given offset
        public Accuracy AddScoreFromNoteType(NoteType noteType, float timingOffset) {
            switch (noteType) {
                case NoteType.Match:
                    AddFlatScore(Profile.MatchNoteValue);

                    return Accuracy.Perfect;
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                    return AddTimedNoteScore(timingOffset, Profile.PressNoteWindows);
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    return AddTimedNoteScore(timingOffset, Profile.ReleaseNoteWindows);
            }

            return Accuracy.Perfect;
        }
        
        // Adds a flat amount of points
        public void AddFlatScore(int amount) => AddScore(amount);

        // Adds points to the max score for a given note type and timing offset. Returns the accuracy type for the given offset
        public void AddMaxScoreFromNoteType(NoteType noteType, int noteIndex) {
            switch (noteType) {
                case NoteType.Match:
                    AddMaxScore(Profile.MatchNoteValue, noteIndex, false);

                    return;
                case NoteType.Tap:
                case NoteType.HoldStart:
                case NoteType.DrumStart:
                    AddMaxScore(Profile.PressNoteWindows[0].MaxValue, noteIndex, false);
                    
                    return;
                case NoteType.SectionContinuationOrEnd:
                case NoteType.DrumEnd:
                    AddMaxScore(Profile.ReleaseNoteWindows[0].MaxValue, noteIndex, false);
                    
                    return;
            }
        }

        // Adds a flat amount of points to the max score
        public void AddFlatMaxScore(int amount, int noteIndex, bool isSustainedNoteTick) => AddMaxScore(amount, noteIndex, isSustainedNoteTick);

        // Moves points for a note from the max score history to the max score so far
        public void PopMaxScoreNote(int noteIndex) => MaxScoreSoFar += maxScoreHistory[noteIndex].PopNoteValue();
        
        // Moves points for a single sustained note tick from the max score history to the max score so far
        public void PopMaxScoreSingleTick(int noteIndex) => MaxScoreSoFar += maxScoreHistory[noteIndex].PopSingleTickValue(Profile.MaxMultiplier);
        
        // Moves points for all ticks of a sustained note from the max score history to the max score so far
        public void PopMaxScoreAllTicks(int noteIndex) => MaxScoreSoFar += maxScoreHistory[noteIndex].PopAllTickValue();

        // Adds a single miss to the miss counter
        public void AddMiss() => accuracyCounters[Accuracy.Miss]++;

        // Sets the current multiplier to 1 and resets the points to the next multiplier
        public void ResetMultiplier() {
            Multiplier = 1;
            pointsToNextMultiplier = Profile.PointsPerMultiplier;
        }
        
        // Deactivates the PFC state for this container
        public void PfcLost() => isPfc = false;
        
        // Gets the total amount of points lost to misses and to mistimings
        public void GetLoss(out int lossToMisses, out int lossToAccuracy) {
            int totalLoss = MaxScore - Score;
            
            lossToAccuracy = Profile.MaxMultiplier * (potentialTimedNoteScore - timedNoteScore);

            if (lossToAccuracy > totalLoss)
                lossToAccuracy = totalLoss;

            lossToMisses = totalLoss - lossToAccuracy;
        }
        
        // Gets the ratio between early mistimings and late mistimings
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

        // Checks if the current score is a PFC
        public bool GetIsPfc(bool checkMaxScore) => isPfc && (!checkMaxScore || Score == MaxScore);

        // Checks if the current score is a new high score
        public bool GetIsHighScore() => Score > HighScore || Score == HighScore && SuperPerfects > HighScoreSuperPerfects;

        // Gets the total number of notes hit with a given accuracy
        public int GetAccuracyCount(Accuracy accuracy, out int loss) {
            if (accuracy == Accuracy.Perfect || accuracy == Accuracy.Miss)
                loss = 0;
            else
                loss = Profile.MaxMultiplier * detailedLossToAccuracy[accuracy];
            
            return accuracyCounters[accuracy];
        }
        
        // Gets the highest attainable score given the current score
        public int GetBestPossible() => MaxScore + Score - MaxScoreSoFar;

        // Gets the percentage of possible points gained from timed note hits
        public float GetAccuracyRating() {
            if (potentialTimedNoteScore == 0)
                return 1f;
            
            return (float) timedNoteScore / potentialTimedNoteScore;
        }

        // Gets the rank given the current score
        public string GetRank() => rank ?? (rank = GetRank(Score, MaxScore)) ?? string.Empty;

        // Gets the rank given a score and its corresponding max score
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

        // For debugging purposes. Gets all notes in the max score history that went unchecked by the end of a track
        public IEnumerable<string> GetMaxScoreSoFarUnchecked() {
            for (int i = 0; i < maxScoreHistory.Length; i++) {
                var item = maxScoreHistory[i];
                int value = item.PopNoteValue();
                int ticks = item.PopAllTickValue();

                if (value > 0 || ticks > 0)
                    yield return $"Note {i}: Type {GameplayState.GetNoteType(i)}, Value {value}, Ticks {ticks}";
            }
        }

        // Adds points to this container and increments the multiplier, splitting points between multipliers if necessary
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
        
        // Adds points to the max possible score and tracks those points in the max score history
        private void AddMaxScore(int amount, int noteIndex, bool isSustainedNoteTick) {
            int scaledAmount = Profile.MaxMultiplier * amount;
            
            MaxScore += scaledAmount;
            
            if (isSustainedNoteTick)
                maxScoreHistory[noteIndex].PushTickValue(scaledAmount);
            else
                maxScoreHistory[noteIndex].PushNoteValue(scaledAmount);
        }

        // Adds points to this container given a timing offset and a set of timing windows
        private Accuracy AddTimedNoteScore(float timingOffset, IList<ScoreSystemProfile.TimedNoteWindow> noteWindows) {
            int amount = GetValueFromTiming(timingOffset, noteWindows, out var accuracy);
            int maxAmount = noteWindows[0].MaxValue;
            
            AddScore(amount);
            accuracyCounters[accuracy]++;
            timedNoteScore += amount;
            potentialTimedNoteScore += maxAmount;

            if (Math.Abs(timingOffset) < Profile.SuperPerfectWindow)
                SuperPerfects++;

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

        // Gets the amount of points to be gained from a given timing offset. Also returns the accuracy type for that offset
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

        // Integer interpolation function used for calculating timed note point values
        private static int IntMap(float x, float a, float b, int y, int z) => (int) Math.Ceiling((z - y) * (x - a) / (b - a) + y);

        // Stores the maximum possible points to gain from a given note
        private struct PointHistoryItem {
            private int noteValue;
            private int tickValue;

            public void PushNoteValue(int value) => noteValue += value;

            public void PushTickValue(int value) => tickValue += value;

            public int PopNoteValue() {
                int value = noteValue;

                noteValue = 0;

                return value;
            }

            public int PopSingleTickValue(int amount) {
                if (amount > tickValue)
                    amount = tickValue;

                tickValue -= amount;

                return amount;
            }

            public int PopAllTickValue() {
                int value = tickValue;

                tickValue = 0;

                return value;
            }
        }
    }
}