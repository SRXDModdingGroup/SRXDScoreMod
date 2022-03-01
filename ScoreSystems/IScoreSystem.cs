namespace SRXDScoreMod; 

/// <summary>
/// Interface for getting information from a score system
/// </summary>
public interface IScoreSystem {
    /// <summary>
    /// The name of the score system
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The current score
    /// </summary>
    int Score { get; }
        
    /// <summary>
    /// The current secondary score
    /// </summary>
    /// <remarks>Not implemented by the base score system</remarks>
    int SecondaryScore { get; }
    
    /// <summary>
    /// The high score for the current track
    /// </summary>
    /// <remarks>Not implemented by the base score system</remarks>
    int HighScore { get; }
        
    /// <summary>
    /// The secondary score associated with the high score for the current track
    /// </summary>
    /// <remarks>Not implemented by the base score system</remarks>
    int HighSecondaryScore { get; }
        
    /// <summary>
    /// The maximum possible score for the current track
    /// </summary>
    /// <remarks>Not implemented by the base score system</remarks>
    int MaxPossibleScore { get; }

    /// <summary>
    /// The current streak
    /// </summary>
    int Streak { get; }

    /// <summary>
    /// The highest streak attained so far
    /// </summary>
    int MaxStreak { get; }
        
    /// <summary>
    /// The best streak for the current track
    /// </summary>
    /// <remarks>Not implemented by the base score system</remarks>
    int BestStreak { get; }
        
    /// <summary>
    /// True if the final score is a new high score
    /// </summary>
    bool IsHighScore { get; }
        
    /// <summary>
    /// The current score multiplier
    /// </summary>
    int Multiplier { get; }
        
    /// <summary>
    /// The current full combo state
    /// </summary>
    FullComboState FullComboState { get; }
        
    /// <summary>
    /// The rank of the final score
    /// </summary>
    string Rank { get; }
        
    /// <summary>
    /// Additional information displayed on the complete screen
    /// </summary>
    string PostGameInfo1Value { get; }
        
    /// <summary>
    /// Additional information displayed on the complete screen
    /// </summary>
    string PostGameInfo2Value { get; }
        
    /// <summary>
    /// Additional information displayed on the complete screen
    /// </summary>
    string PostGameInfo3Value { get; }
    
    /// <summary>
    /// True if the score system implements secondary score
    /// </summary>
    public bool ImplementsSecondaryScore { get; }
    
    /// <summary>
    /// True if the score system implements score prediction
    /// </summary>
    public bool ImplementsScorePrediction { get; }
    
    /// <summary>
    /// The timing windows to display on the timing bar with the SRXDTimingBar mod
    /// </summary>
    public TimingWindow[] TimingWindowsForDisplay { get; }
    
    /// <summary>
    /// Gets the high score info for a given track
    /// </summary>
    /// <param name="handle">The metadata handle for the track</param>
    /// <param name="difficultyType">The difficulty type of the track</param>
    /// <returns></returns>
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType);
}