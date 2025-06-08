using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ChooseWildAnimalSpawns;

public class ChooseWildAnimalSpawnsSettings : ModSettings
{
    public Dictionary<string, float> CustomDensities = new();
    private List<string> customDensitiesKeys;
    private List<float> customDensitiesValues;

    public Dictionary<string, SaveableDictionary> CustomSpawnRates = new();

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
        customSpawnRatesKeys = [];
        customSpawnRatesValues = [];
        CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
        customDensitiesKeys = [];
        customDensitiesValues = [];
        CustomDensities = new Dictionary<string, float>();
        Main.ApplyBiomeSettings();
    }


    public void ResetOneBiome(string biomeDefName)
    {
        CustomSpawnRates.Remove(biomeDefName);

        CustomDensities.Remove(biomeDefName);

        Main.ApplyBiomeSettings();
    }

    public void ResetOneAnimal(string animalKindDefName)
    {
        foreach (var savableDictionary in CustomSpawnRates.Where(saveableDictionary =>
                     saveableDictionary.Value.dictionary.ContainsKey(animalKindDefName)))
        {
            savableDictionary.Value.dictionary.Remove(animalKindDefName);
        }

        var emptySets = CustomSpawnRates.Where(pair => !pair.Value.dictionary.Any()).Select(pair => pair.Key).ToList();

        foreach (var emptySet in emptySets)
        {
            CustomSpawnRates.Remove(emptySet);
        }

        Main.ApplyBiomeSettings();
    }
}