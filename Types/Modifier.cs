using SMU.Utilities;

namespace SRXDScoreMod; 

/// <summary>
/// A score modifier
/// </summary>
public class Modifier {
    /// <summary>
    /// The unique index for the modifier. This value should be between 0 and 31, should not be used by another modifier in the same set, and should not be changed after the modifier is first introduced
    /// </summary>
    public int Index { get; }
    /// <summary>
    /// The percent score bonus to be added if this modifier is enabled
    /// </summary>
    public int Value { get; }
    /// <summary>
    /// True if the modifier should block score submission when enabled
    /// </summary>
    public bool BlocksSubmission { get; }
    /// <summary>
    /// Set to true when the modifier is enabled, and false when it is disabled
    /// </summary>
    public IReadOnlyBindable<bool> Enabled => EnabledInternal;
    
    internal Bindable<bool> EnabledInternal { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="index">The unique index for the modifier. This value should be between 0 and 31, should not be used by another modifier in the same set, and should not be changed after the modifier is first introduced</param>
    /// <param name="value">The percent score bonus to be added if this modifier is enabled</param>
    /// <param name="blocksSubmission">True if the modifier should block score submission when enabled</param>
    /// <param name="enabled">Set to true when the modifier is enabled, and false when it is disabled</param>
    public Modifier(int index, int value, bool blocksSubmission, Bindable<bool> enabled) {
        Index = index;
        Value = value;
        BlocksSubmission = blocksSubmission;
        EnabledInternal = enabled;
    }
}