using SMU.Utilities;

namespace SRXDScoreMod; 

public class Modifier {
    /// <summary>
    /// The name of the modifier
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The unique index for the modifier. This value should be between 0 and 31, should not be used by another modifier in the same set, and should not be changed after the modifier is first introduced
    /// </summary>
    public int Index { get; }
    /// <summary>
    /// The fraction of the total score to be added if this modifier is enabled
    /// </summary>
    public float Value { get; }
    /// <summary>
    /// Set to true when the modifier is enabled, and false when it is disabled
    /// </summary>
    public IReadOnlyBindable<bool> Enabled => EnabledInternal;
    
    internal Bindable<bool> EnabledInternal { get; }

    public Modifier(string name, int index, float value) {
        Name = name;
        Index = index;
        Value = value;
        EnabledInternal = new Bindable<bool>(false);
    }
}