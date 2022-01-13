namespace SRXDScoreMod; 

public interface IScoreSystem {
    public IScoreContainer ScoreContainer { get; }
    
    public bool ImplementsSecondaryScore { get; }
    
    public bool ImplementsScorePrediction { get; }
    
    public string PostGameInfo1Name { get; }
    
    public string PostGameInfo2Name { get; }
    
    public string PostGameInfo3Name { get; }

    public void Init();

    public void Complete();
}