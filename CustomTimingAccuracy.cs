namespace SRXDScoreMod; 

public class CustomTimingAccuracy {
    public string Text { get; }
    
    public NoteTimingAccuracy BaseAccuracy { get; }

    public CustomTimingAccuracy(string text, NoteTimingAccuracy baseAccuracy) {
        Text = text;
        BaseAccuracy = baseAccuracy;
    }
}