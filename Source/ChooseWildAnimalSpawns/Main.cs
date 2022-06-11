using System.Collections.Generic;
using System.Linq;
using ChooseWildAnimalSpawns.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ChooseWildAnimalSpawns;

[StaticConstructorOnStartup]
public static class Main
{
    public static readonly Dictionary<string, List<BiomeAnimalRecord>> VanillaSpawnRates =
        new Dictionary<string, List<BiomeAnimalRecord>>();

    public static readonly Dictionary<string, float> VanillaDensities = new Dictionary<string, float>();

    private static List<PawnKindDef> allAnimals;
    private static List<BiomeDef> allBiomes;

    static Main()
    {
        saveVanillaValues();
        clearPawnKindDefs();
        ApplyBiomeSettings();
    }

    public static List<PawnKindDef> AllAnimals
    {
        get
        {
            if (allAnimals == null || allAnimals.Count == 0)
            {
                allAnimals = (from animal in DefDatabase<PawnKindDef>.AllDefsListForReading
                    where animal.RaceProps?.Animal == true
                    orderby animal.label
                    select animal).ToList();
            }

            return allAnimals;
        }
        set => allAnimals = value;
    }

    public static List<BiomeDef> AllBiomes
    {
        get
        {
            if (allBiomes == null || allBiomes.Count == 0)
            {
                allBiomes = (from biome in DefDatabase<BiomeDef>.AllDefsListForReading
                    orderby biome.label
                    select biome).ToList();
            }

            return allBiomes;
        }
        set => allBiomes = value;
    }

    private static void clearPawnKindDefs()
    {
        foreach (var pawnKindDef in DefDatabase<PawnKindDef>.AllDefs)
        {
            if (pawnKindDef.RaceProps.wildBiomes == null)
            {
                continue;
            }

            pawnKindDef.RaceProps.wildBiomes = null;
        }
    }

    public static void ApplyBiomeSettings()
    {
        var costumSpawnRates = ChooseWildAnimalSpawns_Mod.instance.Settings.CustomSpawnRates;
        var customDensities = ChooseWildAnimalSpawns_Mod.instance.Settings.CustomDensities;
        foreach (var biome in AllBiomes)
        {
            var biomeAnimalList = new List<BiomeAnimalRecord>();
            var customBiomeDefs = new Dictionary<string, float>();
            if (costumSpawnRates.ContainsKey(biome.defName))
            {
                customBiomeDefs = costumSpawnRates[biome.defName].dictionary;
            }

            biome.animalDensity = customDensities?.ContainsKey(biome.defName) == true
                ? customDensities[biome.defName]
                : VanillaDensities[biome.defName];

            var vanillaBiomeDefs = new List<BiomeAnimalRecord>();
            if (VanillaSpawnRates.ContainsKey(biome.defName))
            {
                vanillaBiomeDefs = VanillaSpawnRates[biome.defName];
            }

            foreach (var pawnKindDef in AllAnimals)
            {
                if (customBiomeDefs.ContainsKey(pawnKindDef.defName))
                {
                    biomeAnimalList.Add(new BiomeAnimalRecord
                        { animal = pawnKindDef, commonality = customBiomeDefs[pawnKindDef.defName] });
                    continue;
                }

                if (vanillaBiomeDefs.Any(record => record.animal == pawnKindDef))
                {
                    biomeAnimalList.Add(vanillaBiomeDefs.First(record => record.animal == pawnKindDef));
                    continue;
                }

                biomeAnimalList.Add(new BiomeAnimalRecord { animal = pawnKindDef, commonality = 0 });
            }

            Traverse.Create(biome).Field("wildAnimals").SetValue(biomeAnimalList);

            Traverse.Create(biome).Field("cachedAnimalCommonalities").SetValue(null);
        }
    }

    private static void saveVanillaValues()
    {
        foreach (var biome in AllBiomes)
        {
            VanillaDensities[biome.defName] = biome.animalDensity;
            var allWildAnimalsInBiome = biome.AllWildAnimals;
            if (!allWildAnimalsInBiome.Any())
            {
                VanillaSpawnRates[biome.defName] = new List<BiomeAnimalRecord>();
                continue;
            }

            var currentBiomeRecord = new List<BiomeAnimalRecord>();
            var cachedCommonailtiesTraverse = Traverse.Create(biome).Field("cachedAnimalCommonalities");
            if (cachedCommonailtiesTraverse.GetValue() == null)
            {
                var unused = biome.CommonalityOfAnimal(AllAnimals.First());
            }

            var cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();
            foreach (var animal in biome.AllWildAnimals)
            {
                if (!cachedAnimalCommonalities.TryGetValue(animal, out var commonality))
                {
                    commonality = 0f;
                }

                currentBiomeRecord.Add(new BiomeAnimalRecord
                    { animal = animal, commonality = commonality });
            }

            VanillaSpawnRates[biome.defName] = currentBiomeRecord;
        }
    }

    public static void ResetToVanillaRates()
    {
        foreach (var biome in AllBiomes)
        {
            biome.animalDensity = VanillaDensities[biome.defName];
            if (!biome.AllWildAnimals.Any() && !VanillaSpawnRates.ContainsKey(biome.defName))
            {
                continue;
            }

            Traverse.Create(biome).Field("wildAnimals").SetValue(!VanillaSpawnRates.ContainsKey(biome.defName)
                ? new List<BiomeAnimalRecord>()
                : VanillaSpawnRates[biome.defName]);

            Traverse.Create(biome).Field("cachedAnimalCommonalities").SetValue(null);
        }
    }

    public static void LogMessage(string message, bool forced = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[ChooseWildAnimalSpawns]: {message}");
            return;
        }

        if (!forced && !ChooseWildAnimalSpawns_Mod.instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[ChooseWildAnimalSpawns]: {message}");
    }
}