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
        var returnValue = string.Empty;

        foreach (var keyValuePair in dictionary)
        {
            returnValue += $"#{keyValuePair.Key}:{keyValuePair.Value}";
        }

        return returnValue;
    }

    public static SaveableDictionary FromString(string dictionaryString)
    {
        dictionaryString = dictionaryString.TrimStart('#');
        var array = dictionaryString.Split('#');
        var returnValue = new Dictionary<string, float>();
        foreach (var s in array)
        {
            var parts = s.Split(':');
            if (parts.Length == 2 && float.TryParse(parts[1], out var value))
            {
                returnValue[parts[0]] = value;
            }
        }

        return new SaveableDictionary(returnValue);
    }
}