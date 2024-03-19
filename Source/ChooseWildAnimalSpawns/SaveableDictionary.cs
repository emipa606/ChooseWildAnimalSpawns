using System;
using System.Collections.Generic;

namespace ChooseWildAnimalSpawns;

public class SaveableDictionary(Dictionary<string, float> dictionary)
{
    public SaveableDictionary() : this(new Dictionary<string, float>())
    {
    }

    public Dictionary<string, float> dictionary { get; } = dictionary;

    public override string ToString()
    {
        var returnvalue = string.Empty;

        foreach (var keyValuePair in dictionary)
        {
            returnvalue += $"#{keyValuePair.Key}:{keyValuePair.Value}";
        }

        return returnvalue;
    }

    public static SaveableDictionary FromString(string Str)
    {
        Str = Str.TrimStart('#');
        var array = Str.Split('#');
        var returnValue = new Dictionary<string, float>();
        foreach (var s in array)
        {
            returnValue[s.Split(':')[0]] = Convert.ToSingle(s.Split(':')[1]);
        }

        return new SaveableDictionary(returnValue);
    }
}