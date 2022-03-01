using UnityEngine;

namespace SRXDScoreMod; 

/// <summary>
/// Contains information about a timing accuracy
/// </summary>
public class CustomTimingAccuracy {
    /// <summary>
    /// The name to display on the timing accuracy animation
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// The name to display in the post game stats
    /// </summary>
    public string StatsName { get; }
    
    /// <summary>
    /// The color of the timing accuracy animation
    /// </summary>
    public Color Color { get; }
    
    /// <summary>
    /// The base game accuracy type that this accuracy type represents
    /// </summary>
    public NoteTimingAccuracy BaseAccuracy { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="displayName">The name to display on the timing accuracy animation</param>
    /// <param name="statsName">The name to display in the post game stats</param>
    /// <param name="color">The color of the timing accuracy animation</param>
    /// <param name="baseAccuracy">The base game accuracy type that this accuracy type represents</param>
    public CustomTimingAccuracy(string displayName, string statsName, Color color, NoteTimingAccuracy baseAccuracy) {
        DisplayName = displayName;
        StatsName = statsName;
        Color = color;
        BaseAccuracy = baseAccuracy;
    }
}