using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseWildAnimalSpawns.Settings;

public class ChooseWildAnimalSpawns_Mod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ChooseWildAnimalSpawns_Mod instance;

    private static readonly Vector2 buttonSize = new Vector2(120f, 25f);

    private static readonly Vector2 searchSize = new Vector2(200f, 25f);

    private static readonly Vector2 iconSize = new Vector2(48f, 48f);

    private static readonly int buttonSpacer = 200;

    private static readonly float columnSpacer = 0.1f;

    private static float leftSideWidth;

    private static Listing_Standard listing_Standard;

    private static Vector2 tabsScrollPosition;

    private static string currentVersion;

    private static Vector2 scrollPosition;

    private static Dictionary<PawnKindDef, float> currentBiomeAnimalRecords;
    private static Dictionary<PawnKindDef, int> currentBiomeAnimalDecimals;

    private static Dictionary<BiomeDef, float> currentAnimalBiomeRecords;
    private static Dictionary<BiomeDef, int> currentAnimalBiomeDecimals;

    private static float currentBiomeAnimalDensity;

    private static string selectedDef = "Settings";

    private static string searchText = "";

    private static bool aaWarningShown;

    private static float globalValue;

    private static readonly Color alternateBackground = new Color(0.1f, 0.1f, 0.1f, 0.5f);


    /// <summary>
    ///     The private settings
    /// </summary>
    private ChooseWildAnimalSpawns_Settings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ChooseWildAnimalSpawns_Mod(ModContentPack content)
        : base(content)
    {
        instance = this;
        ParseHelper.Parsers<SaveableDictionary>.Register(SaveableDictionary.FromString);
        if (instance.Settings.CustomSpawnRates == null)
        {
            instance.Settings.CustomSpawnRates = new Dictionary<string, SaveableDictionary>();
        }

        if (instance.Settings.CustomDensities == null)
        {
            instance.Settings.CustomDensities = new Dictionary<string, float>();
        }

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.ChooseWildAnimalSpawns"));
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ChooseWildAnimalSpawns_Settings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = GetSettings<ChooseWildAnimalSpawns_Settings>();
            }

            return settings;
        }

        set => settings = value;
    }

    private static string SelectedDef
    {
        get => selectedDef;
        set
        {
            if (selectedDef != null && selectedDef != "Settings")
            {
                saveBiomeSettings();
                Main.ApplyBiomeSettings();
            }

            currentAnimalBiomeRecords = new Dictionary<BiomeDef, float>();
            currentAnimalBiomeDecimals = new Dictionary<BiomeDef, int>();
            currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
            currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
            currentBiomeAnimalDensity = 0;
            selectedDef = value;
            if (value is null or "Settings")
            {
                return;
            }

            Traverse cachedCommonailtiesTraverse;
            Dictionary<PawnKindDef, float> cachedAnimalCommonalities;
            if (instance.Settings.ReverseSettingsMode)
            {
                var selectedAnimalDef = PawnKindDef.Named(selectedDef);
                foreach (var biomeDef in Main.AllBiomes)
                {
                    cachedCommonailtiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedAnimalCommonalities");
                    if (cachedCommonailtiesTraverse.GetValue() == null)
                    {
                        var unused = biomeDef.CommonalityOfAnimal(selectedAnimalDef);
                    }

                    cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                    if (!cachedAnimalCommonalities.TryGetValue(selectedAnimalDef,
                            out var commonality))
                    {
                        commonality = 0f;
                    }

                    currentAnimalBiomeRecords[biomeDef] = commonality;
                    var decimals =
                        (currentAnimalBiomeRecords[biomeDef] -
                         Math.Truncate(currentAnimalBiomeRecords[biomeDef]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentAnimalBiomeDecimals[biomeDef] = decimals;
                }

                return;
            }

            var selectedBiome = BiomeDef.Named(selectedDef);
            currentBiomeAnimalDensity = selectedBiome.animalDensity;
            cachedCommonailtiesTraverse = Traverse.Create(selectedBiome).Field("cachedAnimalCommonalities");
            if (cachedCommonailtiesTraverse.GetValue() == null)
            {
                var unused = selectedBiome.CommonalityOfAnimal(Main.AllAnimals.First());
            }

            cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

            foreach (var animal in Main.AllAnimals)
            {
                if (!cachedAnimalCommonalities.TryGetValue(animal, out var commonality))
                {
                    commonality = 0f;
                }

                currentBiomeAnimalRecords[animal] = commonality;
                var decimals =
                    (currentBiomeAnimalRecords[animal] - Math.Truncate(currentBiomeAnimalRecords[animal]))
                    .ToString().Length;

                if (decimals < 4)
                {
                    decimals = 4;
                }

                currentBiomeAnimalDecimals[animal] = decimals;
            }
        }
    }

    public override void WriteSettings()
    {
        saveBiomeSettings();
        base.WriteSettings();
        Main.ApplyBiomeSettings();
        SelectedDef = "Settings";
    }

    private static void saveBiomeSettings()
    {
        if (SelectedDef == "Settings")
        {
            return;
        }

        if (instance.Settings.ReverseSettingsMode)
        {
            SaveAnimalSetting();
            return;
        }

        SaveABiomeSetting(SelectedDef);
    }

    private static void SaveAnimalSetting()
    {
        try
        {
            var animal = PawnKindDef.Named(SelectedDef);
            if (currentAnimalBiomeRecords == null)
            {
                currentAnimalBiomeRecords = new Dictionary<BiomeDef, float>();
                currentAnimalBiomeDecimals = new Dictionary<BiomeDef, int>();
                Main.LogMessage($"currentAnimalBiomeRecords null for {SelectedDef}");
                return;
            }

            if (!currentAnimalBiomeRecords.Any())
            {
                Main.LogMessage($"currentAnimalBiomeRecords for {SelectedDef} empty");
                return;
            }

            foreach (var biomeDef in Main.AllBiomes)
            {
                var biomeDefName = biomeDef.defName;

                if (!Main.VanillaSpawnRates.ContainsKey(biomeDefName))
                {
                    Main.LogMessage($"VanillaSpawnRates not contain {biomeDefName}");
                    continue;
                }

                var vanillaValue = Main.VanillaSpawnRates[biomeDefName]
                    .FirstOrFallback(record => record.animal == animal);
                if (vanillaValue != null && vanillaValue.commonality.ToString() ==
                    currentAnimalBiomeRecords[biomeDef].ToString())
                {
                    if (instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName) && instance.Settings
                            .CustomSpawnRates[biomeDefName].dictionary.ContainsKey(SelectedDef))
                    {
                        instance.Settings.CustomSpawnRates[biomeDefName].dictionary.Remove(SelectedDef);
                        if (!instance.Settings.CustomSpawnRates[biomeDefName].dictionary.Any())
                        {
                            Main.LogMessage($"currentBiomeList for {biomeDefName} empty");
                            instance.Settings.CustomSpawnRates.Remove(biomeDefName);
                        }
                    }

                    continue;
                }

                if (vanillaValue == null && currentAnimalBiomeRecords[biomeDef] == 0)
                {
                    continue;
                }

                Main.LogMessage(
                    $"{animal.label} in {biomeDefName}: chosen value {currentAnimalBiomeRecords[biomeDef]}, vanilla value {vanillaValue?.commonality}");

                if (!instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName))
                {
                    instance.Settings.CustomSpawnRates[biomeDefName] = new SaveableDictionary();
                }

                instance.Settings.CustomSpawnRates[biomeDefName].dictionary[SelectedDef] =
                    currentAnimalBiomeRecords[biomeDef];
            }

            currentAnimalBiomeRecords = new Dictionary<BiomeDef, float>();
            currentAnimalBiomeDecimals = new Dictionary<BiomeDef, int>();
        }
        catch (Exception exception)
        {
            Main.LogMessage($"Failed to save settings for {SelectedDef}, {exception}", true, true);
        }
    }

    private static void SaveABiomeSetting(string biomeDefName)
    {
        try
        {
            if (currentBiomeAnimalDensity == Main.VanillaDensities[biomeDefName])
            {
                if (instance.Settings.CustomDensities?.ContainsKey(biomeDefName) == true)
                {
                    instance.Settings.CustomDensities.Remove(biomeDefName);
                }
            }
            else
            {
                instance.Settings.CustomDensities[biomeDefName] = currentBiomeAnimalDensity;
            }

            if (currentBiomeAnimalRecords == null)
            {
                currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
                currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
                Main.LogMessage($"currentBiomeAnimalRecords null for {biomeDefName}");
                return;
            }

            if (!currentBiomeAnimalRecords.Any())
            {
                Main.LogMessage($"currentBiomeAnimalRecords for {biomeDefName} empty");
                return;
            }

            if (!Main.VanillaSpawnRates.ContainsKey(biomeDefName))
            {
                Main.LogMessage($"VanillaSpawnRates not contain {biomeDefName}");
                currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
                currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
                return;
            }

            var currentBiomeList = new Dictionary<string, float>();
            foreach (var animal in Main.AllAnimals)
            {
                var vanillaValue = Main.VanillaSpawnRates[biomeDefName]
                    .FirstOrFallback(record => record.animal == animal);
                if (vanillaValue != null && vanillaValue.commonality.ToString() ==
                    currentBiomeAnimalRecords[animal].ToString())
                {
                    continue;
                }

                if (vanillaValue == null && currentBiomeAnimalRecords[animal] == 0)
                {
                    continue;
                }

                Main.LogMessage(
                    $"{animal.label}: chosen value {currentBiomeAnimalRecords[animal]}, vanilla value {vanillaValue?.commonality}");
                currentBiomeList.Add(animal.defName, currentBiomeAnimalRecords[animal]);
            }

            if (!currentBiomeList.Any())
            {
                if (instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName))
                {
                    instance.Settings.CustomSpawnRates.Remove(biomeDefName);
                }

                currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
                currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
                Main.LogMessage($"currentBiomeList for {biomeDefName} empty");
                return;
            }

            instance.Settings.CustomSpawnRates[biomeDefName] = new SaveableDictionary(currentBiomeList);
            currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
            currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
        }
        catch (Exception exception)
        {
            Main.LogMessage($"Failed to save settings for {biomeDefName}, {exception}", true, true);
        }
    }

    /// <summary>
    ///     The settings-window
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        base.DoSettingsWindowContents(rect);

        var rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 4;

        listing_Standard = new Listing_Standard();

        DrawOptions(rect2);
        DrawTabsList(rect2);
        Settings.Write();
        if (aaWarningShown || ModLister.GetActiveModWithIdentifier("sarg.alphaanimals") == null)
        {
            return;
        }

        Find.WindowStack.Add(new Dialog_MessageBox(
            "CWAS.aaWarning".Translate(),
            "CWAS.ok.button".Translate()));
        aaWarningShown = true;
    }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Choose Wild Animal Spawns";
    }


    private static void DrawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }


    private void DrawIcon(PawnKindDef pawnKind, Rect rect)
    {
        var texture2D = pawnKind?.lifeStages?.Last()?.bodyGraphicData?.Graphic?.MatSingle?.mainTexture;

        if (texture2D == null)
        {
            return;
        }

        var toolTip = $"{pawnKind.LabelCap}\n{pawnKind.race?.description}";
        if (texture2D.width != texture2D.height)
        {
            var ratio = (float)texture2D.width / texture2D.height;

            if (ratio < 1)
            {
                rect.x += (rect.width - (rect.width * ratio)) / 2;
                rect.width *= ratio;
            }
            else
            {
                rect.y += (rect.height - (rect.height / ratio)) / 2;
                rect.height /= ratio;
            }
        }

        GUI.DrawTexture(rect, texture2D);
        TooltipHandler.TipRegion(rect, toolTip);
    }

    private void DrawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + columnSpacer;
        optionsOuterContainer.width -= leftSideWidth + columnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + columnSpacer + 20;
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        switch (SelectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                listing_Standard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                listing_Standard.Label("CWAS.settings".Translate());
                Text.Font = GameFont.Small;
                listing_Standard.Gap();

                if (instance.Settings.CustomSpawnRates?.Any() == true ||
                    instance.Settings.CustomDensities?.Any() == true)
                {
                    var labelPoint = listing_Standard.Label("CWAS.resetall.label".Translate(), -1F,
                        "CWAS.resetall.tooltip".Translate());
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWAS.resetall.confirm".Translate(),
                                delegate { instance.Settings.ResetManualValues(); }));
                        }, "CWAS.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                }

                listing_Standard.CheckboxLabeled("CWAS.reversemode.label".Translate(),
                    ref instance.Settings.ReverseSettingsMode,
                    "CWAS.reversemode.tooltip".Translate());
                listing_Standard.CheckboxLabeled("CWAS.logging.label".Translate(), ref Settings.VerboseLogging,
                    "CWAS.logging.tooltip".Translate());
                if (currentVersion != null)
                {
                    listing_Standard.Gap();
                    GUI.contentColor = Color.gray;
                    listing_Standard.Label("CWAS.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listing_Standard.End();
                break;
            }

            default:
            {
                BiomeDef currentBiomeDef = null;
                PawnKindDef currentAnimalDef = null;
                string description;
                Rect headerLabel;
                listing_Standard.Begin(frameRect);
                if (instance.Settings.ReverseSettingsMode)
                {
                    currentAnimalDef = PawnKindDef.Named(SelectedDef);
                    if (currentAnimalDef == null)
                    {
                        listing_Standard.End();
                        break;
                    }

                    description = currentAnimalDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentAnimalDef.defName;
                    }

                    headerLabel = listing_Standard.Label(currentAnimalDef.label.CapitalizeFirst());
                }
                else
                {
                    currentBiomeDef = BiomeDef.Named(SelectedDef);
                    if (currentBiomeDef == null)
                    {
                        listing_Standard.End();
                        break;
                    }

                    description = currentBiomeDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentBiomeDef.defName;
                    }

                    headerLabel = listing_Standard.Label(currentBiomeDef.label.CapitalizeFirst());
                }


                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position,
                    searchSize), description);


                searchText =
                    Widgets.TextField(
                        new Rect(
                            headerLabel.position +
                            new Vector2((frameRect.width / 2) - (searchSize.x / 2) - (buttonSize.x / 2), 0),
                            searchSize),
                        searchText);
                TooltipHandler.TipRegion(new Rect(
                    headerLabel.position + new Vector2((frameRect.width / 2) - (searchSize.x / 2), 0),
                    searchSize), "CWAS.search".Translate());

                Rect borderRect;
                Rect scrollContentRect;
                Listing_Standard scrollListing;
                bool alternate;
                float currentGlobal;
                bool forceGlobal;
                if (instance.Settings.ReverseSettingsMode)
                {
                    if (instance.Settings.CustomSpawnRates?.Any(
                            pair => pair.Value.dictionary.ContainsKey(SelectedDef)) == true)
                    {
                        DrawButton(() =>
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "CWAS.resetone.confirm".Translate(currentAnimalDef.LabelCap),
                                    delegate
                                    {
                                        instance.Settings.ResetOneAnimal(SelectedDef);
                                        var selectedAnimal = PawnKindDef.Named(SelectedDef);
                                        foreach (var biomeDef in Main.AllBiomes)
                                        {
                                            var cachedCommonailtiesTraverse = Traverse.Create(biomeDef)
                                                .Field("cachedAnimalCommonalities");
                                            if (cachedCommonailtiesTraverse.GetValue() == null)
                                            {
                                                var unused = biomeDef.CommonalityOfAnimal(selectedAnimal);
                                            }

                                            var cachedAnimalCommonalities =
                                                (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                                            if (!cachedAnimalCommonalities.TryGetValue(selectedAnimal,
                                                    out var commonality))
                                            {
                                                commonality = 0f;
                                            }

                                            currentAnimalBiomeRecords[biomeDef] = commonality;
                                            var decimals =
                                                (currentAnimalBiomeRecords[biomeDef] -
                                                 Math.Truncate(currentAnimalBiomeRecords[biomeDef]))
                                                .ToString().Length;

                                            if (decimals < 4)
                                            {
                                                decimals = 4;
                                            }

                                            currentAnimalBiomeDecimals[biomeDef] = decimals;
                                        }
                                    }));
                            }, "CWAS.reset.button".Translate(),
                            new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                                headerLabel.position.y));
                    }

                    DrawButton(delegate { CopyOtherAnimalValues(SelectedDef); }, "CWAS.copy.button".Translate(),
                        headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));

                    listing_Standard.End();
                    var biomes = Main.AllBiomes;
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        biomes = Main.AllBiomes.Where(def =>
                                def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name.ToLower()
                                    .Contains(searchText.ToLower()) == true)
                            .ToList();
                    }

                    borderRect = frameRect;
                    borderRect.y += headerLabel.y + 30;
                    borderRect.height -= headerLabel.y + 30;
                    scrollContentRect = frameRect;
                    scrollContentRect.height = biomes.Count * 51f;
                    scrollContentRect.width -= 20;
                    scrollContentRect.x = 0;
                    scrollContentRect.y = 0;

                    scrollListing = new Listing_Standard();
                    Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                    scrollListing.Begin(scrollContentRect);

                    alternate = true;
                    currentGlobal = globalValue;
                    globalValue =
                        (float)Math.Round((decimal)Widgets.HorizontalSlider(
                            scrollListing.GetRect(50),
                            globalValue, 0,
                            3f, false,
                            globalValue.ToString("N4")
                                .TrimEnd('0').TrimEnd('.'),
                            "CWAS.globalvalue".Translate()), 4);
                    forceGlobal = currentGlobal != globalValue;

                    foreach (var biomeDef in biomes)
                    {
                        if (forceGlobal)
                        {
                            currentAnimalBiomeRecords[biomeDef] = globalValue;
                        }

                        var modInfo = biomeDef.modContentPack?.Name;
                        var rowRect = scrollListing.GetRect(50);
                        alternate = !alternate;
                        if (alternate)
                        {
                            Widgets.DrawBoxSolid(rowRect.ExpandedBy(10, 0), alternateBackground);
                        }

                        var biomeTitle = biomeDef.label.CapitalizeFirst();
                        if (biomeTitle.Length > 30)
                        {
                            biomeTitle = $"{biomeTitle.Substring(0, 27)}...";
                        }

                        if (modInfo is { Length: > 30 })
                        {
                            modInfo = $"{modInfo.Substring(0, 27)}...";
                        }

                        if (instance.Settings.CustomSpawnRates != null && instance.Settings
                                .CustomSpawnRates.ContainsKey(biomeDef.defName) && instance.Settings
                                .CustomSpawnRates[biomeDef.defName].dictionary?.ContainsKey(SelectedDef) == true)
                        {
                            GUI.color = Color.green;
                        }

                        currentAnimalBiomeRecords[biomeDef] =
                            (float)Math.Round((decimal)Widgets.HorizontalSlider(
                                rowRect,
                                currentAnimalBiomeRecords[biomeDef], 0,
                                3f, false,
                                currentAnimalBiomeRecords[biomeDef].ToString($"N{currentAnimalBiomeDecimals[biomeDef]}")
                                    .TrimEnd('0').TrimEnd('.'),
                                biomeTitle,
                                modInfo), currentAnimalBiomeDecimals[biomeDef]);
                        GUI.color = Color.white;
                    }

                    scrollListing.End();
                    Widgets.EndScrollView();
                    break;
                }

                if (instance.Settings.CustomSpawnRates?.ContainsKey(SelectedDef) == true ||
                    instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWAS.resetone.confirm".Translate(currentBiomeDef.LabelCap),
                                delegate
                                {
                                    instance.Settings.ResetOneBiome(SelectedDef);
                                    currentBiomeAnimalDensity = Main.VanillaDensities[SelectedDef];
                                    var selectedBiome = BiomeDef.Named(SelectedDef);
                                    var cachedCommonailtiesTraverse = Traverse.Create(selectedBiome)
                                        .Field("cachedAnimalCommonalities");
                                    if (cachedCommonailtiesTraverse.GetValue() == null)
                                    {
                                        var unused =
                                            selectedBiome.CommonalityOfAnimal(Main.AllAnimals.First());
                                    }

                                    var cachedAnimalCommonalities =
                                        (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                                    foreach (var animal in Main.AllAnimals)
                                    {
                                        if (!cachedAnimalCommonalities.TryGetValue(animal, out var commonality))
                                        {
                                            commonality = 0f;
                                        }

                                        currentBiomeAnimalRecords[animal] = commonality;
                                        var decimals =
                                            (currentBiomeAnimalRecords[animal] -
                                             Math.Truncate(currentBiomeAnimalRecords[animal]))
                                            .ToString().Length;

                                        if (decimals < 4)
                                        {
                                            decimals = 4;
                                        }

                                        currentBiomeAnimalDecimals[animal] = decimals;
                                    }
                                }));
                        }, "CWAS.reset.button".Translate(),
                        new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                            headerLabel.position.y));
                }


                DrawButton(delegate { CopySpawnValues(SelectedDef); }, "CWAS.copy.button".Translate(),
                    headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));
                if (instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    GUI.color = Color.green;
                }

                listing_Standard.Gap();
                currentBiomeAnimalDensity =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        listing_Standard.GetRect(50),
                        currentBiomeAnimalDensity, 0,
                        6f, false,
                        currentBiomeAnimalDensity.ToString(),
                        "CWAS.density.label".Translate()), 3);
                GUI.color = Color.white;
                listing_Standard.End();

                var animals = Main.AllAnimals;
                if (!string.IsNullOrEmpty(searchText))
                {
                    animals = Main.AllAnimals.Where(def =>
                            def.label.ToLower().Contains(searchText.ToLower()) || def.modContentPack?.Name.ToLower()
                                .Contains(searchText.ToLower()) == true)
                        .ToList();
                }

                borderRect = frameRect;
                borderRect.y += headerLabel.y + 90;
                borderRect.height -= headerLabel.y + 90;
                scrollContentRect = frameRect;
                scrollContentRect.height = animals.Count * 51f;
                scrollContentRect.width -= 20;
                scrollContentRect.x = 0;
                scrollContentRect.y = 0;

                scrollListing = new Listing_Standard();
                Widgets.BeginScrollView(borderRect, ref scrollPosition, scrollContentRect);
                scrollListing.Begin(scrollContentRect);

                alternate = false;
                currentGlobal = globalValue;
                globalValue =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        scrollListing.GetRect(50),
                        globalValue, 0,
                        3f, false,
                        globalValue.ToString("N4")
                            .TrimEnd('0').TrimEnd('.'),
                        "CWAS.globalvalue".Translate()), 4);
                forceGlobal = currentGlobal != globalValue;
                foreach (var animal in animals)
                {
                    if (forceGlobal)
                    {
                        currentBiomeAnimalRecords[animal] = globalValue;
                    }

                    var modInfo = animal.modContentPack?.Name;
                    var rowRect = scrollListing.GetRect(50);
                    alternate = !alternate;
                    if (alternate)
                    {
                        Widgets.DrawBoxSolid(rowRect.ExpandedBy(10, 0), alternateBackground);
                    }

                    var sliderRect = new Rect(rowRect.position + new Vector2(iconSize.x, 0),
                        rowRect.size - new Vector2(iconSize.x, 0));
                    var animalTitle = animal.label.CapitalizeFirst();
                    if (animalTitle.Length > 30)
                    {
                        animalTitle = $"{animalTitle.Substring(0, 27)}...";
                    }

                    if (modInfo is { Length: > 30 })
                    {
                        modInfo = $"{modInfo.Substring(0, 27)}...";
                    }

                    if (instance.Settings.CustomSpawnRates != null &&
                        instance.Settings.CustomSpawnRates.ContainsKey(SelectedDef) && instance.Settings
                            .CustomSpawnRates[SelectedDef]?.dictionary?.ContainsKey(animal.defName) ==
                        true)
                    {
                        GUI.color = Color.green;
                    }

                    currentBiomeAnimalRecords[animal] =
                        (float)Math.Round((decimal)Widgets.HorizontalSlider(
                            sliderRect,
                            currentBiomeAnimalRecords[animal], 0,
                            3f, false,
                            currentBiomeAnimalRecords[animal].ToString($"N{currentBiomeAnimalDecimals[animal]}")
                                .TrimEnd('0').TrimEnd('.'),
                            animalTitle,
                            modInfo), currentBiomeAnimalDecimals[animal]);
                    GUI.color = Color.white;
                    DrawIcon(animal,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private static void CopySpawnValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var biome in Main.AllBiomes.Where(biomeDef => biomeDef.defName != originalDef))
        {
            void action()
            {
                Main.LogMessage($"Copying overall animal density from {biome.defName} to {originalDef}");
                currentBiomeAnimalDensity = Main.VanillaDensities[biome.defName];
                if (instance.Settings.CustomDensities.ContainsKey(biome.defName))
                {
                    currentBiomeAnimalDensity = instance.Settings.CustomDensities[biome.defName];
                }


                Main.LogMessage($"Fetching current animal spawnrates for {biome.defName}");
                var cachedCommonailtiesTraverse = Traverse.Create(biome)
                    .Field("cachedAnimalCommonalities");
                if (cachedCommonailtiesTraverse.GetValue() == null)
                {
                    var unused =
                        biome.CommonalityOfAnimal(Main.AllAnimals.First());
                }

                var cachedAnimalCommonalities =
                    (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                foreach (var animal in Main.AllAnimals)
                {
                    Main.LogMessage($"Setting spawnrate for {animal.defName}");
                    if (!cachedAnimalCommonalities.TryGetValue(animal, out var commonality))
                    {
                        commonality = 0f;
                    }

                    currentBiomeAnimalRecords[animal] = commonality;
                    var decimals =
                        (currentBiomeAnimalRecords[animal] -
                         Math.Truncate(currentBiomeAnimalRecords[animal]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentBiomeAnimalDecimals[animal] = decimals;
                }

                SelectedDef = originalDef;
            }

            list.Add(new FloatMenuOption(biome.LabelCap, action));
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void CopyOtherAnimalValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var animal in Main.AllAnimals.Where(pawnKindDef => pawnKindDef.defName != originalDef))
        {
            void action()
            {
                Main.LogMessage($"Setting spawnrate from {animal.defName}");

                foreach (var biomeDef in Main.AllBiomes)
                {
                    var cachedCommonailtiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedAnimalCommonalities");
                    if (cachedCommonailtiesTraverse.GetValue() == null)
                    {
                        var unused = biomeDef.CommonalityOfAnimal(animal);
                    }

                    var cachedAnimalCommonalities =
                        (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                    if (!cachedAnimalCommonalities.TryGetValue(animal, out var commonality))
                    {
                        commonality = 0f;
                    }

                    currentAnimalBiomeRecords[biomeDef] = commonality;
                    var decimals =
                        (currentAnimalBiomeRecords[biomeDef] -
                         Math.Truncate(currentAnimalBiomeRecords[biomeDef]))
                        .ToString().Length;

                    if (decimals < 4)
                    {
                        decimals = 4;
                    }

                    currentAnimalBiomeDecimals[biomeDef] = decimals;
                }

                SelectedDef = originalDef;
            }

            list.Add(new FloatMenuOption(animal.LabelCap, action));
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private void DrawTabsList(Rect rect)
    {
        var scrollContainer = rect.ContractedBy(10);
        scrollContainer.width = leftSideWidth;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        var innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        var tabFrameRect = innerContainer.ContractedBy(5);
        tabFrameRect.y += 15;
        tabFrameRect.height -= 15;
        var tabContentRect = tabFrameRect;
        tabContentRect.x = 0;
        tabContentRect.y = 0;
        tabContentRect.width -= 20;
        var allBiomes = Main.AllBiomes;
        var listAddition = 50;
        var height = allBiomes.Count;
        if (instance.Settings.ReverseSettingsMode)
        {
            height = Main.AllAnimals.Count;
        }

        tabContentRect.height = (height * 27f) + listAddition;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listing_Standard.Begin(tabContentRect);
        if (listing_Standard.ListItemSelectable("CWAS.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow, out _);
        if (instance.Settings.ReverseSettingsMode)
        {
            foreach (var animalDef in Main.AllAnimals)
            {
                var toolTip = string.Empty;
                if (instance.Settings.CustomSpawnRates?.Any(
                        pair => pair.Value.dictionary.ContainsKey(animalDef.defName)) == true)
                {
                    GUI.color = Color.green;
                    toolTip = "CWAS.customexists".Translate();
                }

                if (listing_Standard.ListItemSelectable(animalDef.label.CapitalizeFirst(), Color.yellow,
                        out _,
                        SelectedDef == animalDef.defName, false, toolTip))
                {
                    SelectedDef = SelectedDef == animalDef.defName ? null : animalDef.defName;
                }

                GUI.color = Color.white;
            }

            listing_Standard.End();
            Widgets.EndScrollView();
            return;
        }

        foreach (var biomeDef in allBiomes)
        {
            var toolTip = string.Empty;
            if (instance.Settings.CustomSpawnRates.ContainsKey(biomeDef.defName) ||
                instance.Settings.CustomDensities.ContainsKey(biomeDef.defName))
            {
                GUI.color = Color.green;
                toolTip = "CWAS.customexists".Translate();
            }

            if (listing_Standard.ListItemSelectable(biomeDef.label.CapitalizeFirst(), Color.yellow,
                    out _,
                    SelectedDef == biomeDef.defName, false, toolTip))
            {
                SelectedDef = SelectedDef == biomeDef.defName ? null : biomeDef.defName;
            }

            GUI.color = Color.white;
        }

        listing_Standard.End();
        Widgets.EndScrollView();
    }
}