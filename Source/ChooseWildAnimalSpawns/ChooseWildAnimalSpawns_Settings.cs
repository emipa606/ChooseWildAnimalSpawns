using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ChooseWildAnimalSpawns;

public class ChooseWildAnimalSpawns_Settings : ModSettings
{
    public Dictionary<string, float> CustomDensities = new Dictionary<string, float>();
    private List<string> customDensitiesKeys;
    private List<float> customDensitiesValues;

    public Dictionary<string, SaveableDictionary> CustomSpawnRates =
        new Dictionary<string, SaveableDictionary>();

    private List<string> customSpawnRatesKeys;

    private List<SaveableDictionary> customSpawnRatesValues;
    public bool ReverseSettingsMode;

    public bool VerboseLogging;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
        Scribe_Values.Look(ref ReverseSettingsMode, "ReverseSettingsMode");
        Scribe_Collections.Look(ref CustomSpawnRates, "CustomSpawnRates", LookMode.Value,
            LookMode.Value,
            ref customSpawnRatesKeys, ref customSpawnRatesValues);
        Scribe_Collections.Look(ref CustomDensities, "CustomDensities", LookMode.Value,
            LookMode.Value,
            ref customDensitiesKeys, ref customDensitiesValues);
    }

    public void ResetManualValues()
    {
        customSpawnRatesKeys = new List<string>();
        customSpawnRatesValues = new List<SaveableDictionary>();
        CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
        customDensitiesKeys = new List<string>();
        customDensitiesValues = new List<float>();
        CustomDensities = new Dictionary<string, float>();
        Main.ApplyBiomeSettings();
    }


    public void ResetOneBiome(string biomeDefName)
    {
        if (CustomSpawnRates.ContainsKey(biomeDefName))
        {
            CustomSpawnRates.Remove(biomeDefName);
        }

        if (CustomDensities.ContainsKey(biomeDefName))
        {
            CustomDensities.Remove(biomeDefName);
        }

        Main.ApplyBiomeSettings();
    }

    public void ResetOneAnimal(string animalKindDefName)
    {
        foreach (var saveableDictionary in CustomSpawnRates.Where(saveableDictionary =>
                     saveableDictionary.Value.dictionary.ContainsKey(animalKindDefName)))
        {
            saveableDictionary.Value.dictionary.Remove(animalKindDefName);
        }

        var emptySets = CustomSpawnRates.Where(pair => !pair.Value.dictionary.Any()).Select(pair => pair.Key).ToList();

        foreach (var emptySet in emptySets)
        {
            CustomSpawnRates.Remove(emptySet);
        }

        Main.ApplyBiomeSettings();
    }
}