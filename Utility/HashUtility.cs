using System;
using UnityEngine;

namespace SRXDScoreMod; 

public static class HashUtility {
    private const uint HASH_BIAS = 2166136261u;
    private const int HASH_COEFF = 486187739;
    
    public static int Combine(params object[] objects) {
        unchecked {
            int hash = (int) HASH_BIAS;

            foreach (object o in objects)
                hash = hash * HASH_COEFF ^ GetStableHash(o);

            return hash;
        }
    }

    public static int GetStableHash(bool b) => b ? 1 : 0;
    public static int GetStableHash(char c) => c | c << 16;
    public static unsafe int GetStableHash(float f) {
        if (f == 0.0)
            return 0;
        
        return *(int*) &f;
    }
    public static int GetStableHash(string s) {
        unchecked {
            int hash = (int) HASH_BIAS;
        
            foreach (char c in s)
                hash = hash * HASH_COEFF ^ GetStableHash(c);

            return hash;
        }
    }
    public static int GetStableHash(Color c) {
        unchecked {
            int hash = (int) HASH_BIAS;
        
            hash = hash * HASH_COEFF ^ GetStableHash(c.a);
            hash = hash * HASH_COEFF ^ GetStableHash(c.r);
            hash = hash * HASH_COEFF ^ GetStableHash(c.g);
            hash = hash * HASH_COEFF ^ GetStableHash(c.b);

            return hash;
        }
    }
    public static int GetStableHash(Array a) {
        unchecked {
            int hash = (int) HASH_BIAS;

            foreach (object o in a)
                hash = hash * HASH_COEFF ^ GetStableHash(o);

            return hash;
        }
    }
    public static int GetStableHash(object o) => o switch {
        bool b => GetStableHash(b),
        char c => GetStableHash(c),
        int i => i,
        float f => GetStableHash(f),
        string s => GetStableHash(s),
        Color c => GetStableHash(c),
        IHashable h => h.GetStableHash(),
        Array a => GetStableHash(a),
        _ => throw new ArgumentException("GetStableHash can only be called on objects of type bool, char, int, float, string, Color, IHashable, or Array")
    };
}