using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

internal class CustomScoreSystem : IScoreSystem {
    #region IScoreSystemProperties

    public string Name { get; }
    
    public string Id { get; }
    
    public int Score { get; private set; }
    
    public int SecondaryScore { get; private set; }

    public int HighScore { get; private set; }

    public int HighSecondaryScore { get; private set; }
    
    public int MaxPossibleScore { get; private set; }

    public int MaxPossibleScoreSoFar { get; private set; }
    
    public int Streak { get; private set; }
    
    public int MaxStreak { get; private set; }

    public int BestStreak { get; private set; }

    public bool IsHighScore { get; private set; }
    
    public int Multiplier { get; private set; }

    public FullComboState FullComboState { get; private set; }

    public FullComboState StarState { get; private set; }

    public Color StarColor { get; private set; }
    
    public string Rank { get; private set; }

    public bool ImplementsScorePrediction => true;

    public TimingWindow[] TimingWindowsForDisplay => tapTimingWindows;

    public bool ImplementsSecondaryScore => true;

    public string PostGameInfo1Name => "Performance";

    public string PostGameInfo2Name => "Accuracy";

    public string PostGameInfo3Name => "Early / Late";

    public List<ColoredGraphValue> PerformanceGraphValues { get; }

    public string PostGameInfo1Value { get; private set; }
    
    public string PostGameInfo2Value { get; private set; }
    
    public string PostGameInfo3Value { get; private set; }

    #endregion

    #region ScoreSystemProfileValues

    private int matchPointValue;
    private int spinStartPointValue;
    private int maxMultiplier;
    private int multiplierChangeForOverbeat;
    private int multiplierChangeForMissedMatch;
    private int multiplierChangeForMissedTapOrHold;
    private int multiplierChangeForMissedBeat;
    private int multiplierChangeForMissedLiftoff;
    private int multiplierChangeForMissedBeatRelease;
    private int multiplierChangeForMissedSpin;
    private int multiplierChangeForBrokenHold;
    private int multiplierChangeForBrokenBeatHold;
    private int multiplierChangeForBrokenSpin;
    private int multiplierChangeForBrokenScratch;
    private int sPlusThreshold;
    private float holdTickRate;
    private float beatHoldTickRate;
    private float spinTickRate;
    private float scratchTickRate;
    private int[] pointsPerMultiplier;
    private RankThreshold[] rankThresholds;
    private TimingWindow[] tapTimingWindows;
    private TimingWindow[] beatTimingWindows;
    private TimingWindow[] liftoffTimingWindows;
    private TimingWindow[] beatReleaseTimingWindows;

    #endregion
    
    private int maxPossibleStreak;
    private int maxPossibleStreakSoFar;
    private int pointsToNextMultiplier;
    private int earlies;
    private int lates;
    private NoteScoreState[] scoreStates;
    private List<float> overbeatTimes;
    private PlayableTrackData trackData;

    internal CustomScoreSystem(ScoreSystemProfile profile) {
        Name = profile.Name;
        Id = profile.Id;
        matchPointValue = profile.MatchPointValue;
        spinStartPointValue = profile.SpinStartPointValue;
        maxMultiplier = profile.MaxMultiplier;
        multiplierChangeForOverbeat = profile.MultiplierChangeForOverbeat;
        multiplierChangeForMissedMatch = profile.MultiplierChangeForMissedMatch;
        multiplierChangeForMissedTapOrHold = profile.MultiplierChangeForMissedTapOrHold;
        multiplierChangeForMissedBeat = profile.MultiplierChangeForMissedBeat;
        multiplierChangeForMissedLiftoff = profile.MultiplierChangeForMissedLiftoff;
        multiplierChangeForMissedBeatRelease = profile.MultiplierChangeForMissedBeatRelease;
        multiplierChangeForMissedSpin = profile.MultiplierChangeForMissedSpin;
        multiplierChangeForBrokenHold = profile.MultiplierChangeForBrokenHold;
        multiplierChangeForBrokenBeatHold = profile.MultiplierChangeForBrokenBeatHold;
        multiplierChangeForBrokenSpin = profile.MultiplierChangeForBrokenSpin;
        multiplierChangeForBrokenScratch = profile.MultiplierChangeForBrokenScratch;
        sPlusThreshold = profile.SPlusThreshold;
        holdTickRate = profile.HoldTickRate;
        beatHoldTickRate = profile.BeatHoldTickRate;
        spinTickRate = profile.SpinTickRate;
        scratchTickRate = profile.ScratchTickRate;
        pointsPerMultiplier = profile.PointsPerMultiplier;
        rankThresholds = profile.RankThresholds;
        tapTimingWindows = profile.TapTimingWindows;
        beatTimingWindows = profile.BeatTimingWindows;
        liftoffTimingWindows = profile.LiftoffTimingWindows;
        beatReleaseTimingWindows = profile.BeatReleaseTimingWindows;
        overbeatTimes = new List<float>();
        PerformanceGraphValues = new List<ColoredGraphValue>();
    }

