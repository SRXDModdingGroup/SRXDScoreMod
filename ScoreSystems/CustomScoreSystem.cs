using UnityEngine;

namespace SRXDScoreMod; 

public abstract class CustomScoreSystem : IScoreSystem {
    #region IReadOnlyScoreSystemProperties

    public int Score { get; private set; }
    
    public int SecondaryScore { get; private set; }

    public int HighScore => 0;

    public int HighSecondaryScore => 0;
    
    public int MaxPossibleScore { get; private set; }

    public int MaxPossibleScoreSoFar { get; private set; }
    
    public int Streak { get; private set; }
    
    public int MaxStreak { get; private set; }

    public int BestStreak => 0;

    public bool IsHighScore => false;
    
    public int Multiplier { get; private set; }

    public FullComboState FullComboState => isFC ? isPFC ? FullComboState.PerfectFullCombo : FullComboState.FullCombo : FullComboState.None;

    public FullComboState StarState => isPFC || MaxPossibleScoreSoFar - Score < SPlusThreshold ?
        FullComboState.PerfectFullCombo : isFC ? FullComboState.FullCombo : FullComboState.None;

    public Color StarColor => isPFC ? Color.cyan : Color.green;
    
    public string Rank { get; private set; }

    public string PostGameInfo1Value => string.Empty;
    
    public string PostGameInfo2Value => string.Empty;
    
    public string PostGameInfo3Value => string.Empty;

    public bool ImplementsSecondaryScore => false;

    public bool ImplementsScorePrediction => true;
    
    public string PostGameInfo1Name => string.Empty;
    
    public string PostGameInfo2Name => string.Empty;
    
    public string PostGameInfo3Name => string.Empty;

    public TimingWindow[] TimingWindowsForDisplay => TapTimingWindows;

    #endregion

    protected virtual int MaxMultiplier => 4;

    #region MultiplierChangesForMisses

    protected virtual int MultiplierChangeForOverbeat => -3;
    
    protected virtual int MultiplierChangeForMissedMatch => -3;
    
    protected virtual int MultiplierChangeForMissedTapOrHold => -3;
    
    protected virtual int MultiplierChangeForMissedBeat => -3;
    
    protected virtual int MultiplierChangeForMissedLiftoff => 0;
    
    protected virtual int MultiplierChangeForMissedBeatRelease => 0;
    
    protected virtual int MultiplierChangeForMissedSpin => -3;
    
    protected virtual int MultiplierChangeForBrokenHold => 0;
    
    protected virtual int MultiplierChangeForBrokenBeatHold => 0;
    
    protected virtual int MultiplierChangeForBrokenSpin => 0;
    
    protected virtual int MultiplierChangeForBrokenScratch => -3;

    #endregion

    #region PointValuesAndTickRates

    protected abstract int MatchPointValue { get; }

    protected abstract int SpinStartPointValue { get; }
    
    protected abstract float HoldTickRate { get; }
    
    protected abstract float BeatHoldTickRate { get; }
    
    protected abstract float SpinTickRate { get; }
    
    protected abstract float ScratchTickRate { get; }

    #endregion

    #region TimingWindows

    protected abstract TimingWindow[] TapTimingWindows { get; }
    
    protected abstract TimingWindow[] BeatTimingWindows { get; }
    
    protected abstract TimingWindow[] LiftoffTimingWindows { get; }
    
    protected abstract TimingWindow[] BeatReleaseTimingWindows { get; }

    #endregion
    
    protected abstract RankThreshold[] RankThresholds { get; }
    
    protected abstract int SPlusThreshold { get; }

    private bool isFC;
    private bool isPFC;
    private int maxPossibleStreak;
    private int pointsToNextMultiplier;
    private PlayableTrackData trackData;
    private NoteScoreState[] scoreStates;

