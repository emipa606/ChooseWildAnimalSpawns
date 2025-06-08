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
    public static readonly Dictionary<string, List<BiomeAnimalRecord>> VanillaSpawnRates = new();

    public static readonly Dictionary<string, float> VanillaDensities = new();

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
                allAnimals = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Where(animal => animal.RaceProps?.Animal == true)
                    .OrderBy(animal => animal.label)
                    .ToList();
            }

            return allAnimals;
        }
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
        var customSpawnRates = ChooseWildAnimalSpawns_Mod.Instance.Settings.CustomSpawnRates;
        var customDensities = ChooseWildAnimalSpawns_Mod.Instance.Settings.CustomDensities;
        var biomes = AllBiomes;
        foreach (var biome in biomes)
        {
            var biomeAnimalList = new List<BiomeAnimalRecord>();
            var customBiomeDefs = new Dictionary<string, float>();
            if (customSpawnRates.TryGetValue(biome.defName, out var rate))
            {
                customBiomeDefs = rate.dictionary;
            }

            biome.animalDensity = customDensities?.TryGetValue(biome.defName, out var density) is true
                ? density
                : VanillaDensities[biome.defName];

            var vanillaBiomeDefs = new List<BiomeAnimalRecord>();
            if (VanillaSpawnRates.TryGetValue(biome.defName, out var spawnRate))
            {
                vanillaBiomeDefs = spawnRate;
            }

            var animals = AllAnimals;
            foreach (var pawnKindDef in animals)
            {
                if (customBiomeDefs.TryGetValue(pawnKindDef.defName, out var def))
                {
                    biomeAnimalList.Add(new BiomeAnimalRecord
                        { animal = pawnKindDef, commonality = def });
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
                VanillaSpawnRates[biome.defName] = [];
                continue;
            }

            var currentBiomeRecord = new List<BiomeAnimalRecord>();
            var cachedCommonailtiesTraverse = Traverse.Create(biome).Field("cachedAnimalCommonalities");
            if (cachedCommonailtiesTraverse.GetValue() == null)
            {
                _ = biome.CommonalityOfAnimal(AllAnimals.First());
            }

            var cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();
            foreach (var animal in biome.AllWildAnimals)
            {
                var commonality = cachedAnimalCommonalities.GetValueOrDefault(animal, 0f);

                currentBiomeRecord.Add(new BiomeAnimalRecord
                    { animal = animal, commonality = commonality });
            }

            VanillaSpawnRates[biome.defName] = currentBiomeRecord;
        }
    }

    public static void LogMessage(string message, bool forced = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[ChooseWildAnimalSpawns]: {message}");
            return;
        }

        if (!forced && !ChooseWildAnimalSpawns_Mod.Instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[ChooseWildAnimalSpawns]: {message}");
    }
}