    #region IScoreSystemFunctions

    public void Init(PlayState playState) {
        Score = 0;
        SecondaryScore = 0;
        MaxPossibleScore = 0;
        MaxPossibleScoreSoFar = 0;
        Streak = 0;
        MaxStreak = 0;
        Multiplier = maxMultiplier;
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
        maxPossibleStreak = 0;
        maxPossibleStreakSoFar = 0;
        pointsToNextMultiplier = GetPointsToNextMultiplier(maxMultiplier);
        overbeatTimes.Clear();
        trackData = playState.trackData;
        
        InitScoreStates();
        
        var highScoreInfo = HighScoresContainer.GetHighScore(playState.TrackInfoRef, playState.trackData.Difficulty, Id, string.Empty);

        HighScore = highScoreInfo.Score;
        HighSecondaryScore = highScoreInfo.SecondaryScore;
        BestStreak = highScoreInfo.Streak;
    }

    public void Complete(PlayState playState) {
        Rank = GetRank(Score, MaxPossibleScore);
        UpdateFullComboState();

        IsHighScore = HighScoresContainer.TrySetHighScore(playState.TrackInfoRef, playState.CurrentDifficulty, Id, string.Empty, new SavedHighScoreInfo(
            string.Empty,
            Score,
            Streak,
            MaxPossibleScore,
            maxPossibleStreak,
            SecondaryScore));

        if (MaxPossibleScore <= 0)
            PostGameInfo1Value = "100%";
        else
            PostGameInfo1Value = ((float) Score / MaxPossibleScore).ToString("P");

        GetValuesFromScoreStates();
        PostGameInfo2Value = GetAccuracy().ToString("P");
        PostGameInfo3Value = GetEarlyLateRatio();
    }

