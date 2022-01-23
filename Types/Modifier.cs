using SMU.Utilities;

namespace SRXDScoreMod; 

public class Modifier : IHashable {
    /// <summary>
    /// The name of the modifier
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The unique identifier for the modifier. This value should not be changed after the modifier is first introduced
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The fraction of the total score to be added if this modifier is enabled
    /// </summary>
    public float Value { get; }
    /// <summary>
    /// Set to true when the modifier is enabled, and false when it is disabled
    /// </summary>
    public IReadOnlyBindable<bool> Enabled => EnabledInternal;
    
    internal Bindable<bool> EnabledInternal { get; }

    public Modifier(string name, string id, float value) {
        Name = name;
        Id = id;
        Value = value;
        EnabledInternal = new Bindable<bool>();
    }

    public int GetStableHash() => HashUtility.Combine(Id, Value);
}