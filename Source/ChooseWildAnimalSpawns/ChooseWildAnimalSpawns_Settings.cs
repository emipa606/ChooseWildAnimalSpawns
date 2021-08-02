using System.Collections.Generic;
using Verse;

namespace ChooseWildAnimalSpawns
{
    public class ChooseWildAnimalSpawns_Settings : ModSettings
    {
        public Dictionary<string, SaveableDictionary> CustomSpawnRates =
            new Dictionary<string, SaveableDictionary>();

        private List<string> customSpawnRatesKeys;

        private List<SaveableDictionary> customSpawnRatesValues;

        public bool VerboseLogging;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
            Scribe_Collections.Look(ref CustomSpawnRates, "CustomSpawnRates", LookMode.Value,
                LookMode.Value,
                ref customSpawnRatesKeys, ref customSpawnRatesValues);
        }

        public void ResetManualValues()
        {
            customSpawnRatesKeys = new List<string>();
            customSpawnRatesValues = new List<SaveableDictionary>();
            CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
            Main.ApplyBiomeSettings();
        }


        public void ResetOneValue(string BiomeDefName)
        {
            if (CustomSpawnRates.ContainsKey(BiomeDefName))
            {
                CustomSpawnRates.Remove(BiomeDefName);
            }

            Main.ApplyBiomeSettings();
        }
    }
}