    public int GetPointValueForSustain(int baseValue, int noteIndex) => scoreStates[noteIndex].GainedBaseSustainPoints;

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset) => GetTimingWindow(timeOffset, tapTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset) => GetTimingWindow(timeOffset, beatTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset) => GetTimingWindow(timeOffset, liftoffTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset) => GetTimingWindow(timeOffset, beatReleaseTimingWindows).TimingAccuracy;
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType)
        => GetHighScoreInfoForTrack(handle.TrackInfoRef, difficultyType);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata)
        => GetHighScoreInfoForTrack(trackInfoRef, metadata.DifficultyType);
    
    #endregion

    #region NoteEvents

    internal void HitMatch(int noteIndex) => AddScore(noteIndex, matchPointValue, 0, null, false);

    internal void HitTap(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, tapTimingWindows);
    
    internal void HitBeat(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, beatTimingWindows);

    internal void HitLiftoff(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, liftoffTimingWindows);

    internal void HitBeatRelease(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, beatReleaseTimingWindows);

    internal void HitSpin(int noteIndex) => AddScore(noteIndex, spinStartPointValue, 0, null, false);

    internal void Overbeat(float time) {
        ChangeMultiplier(-1, multiplierChangeForOverbeat);
        
        if (multiplierChangeForOverbeat < 0)
            overbeatTimes.Add(time);
    }

    internal void MissMatch(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedMatch);

    internal void MissTap(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedTapOrHold);
    
    internal void MissBeat(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedBeat);
    
    internal void MissHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForMissedTapOrHold);

    internal void MissBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForMissedBeat);

    internal void MissLiftoff(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedLiftoff);
    
    internal void MissBeatRelease(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedBeatRelease);

    internal void MissSpin(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedSpin);
    
    internal void BreakHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForBrokenHold);
    
    internal void BreakBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForBrokenBeatHold);
    
    internal void BreakSpin(int noteIndex) => MissNote(noteIndex, multiplierChangeForBrokenSpin);
    
    internal void BreakScratch(int noteIndex) => MissNote(noteIndex, multiplierChangeForBrokenScratch);

    internal void UpdateHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, holdTickRate);

    internal void UpdateBeatHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, beatHoldTickRate);

    internal void UpdateSpin(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, spinTickRate);

    internal void UpdateScratch(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, scratchTickRate);

    internal void CompleteNote(int noteIndex) {
        if (AddRemainingValueToMaxScoreSoFar(noteIndex))
            UpdateFullComboState();
    }

    #endregion

    #region InternalLogic

    private void AddScore(int noteIndex, int amount, int secondaryAmount, CustomTimingAccuracy timingAccuracy, bool fromSustain) {
        int acc = amount;
        int scoreAdded = 0;
            
        while (Multiplier < maxMultiplier && acc >= pointsToNextMultiplier) {
            scoreAdded += Multiplier * pointsToNextMultiplier;
            acc -= pointsToNextMultiplier;
            ChangeMultiplier(noteIndex, 1);
        }

        if (Multiplier < maxMultiplier)
            pointsToNextMultiplier -= acc;
        
        scoreAdded += Multiplier * acc;
        Score += scoreAdded;
        SecondaryScore += secondaryAmount;

        var scoreState = scoreStates[noteIndex];

        if (fromSustain) {
            scoreState.GainedBaseSustainPoints += amount;
            scoreState.GainedTotalSustainPoints += scoreAdded;

            int maxAmount = maxMultiplier * amount;
            
            MaxPossibleScoreSoFar += maxAmount;
            scoreState.RemainingTotalSustainPoints -= maxAmount;
        }
        else {
            scoreState.GainedBasePoints = amount;
            scoreState.GainedTotalPoints = scoreAdded;
            scoreState.TimingAccuracy = timingAccuracy;
            
            MaxPossibleScoreSoFar += scoreState.RemainingTotalPoints;
            scoreState.RemainingTotalPoints = 0;
        }

        Streak += scoreState.RemainingStreak;
        maxPossibleStreakSoFar += scoreState.RemainingStreak;
        scoreState.RemainingStreak = 0;
        
        if (Streak > MaxStreak)
            MaxStreak = Streak;

        if (scoreState.RemainingTotalPoints == 0
            && scoreState.RemainingTotalSustainPoints == 0
            && scoreState.RemainingStreak == 0)
            scoreState.Completed = true;

        UpdateFullComboState();
    }

    private void AddTimedNoteScore(int noteIndex, float timeOffset, TimingWindow[] timingWindows) {
        var timingWindow = GetTimingWindow(timeOffset, timingWindows);
        
        AddScore(noteIndex, timingWindow.PointValue, timingWindow.SecondaryPointValue, timingWindow.TimingAccuracy, false);
    }

    private void MissNote(int noteIndex, int multiplierChange) {
        if (!AddRemainingValueToMaxScoreSoFar(noteIndex))
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }
    
    private void MissPairedNote(int noteIndex, int endNoteIndex, int multiplierChange) {
        if (!AddRemainingValueToMaxScoreSoFar(noteIndex) && !AddRemainingValueToMaxScoreSoFar(endNoteIndex))
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }

    private void UpdateSustainedNoteValue(int noteIndex, float heldTime, float tickRate) {
        var scoreState = scoreStates[noteIndex];

        if (scoreState.AvailableBaseSustainPoints == 0)
            return;
        
        int valueChange = Mathf.Clamp(Mathf.FloorToInt(tickRate * heldTime), 0, scoreState.AvailableBaseSustainPoints)
            - scoreState.GainedBaseSustainPoints;
        
        if (valueChange > 0)
            AddScore(noteIndex, valueChange, 0, null, true);
    }

    private void ChangeMultiplier(int noteIndex, int amount) {
        if (amount == 0)
            return;
        
        Multiplier = Mathf.Clamp(Multiplier + amount, 1, maxMultiplier);
        pointsToNextMultiplier = GetPointsToNextMultiplier(Multiplier);

        if (amount > 0)
            return;
        
        Streak = 0;
        
        if (noteIndex >= 0)
            scoreStates[noteIndex].LostMultiplier = true;
    }

    private void UpdateFullComboState() {
        if (Streak > maxPossibleStreakSoFar || Score > MaxPossibleScoreSoFar) {
            FullComboState = FullComboState.PerfectFullCombo;
            StarState = FullComboState.PerfectFullCombo;
            StarColor = Color.red;
            
            return;
        }
        
        if (Streak < maxPossibleStreakSoFar && MaxStreak < maxPossibleStreak) {
            FullComboState = FullComboState.None;
            StarState = FullComboState.None;

            return;
        }

        if (Score < MaxPossibleScoreSoFar - sPlusThreshold) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.FullCombo;
            StarColor = Color.green;

            return;
        }
        
        if (Score < MaxPossibleScoreSoFar) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.PerfectFullCombo;
            StarColor = Color.green;
            
            return;
        }
        
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
    }

    private void InitScoreStates() {
        scoreStates = new NoteScoreState[trackData.NoteCount];

        int maxTapValue = GetMaxPointValue(tapTimingWindows);
        int maxBeatValue = GetMaxPointValue(beatTimingWindows);
        int maxLiftoffValue = GetMaxPointValue(liftoffTimingWindows);
        int maxBeatReleaseValue = GetMaxPointValue(beatReleaseTimingWindows);

        for (int i = 0; i < scoreStates.Length; i++) {
            var note = trackData.GetNote(i);
            int availableBasePoints = 0;
            int availableBaseSustainPoints = 0;
            int availableStreak = 0;

            switch (note.NoteType) {
                case NoteType.Match:
                    availableBasePoints = matchPointValue;
                    availableStreak = 1;
                    
                    break;
                case NoteType.DrumStart:
                    availableBasePoints = maxBeatValue;
                    availableStreak = 1;

                    if (note.length > 0f)
                        availableBaseSustainPoints = Mathf.FloorToInt(beatHoldTickRate * note.length);
                    
                    break;
                case NoteType.SpinRightStart:
                case NoteType.SpinLeftStart:
                    var spinnerSection = trackData.SpinnerSections[trackData.SpinnerSectionIndexForNoteIndex[i]];
                    
                    availableBasePoints = spinStartPointValue;
                    availableBaseSustainPoints = Mathf.FloorToInt(spinTickRate * (spinnerSection.endsAtTime - spinnerSection.startsAtTime));
                    availableStreak = 1;
                    
                    break;
                case NoteType.HoldStart:
                    var freestyleSection0 = trackData.FreestyleSections[trackData.FreestyleSectionIndexForNoteIndex[i]];

                    availableBasePoints = maxTapValue;
                    availableBaseSustainPoints = Mathf.FloorToInt(holdTickRate * (freestyleSection0.EndTime - freestyleSection0.Time));
                    availableStreak = 1;
                    
                    break;
                case NoteType.SectionContinuationOrEnd:
                    if (note.FreestyleEndType != FreestyleSection.EndType.Release)
                        break;
                    
                    var freestyleSection1 = trackData.FreestyleSections[trackData.FreestyleSectionIndexForNoteIndex[i]];

                    if (freestyleSection1.endNoteIndex != i)
                        break;

                    availableBasePoints = maxLiftoffValue;
                    availableStreak = 1;

                    break;
                case NoteType.Tap:
                    availableBasePoints = maxTapValue;
                    availableStreak = 1;
                    
                    break;
                case NoteType.DrumEnd:
                    if (note.DrumEndType != DrumSection.EndType.Release)
                        break;

                    var startNote = trackData.GetNote(trackData.DrumIndexForNoteIndex[i]);
                    
                    if (startNote.endNoteIndex != i)
                        break;
                    
                    availableBasePoints = maxBeatReleaseValue;
                    availableStreak = 1;

                    break;
                case NoteType.ScratchStart:
                    var scratchSection = trackData.ScratchSections[trackData.ScratchSectionIndexForNoteIndex[i]];

                    if (scratchSection.IsEmpty)
                        break;
                    
                    availableBaseSustainPoints = Mathf.FloorToInt(scratchTickRate * (scratchSection.endsAtTime - scratchSection.startsAtTime));
                    availableStreak = 1;

                    break;
            }

            int availableTotalPoints = maxMultiplier * availableBasePoints;
            int availableTotalSustainPoints = maxMultiplier * availableBaseSustainPoints;

            MaxPossibleScore += availableTotalPoints + availableTotalSustainPoints;
            maxPossibleStreak += availableStreak;
            scoreStates[i] = new NoteScoreState(
                availableBasePoints,
                availableBaseSustainPoints,
                availableTotalPoints,
                availableTotalSustainPoints,
                availableStreak);
        }
    }

    private void GetValuesFromScoreStates() {
        earlies = 0;
        lates = 0;
        PerformanceGraphValues.Clear();
        overbeatTimes.Sort();

        float sectionLength = trackData.SoundEndTime / 60f;
        float nextSectionTime = sectionLength;
        int counter = 1;
        int totalValueForSection = 0;
        int maxValueForSection = 0;
        bool currentSectionLostMultiplier = false;

        for (int i = 0; i < trackData.NoteCount; i++) {
            var note = trackData.GetNote(i);
            float time = note.time;
            
            while (time > nextSectionTime)
                PopSection();

            var scoreState = scoreStates[i];

            totalValueForSection += scoreState.GainedTotalPoints + scoreState.GainedTotalSustainPoints;
            maxValueForSection += scoreState.AvailableTotalPoints + scoreState.AvailableTotalSustainPoints;

            if (scoreState.LostMultiplier)
                currentSectionLostMultiplier = true;
            
            var timingAccuracy = scoreState.TimingAccuracy;
            
            if (timingAccuracy == null)
                continue;

            var baseTimingAccuracy = timingAccuracy.BaseAccuracy;
            
            if (baseTimingAccuracy == NoteTimingAccuracy.Early)
                earlies++;
            else if (baseTimingAccuracy == NoteTimingAccuracy.Late)
                lates++;
        }
        
        PopSection();

        void PopSection() {
            if (maxValueForSection > 0) {
                while (overbeatTimes.Count > 0 && overbeatTimes[0] < nextSectionTime) {
                    currentSectionLostMultiplier = true;
                    overbeatTimes.RemoveAt(0);
                }
                
                float value = Mathf.Clamp((float) totalValueForSection / maxValueForSection, 0.05f, 1f);
                Color color;

                if (currentSectionLostMultiplier || totalValueForSection == 0)
                    color = Color.red;
                else if (totalValueForSection == maxValueForSection)
                    color = Color.cyan;
                else
                    color = Color.yellow;
                
                PerformanceGraphValues.Add(new ColoredGraphValue(value, color));
                totalValueForSection = 0;
                maxValueForSection = 0;
                currentSectionLostMultiplier = false;
            }

            counter++;
            nextSectionTime = sectionLength * counter;
        }
    }

    private bool AddRemainingValueToMaxScoreSoFar(int noteIndex) {
        var scoreState = scoreStates[noteIndex];
        
        if (scoreState.Completed)
            return false;

        MaxPossibleScoreSoFar += scoreState.RemainingTotalPoints + scoreState.RemainingTotalSustainPoints;
        maxPossibleStreakSoFar += scoreState.RemainingStreak;
        scoreState.RemainingTotalPoints = 0;
        scoreState.RemainingTotalSustainPoints = 0;
        scoreState.RemainingStreak = 0;
        scoreState.Completed = true;

        return true;
    }

    private int GetPointsToNextMultiplier(int currentMultiplier) => pointsPerMultiplier[Mathf.Min(currentMultiplier, pointsPerMultiplier.Length) - 1];

    private float GetAccuracy() {
        int totalGained = 0;
        int totalPossible = 0;

        foreach (var scoreState in scoreStates) {
            int gained = scoreState.GainedBasePoints;
            
            if (gained == 0)
                continue;

            totalGained += gained;
            totalPossible += scoreState.AvailableBasePoints;
        }

        if (totalPossible == 0f)
            return 1f;

        return (float) totalGained / totalPossible;
    }

    private string GetRank(int score, int maxScore) {
        if (score == 0)
            return "-";
        
        if (score > maxScore - sPlusThreshold)
            return "S+";
        
        float scoreRatio = (float) score / maxScore;

        foreach (var pair in rankThresholds) {
            if (scoreRatio < pair.Threshold)
                continue;

            return pair.Rank;
        }

        return rankThresholds[rankThresholds.Length - 1].Rank;
    }

    private string GetEarlyLateRatio() {
        if (earlies == 0 && lates == 0)
            return "0 : 0";

        int sum = earlies + lates;
        int early = Mathf.RoundToInt((float) earlies / sum * 100f);
        int late = Mathf.RoundToInt((float) lates / sum * 100f);

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

        return $"{early} : {late}";
    }

    private HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType) {
        var savedInfo = HighScoresContainer.GetHighScore(trackInfoRef, difficultyType, Id, string.Empty);

        return new HighScoreInfo(
            savedInfo.Score,
            savedInfo.Streak,
            savedInfo.SecondaryScore,
            GetRank(savedInfo.Score, savedInfo.MaxScore),
            GetFullComboState(savedInfo.Score, savedInfo.MaxStreak, savedInfo.Streak, savedInfo.MaxStreak));
    }

    private static int GetMaxPointValue(TimingWindow[] timingWindows) {
        int max = 0;

        foreach (var window in timingWindows) {
            int pointValue = window.PointValue;

            if (pointValue > max)
                max = pointValue;
        }

        return max;
    }

    private static FullComboState GetFullComboState(int score, int maxScore, int streak, int maxStreak) {
        if (maxScore == 0 || maxStreak == 0)
            return FullComboState.None;

        if (score == maxScore)
            return FullComboState.PerfectFullCombo;

        if (streak == maxStreak)
            return FullComboState.FullCombo;

        return FullComboState.None;
    }

    private static TimingWindow GetTimingWindow(float timeOffset, TimingWindow[] timingWindows) {
        foreach (var window in timingWindows) {
            if (timeOffset < window.UpperBound)
                return window;
        }

        return timingWindows[timingWindows.Length - 1];
    }

    #endregion

    private class NoteScoreState {
        public int AvailableBasePoints { get; }
        public int AvailableBaseSustainPoints { get; }
        public int AvailableTotalPoints { get; }
        public int AvailableTotalSustainPoints { get; }
        public int GainedBasePoints { get; set; }
        public int GainedBaseSustainPoints { get; set; }
        public int GainedTotalPoints { get; set; }
        public int GainedTotalSustainPoints { get; set; }
        public int RemainingTotalPoints { get; set; }
        public int RemainingTotalSustainPoints { get; set; }
        public int RemainingStreak { get; set; }
        public bool Completed { get; set; }
        public bool LostMultiplier { get; set; }
        public CustomTimingAccuracy TimingAccuracy { get; set; }

        public NoteScoreState(int availableBasePoints, int availableBaseSustainPoints, int availableTotalPoints, int availableTotalSustainPoints, int availableStreak) {
            Completed = false;
            AvailableBasePoints = availableBasePoints;
            AvailableBaseSustainPoints = availableBaseSustainPoints;
            AvailableTotalPoints = availableTotalPoints;
            AvailableTotalSustainPoints = availableTotalSustainPoints;
            GainedBasePoints = 0;
            GainedBaseSustainPoints = 0;
            GainedTotalPoints = 0;
            GainedTotalSustainPoints = 0;
            RemainingTotalPoints = availableTotalPoints;
            RemainingTotalSustainPoints = availableTotalSustainPoints;
            RemainingStreak = availableStreak;
            LostMultiplier = false;
            TimingAccuracy = null;
        }
    }
}