    public virtual void Init(PlayState playState) {
        Score = 0;
        SecondaryScore = 0;
        MaxPossibleScore = 0;
        MaxPossibleScoreSoFar = 0;
        Streak = 0;
        MaxStreak = 0;
        Multiplier = MaxMultiplier;
        isFC = true;
        isPFC = true;
        maxPossibleStreak = 0;
        pointsToNextMultiplier = GetPointsToNextMultiplier(MaxMultiplier);
        trackData = playState.trackData;
        
        InitScoreStates(trackData);
    }

    public virtual void Complete(PlayState playState) {
        float scoreRatio = (float) Score / MaxPossibleScore;

        for (int i = 0; i < RankThresholds.Length; i++) {
            var pair = RankThresholds[i];
            
            if (scoreRatio < pair.Threshold)
                continue;

            if (i == 0 && Score > MaxPossibleScore - SPlusThreshold)
                Rank = "S+";
            else
                Rank = pair.Rank;

            break;
        }
    }

    #region TimingAccuracyFunctions

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset) => GetTimingAccuracy(timeOffset, TapTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset) => GetTimingAccuracy(timeOffset, BeatTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset) => GetTimingAccuracy(timeOffset, LiftoffTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset) => GetTimingAccuracy(timeOffset, BeatReleaseTimingWindows);

    #endregion
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType)
        => GetHighScoreInfoForTrack(handle.TrackInfoRef, difficultyType);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata)
        => GetHighScoreInfoForTrack(trackInfoRef, metadata.DifficultyType);

    protected abstract int GetPointsToNextMultiplier(int currentMultiplier);

    #region TimedNoteValueFunctions

    protected abstract int GetPointValueForTap(float timeOffset);

    protected abstract int GetPointValueForBeat(float timeOffset);

    protected abstract int GetPointValueForLiftoff(float timeOffset);

    protected abstract int GetPointValueForBeatRelease(float timeOffset);

    #endregion

    #region NoteEvents

    internal void HitMatch(int noteIndex) => AddScore(noteIndex, MatchPointValue, 1, null, false);

    internal void HitTap(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForTap(timeOffset), 1, GetTimingAccuracyForTap(timeOffset), false);

    internal void HitBeat(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForBeat(timeOffset), 1, GetTimingAccuracyForBeat(timeOffset), false);

    internal void HitLiftoff(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForLiftoff(timeOffset), 1, GetTimingAccuracyForLiftoff(timeOffset), false);

    internal void HitBeatRelease(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForBeatRelease(timeOffset), 1, GetTimingAccuracyForBeatRelease(timeOffset), false);

    internal void HitSpin(int noteIndex) => AddScore(noteIndex, SpinStartPointValue, 1, null, false);

    internal void Overbeat() => ChangeMultiplier(MultiplierChangeForOverbeat);
    
    internal void MissMatch(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedMatch);

    internal void MissTap(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedTapOrHold);
    
    internal void MissBeat(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedBeat);
    
    internal void MissHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, MultiplierChangeForMissedTapOrHold);

    internal void MissBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, MultiplierChangeForMissedBeat);

    internal void MissLiftoff(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedLiftoff);
    
    internal void MissBeatRelease(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedBeatRelease);

    internal void MissSpin(int noteIndex) => MissNote(noteIndex, MultiplierChangeForMissedSpin);
    
    internal void BreakHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, MultiplierChangeForBrokenHold);
    
    internal void BreakBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, MultiplierChangeForBrokenBeatHold);
    
    internal void BreakSpin(int noteIndex) => MissNote(noteIndex, MultiplierChangeForBrokenSpin);
    
    internal void BreakScratch(int noteIndex) => MissNote(noteIndex, MultiplierChangeForBrokenScratch);

    internal void UpdateHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, HoldTickRate);

    internal void UpdateBeatHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, BeatHoldTickRate);

    internal void UpdateSpin(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, SpinTickRate);

    internal void UpdateScratch(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, ScratchTickRate);

    #endregion

    #region ScoringLogic

    private void AddScore(int noteIndex, int amount, int addStreak, CustomTimingAccuracy timingAccuracy, bool fromSustain) {
        int acc = amount;
        int scoreAdded = 0;
            
        while (Multiplier < MaxMultiplier && acc >= pointsToNextMultiplier) {
            scoreAdded += Multiplier * pointsToNextMultiplier;
            acc -= pointsToNextMultiplier;
            ChangeMultiplier(1);
        }

        if (Multiplier < MaxMultiplier)
            pointsToNextMultiplier -= acc;
        
        scoreAdded += Multiplier * acc;
        Score += scoreAdded;
        Streak += addStreak;

        if (Streak > MaxStreak)
            MaxStreak = Streak;

        var scoreState = scoreStates[noteIndex];

        if (fromSustain) {
            scoreState.GainedBaseSustainPoints += amount;
            scoreState.GainedTotalSustainPoints += scoreAdded;
            MaxPossibleScoreSoFar += MaxMultiplier * amount;
        }
        else {
            scoreState.GainedBasePoints = amount;
            scoreState.GainedTotalPoints = scoreAdded;
            scoreState.TimingAccuracy = timingAccuracy;
            MaxPossibleScoreSoFar += scoreStates[noteIndex].AvailableTotalPoints;
        }

        scoreStates[noteIndex] = scoreState;
        isPFC = Score >= MaxPossibleScoreSoFar;
    }

    private void MissNote(int noteIndex, int multiplierChange) {
        AddRemainingValueToMaxScoreSoFar(noteIndex);
        ChangeMultiplier(multiplierChange);
    }
    
    private void MissPairedNote(int noteIndex, int endNoteIndex, int multiplierChange) {
        AddRemainingValueToMaxScoreSoFar(noteIndex);
        AddRemainingValueToMaxScoreSoFar(endNoteIndex);
        ChangeMultiplier(multiplierChange);
    }

    private void UpdateSustainedNoteValue(int noteIndex, float time, float tickRate) {
        var scoreState = scoreStates[noteIndex];

        if (scoreState.AvailableBaseSustainPoints == 0)
            return;
        
        int newValue = Mathf.Clamp(Mathf.FloorToInt(tickRate * time), 1, scoreState.AvailableBaseSustainPoints);
        
        AddScore(noteIndex, newValue - scoreState.GainedBaseSustainPoints, 0, null, true);
    }

    private void AddRemainingValueToMaxScoreSoFar(int noteIndex) {
        var scoreState = scoreStates[noteIndex];

        MaxPossibleScoreSoFar += scoreState.AvailableTotalPoints + scoreState.AvailableTotalSustainPoints
                                 - (scoreState.GainedTotalPoints - scoreState.GainedTotalSustainPoints);
    }

    private void ChangeMultiplier(int amount) {
        Multiplier = Mathf.Clamp(Multiplier + amount, 1, MaxMultiplier);
        pointsToNextMultiplier = GetPointsToNextMultiplier(Multiplier);
        
        if (amount < 0)
            ResetStreak();
    }

    private void ResetStreak() {
        Streak = 0;

        if (MaxStreak < maxPossibleStreak)
            isFC = false;
    }

    private void InitScoreStates(PlayableTrackData trackData) {
        scoreStates = new NoteScoreState[trackData.NoteCount];

        int maxTapValue = GetPointValueForTap(0f);
        int maxBeatValue = GetPointValueForBeat(0f);
        int maxLiftoffValue = GetPointValueForLiftoff(0f);
        int maxBeatReleaseValue = GetPointValueForBeatRelease(0f);

        for (int i = 0; i < scoreStates.Length; i++) {
            var note = trackData.GetNote(i);

            int availableBasePoints = 0;
            int availableBaseSustainPoints = 0;

            switch (note.NoteType) {
                case NoteType.Match:
                    availableBasePoints = MatchPointValue;
                    maxPossibleStreak++;
                    
                    break;
                case NoteType.DrumStart:
                    availableBasePoints = maxBeatValue;
                    maxPossibleStreak++;

                    if (note.length > 0f)
                        availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(BeatHoldTickRate * note.length));
                    
                    break;
                case NoteType.SpinRightStart:
                case NoteType.SpinLeftStart:
                    var spinnerSection = trackData.SpinnerSections[trackData.SpinnerSectionIndexForNoteIndex[i]];
                    
                    availableBasePoints = SpinStartPointValue;
                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(SpinTickRate * (spinnerSection.endsAtTime - spinnerSection.startsAtTime)));
                    maxPossibleStreak++;
                    
                    break;
                case NoteType.HoldStart:
                    var freestyleSection = trackData.FreestyleSections[trackData.FreestyleSectionIndexForNoteIndex[i]];

                    availableBasePoints = maxTapValue;
                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(HoldTickRate * (freestyleSection.EndTime - freestyleSection.Time)));
                    maxPossibleStreak++;
                    
                    break;
                case NoteType.SectionContinuationOrEnd:
                    if (note.FreestyleEndType == FreestyleSection.EndType.Release) {
                        availableBasePoints = maxLiftoffValue;
                        maxPossibleStreak++;
                    }

                    break;
                case NoteType.Tap:
                    availableBasePoints = maxTapValue;
                    maxPossibleStreak++;
                    
                    break;
                case NoteType.DrumEnd:
                    if (note.DrumEndType == DrumSection.EndType.Release) {
                        availableBasePoints = maxBeatReleaseValue;
                        maxPossibleStreak++;
                    }

                    break;
                case NoteType.ScratchStart:
                    var scratchSection = trackData.ScratchSections[trackData.ScratchSectionIndexForNoteIndex[i]];

                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(ScratchTickRate * (scratchSection.endsAtTime - scratchSection.startsAtTime)));
                    maxPossibleStreak++;
                    
                    break;
            }

            int availableTotalPoints = MaxMultiplier * availableBasePoints;
            int availableTotalSustainPoints = MaxMultiplier * availableBaseSustainPoints;

            MaxPossibleScore += availableTotalPoints + availableTotalSustainPoints;
            scoreStates[i] = new NoteScoreState(
                availableBasePoints,
                availableBaseSustainPoints,
                availableTotalPoints,
                availableTotalSustainPoints);
        }
    }

    private CustomTimingAccuracy GetTimingAccuracy(float timeOffset, TimingWindow[] timingWindows) {
        foreach (var window in timingWindows) {
            if (timeOffset < window.UpperBound)
                return window.TimingAccuracy;
        }

        return timingWindows[timingWindows.Length - 1].TimingAccuracy;
    }

    #endregion

    private HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType) => HighScoreInfo.Blank;
    
    private struct NoteScoreState {
        public int AvailableBasePoints { get; }
    
        public int AvailableBaseSustainPoints { get; }
    
        public int AvailableTotalPoints { get; }
    
        public int AvailableTotalSustainPoints { get; }
    
        public int GainedBasePoints { get; set; }
    
        public int GainedBaseSustainPoints { get; set; }
    
        public int GainedTotalPoints { get; set; }
    
        public int GainedTotalSustainPoints { get; set; }
        
        public CustomTimingAccuracy TimingAccuracy { get; set; }

        public NoteScoreState(int availableBasePoints, int availableBaseSustainPoints, int availableTotalPoints, int availableTotalSustainPoints) {
            AvailableBasePoints = availableBasePoints;
            AvailableBaseSustainPoints = availableBaseSustainPoints;
            AvailableTotalPoints = availableTotalPoints;
            AvailableTotalSustainPoints = availableTotalSustainPoints;
            GainedBasePoints = 0;
            GainedBaseSustainPoints = 0;
            GainedTotalPoints = 0;
            GainedTotalSustainPoints = 0;
            TimingAccuracy = null;
        }
    }
}