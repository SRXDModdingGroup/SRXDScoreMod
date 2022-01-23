using System.Collections.Generic;
using System.Collections.ObjectModel;
using SMU.Utilities;

namespace SRXDScoreMod; 

public class ModifierSet {
    /// <summary>
    /// The name of the modifier set
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The unique identifier for the set. This value should not be changed after the modifier is first introduced
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The set of available modifiers
    /// </summary>
    public ReadOnlyDictionary<string, Modifier> Modifiers { get; }
    
    internal string Key { get; }

    public ModifierSet(string name, string id, params Modifier[] modifiers) {
        Name = name;
        Id = id;

        var modifiersDict = new Dictionary<string, Modifier>();

        foreach (var modifier in modifiers)
            modifiersDict.Add(modifier.Name, modifier);

        Modifiers = new ReadOnlyDictionary<string, Modifier>(modifiersDict);

        int hash = HashUtility.Combine(Id, modifiersDict.Values);
        
        unchecked {
            Key = $"{Id}_{(uint) hash:x8}";
        }
    }

    internal bool GetAnyEnabled() {
        foreach (var pair in Modifiers) {
            var modifier = pair.Value;

            if (modifier.Enabled.Value)
                return true;
        }

        return false;
    }

    internal float GetOverallMultiplier() {
        float multiplier = 1f;

        foreach (var pair in Modifiers) {
            var modifier = pair.Value;

            if (modifier.Enabled.Value)
                multiplier += modifier.Value;
        }

        return multiplier;
    }
}