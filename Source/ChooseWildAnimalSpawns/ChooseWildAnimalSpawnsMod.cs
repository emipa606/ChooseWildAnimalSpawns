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

public class ChooseWildAnimalSpawnsMod : Mod
{
    private const int ButtonSpacer = 200;

    private const float ColumnSpacer = 0.1f;

    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ChooseWildAnimalSpawnsMod Instance;

    private static readonly Vector2 buttonSize = new(120f, 25f);

    private static readonly Vector2 searchSize = new(200f, 25f);

    private static readonly Vector2 iconSize = new(48f, 48f);

    private static float leftSideWidth;

    private static Listing_Standard listingStandard;

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

    private static readonly Color alternateBackground = new(0.1f, 0.1f, 0.1f, 0.5f);


    /// <summary>
    ///     The private settings
    /// </summary>
    private ChooseWildAnimalSpawnsSettings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ChooseWildAnimalSpawnsMod(ModContentPack content)
        : base(content)
    {
        Instance = this;
        ParseHelper.Parsers<SaveableDictionary>.Register(SaveableDictionary.FromString);
        Instance.Settings.CustomSpawnRates ??= new Dictionary<string, SaveableDictionary>();

        Instance.Settings.CustomDensities ??= new Dictionary<string, float>();

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ChooseWildAnimalSpawnsSettings Settings
    {
        get
        {
            settings ??= GetSettings<ChooseWildAnimalSpawnsSettings>();

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

            Traverse cachedCommonalitiesTraverse;
            Dictionary<PawnKindDef, float> cachedAnimalCommonalities;
            if (Instance.Settings.ReverseSettingsMode)
            {
                var selectedAnimalDef = PawnKindDef.Named(selectedDef);
                foreach (var biomeDef in Main.AllBiomes)
                {
                    cachedCommonalitiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedAnimalCommonalities");
                    if (cachedCommonalitiesTraverse.GetValue() == null)
                    {
                        _ = biomeDef.CommonalityOfAnimal(selectedAnimalDef);
                    }

                    cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonalitiesTraverse.GetValue();

                    var commonality = cachedAnimalCommonalities.GetValueOrDefault(selectedAnimalDef, 0f);

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
            cachedCommonalitiesTraverse = Traverse.Create(selectedBiome).Field("cachedAnimalCommonalities");
            if (cachedCommonalitiesTraverse.GetValue() == null)
            {
                _ = selectedBiome.CommonalityOfAnimal(Main.AllAnimals.First());
            }

            cachedAnimalCommonalities = (Dictionary<PawnKindDef, float>)cachedCommonalitiesTraverse.GetValue();

            foreach (var animal in Main.AllAnimals)
            {
                var commonality = cachedAnimalCommonalities.GetValueOrDefault(animal, 0f);

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

        if (Instance.Settings.ReverseSettingsMode)
        {
            saveAnimalSetting();
            return;
        }

        saveABiomeSetting(SelectedDef);
    }

    private static void saveAnimalSetting()
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

                if (!Main.VanillaSpawnRates.TryGetValue(biomeDefName, out var rate))
                {
                    Main.LogMessage($"VanillaSpawnRates not contain {biomeDefName}");
                    continue;
                }

                var vanillaValue = rate
                    .FirstOrFallback(record => record.animal == animal);
                if (vanillaValue != null && vanillaValue.commonality.ToString() ==
                    currentAnimalBiomeRecords[biomeDef].ToString())
                {
                    if (Instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName) && Instance.Settings
                            .CustomSpawnRates[biomeDefName].dictionary.Remove(SelectedDef))
                    {
                        if (!Instance.Settings.CustomSpawnRates[biomeDefName].dictionary.Any())
                        {
                            Main.LogMessage($"currentBiomeList for {biomeDefName} empty");
                            Instance.Settings.CustomSpawnRates.Remove(biomeDefName);
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

                if (!Instance.Settings.CustomSpawnRates.ContainsKey(biomeDefName))
                {
                    Instance.Settings.CustomSpawnRates[biomeDefName] = new SaveableDictionary();
                }

                Instance.Settings.CustomSpawnRates[biomeDefName].dictionary[SelectedDef] =
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

    private static void saveABiomeSetting(string biomeDefName)
    {
        try
        {
            if (currentBiomeAnimalDensity == Main.VanillaDensities[biomeDefName])
            {
                Instance.Settings.CustomDensities?.Remove(biomeDefName);
            }
            else
            {
                Instance.Settings.CustomDensities[biomeDefName] = currentBiomeAnimalDensity;
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
                Instance.Settings.CustomSpawnRates.Remove(biomeDefName);

                currentBiomeAnimalRecords = new Dictionary<PawnKindDef, float>();
                currentBiomeAnimalDecimals = new Dictionary<PawnKindDef, int>();
                Main.LogMessage($"currentBiomeList for {biomeDefName} empty");
                return;
            }

            Instance.Settings.CustomSpawnRates[biomeDefName] = new SaveableDictionary(currentBiomeList);
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

        listingStandard = new Listing_Standard();

        drawOptions(rect2);
        DrawTabsList(rect2);
        Settings.Write();
        if (aaWarningShown || ModLister.GetActiveModWithIdentifier("sarg.alphaanimals", true) == null)
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


    private static void drawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }


    private static void drawIcon(PawnKindDef pawnKind, Rect rect)
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

    private void drawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + ColumnSpacer;
        optionsOuterContainer.width -= leftSideWidth + ColumnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + ColumnSpacer + 20;
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
                listingStandard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                listingStandard.Label("CWAS.settings".Translate());
                Text.Font = GameFont.Small;
                listingStandard.Gap();

                if (Instance.Settings.CustomSpawnRates?.Any() == true ||
                    Instance.Settings.CustomDensities?.Any() == true)
                {
                    var labelPoint = listingStandard.Label("CWAS.resetall.label".Translate(), -1F,
                        "CWAS.resetall.tooltip".Translate());
                    drawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "CWAS.resetall.confirm".Translate(),
                                delegate { Instance.Settings.ResetManualValues(); }));
                        }, "CWAS.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + ButtonSpacer, labelPoint.position.y));
                }

                listingStandard.CheckboxLabeled("CWAS.reversemode.label".Translate(),
                    ref Instance.Settings.ReverseSettingsMode,
                    "CWAS.reversemode.tooltip".Translate());
                listingStandard.CheckboxLabeled("CWAS.logging.label".Translate(), ref Settings.VerboseLogging,
                    "CWAS.logging.tooltip".Translate());
                if (currentVersion != null)
                {
                    listingStandard.Gap();
                    GUI.contentColor = Color.gray;
                    listingStandard.Label("CWAS.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listingStandard.End();
                break;
            }

            default:
            {
                BiomeDef currentBiomeDef = null;
                PawnKindDef currentAnimalDef = null;
                string description;
                Rect headerLabel;
                listingStandard.Begin(frameRect);
                if (Instance.Settings.ReverseSettingsMode)
                {
                    currentAnimalDef = PawnKindDef.Named(SelectedDef);
                    if (currentAnimalDef == null)
                    {
                        listingStandard.End();
                        break;
                    }

                    description = currentAnimalDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentAnimalDef.defName;
                    }

                    headerLabel = listingStandard.Label(currentAnimalDef.label.CapitalizeFirst());
                }
                else
                {
                    currentBiomeDef = BiomeDef.Named(SelectedDef);
                    if (currentBiomeDef == null)
                    {
                        listingStandard.End();
                        break;
                    }

                    description = currentBiomeDef.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = currentBiomeDef.defName;
                    }

                    headerLabel = listingStandard.Label(currentBiomeDef.label.CapitalizeFirst());
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
                if (Instance.Settings.ReverseSettingsMode)
                {
                    if (Instance.Settings.CustomSpawnRates?.Any(pair =>
                            pair.Value.dictionary.ContainsKey(SelectedDef)) == true)
                    {
                        drawButton(() =>
                            {
                                if (currentAnimalDef != null)
                                {
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                        "CWAS.resetone.confirm".Translate(currentAnimalDef.LabelCap),
                                        delegate
                                        {
                                            Instance.Settings.ResetOneAnimal(SelectedDef);
                                            var selectedAnimal = PawnKindDef.Named(SelectedDef);
                                            foreach (var biomeDef in Main.AllBiomes)
                                            {
                                                var cachedCommonalitiesTraverse = Traverse.Create(biomeDef)
                                                    .Field("cachedAnimalCommonalities");
                                                if (cachedCommonalitiesTraverse.GetValue() == null)
                                                {
                                                    _ = biomeDef.CommonalityOfAnimal(selectedAnimal);
                                                }

                                                var cachedAnimalCommonalities =
                                                    (Dictionary<PawnKindDef, float>)
                                                    cachedCommonalitiesTraverse.GetValue();

                                                var commonality =
                                                    cachedAnimalCommonalities.GetValueOrDefault(selectedAnimal, 0f);

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
                                }
                            }, "CWAS.reset.button".Translate(),
                            new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                                headerLabel.position.y));
                    }

                    drawButton(delegate { copyOtherAnimalValues(SelectedDef); }, "CWAS.copy.button".Translate(),
                        headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));

                    listingStandard.End();
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

                        if (Instance.Settings.CustomSpawnRates != null && Instance.Settings
                                .CustomSpawnRates.ContainsKey(biomeDef.defName) && Instance.Settings
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

                if (Instance.Settings.CustomSpawnRates?.ContainsKey(SelectedDef) == true ||
                    Instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    drawButton(() =>
                        {
                            if (currentBiomeDef != null)
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "CWAS.resetone.confirm".Translate(currentBiomeDef.LabelCap),
                                    delegate
                                    {
                                        Instance.Settings.ResetOneBiome(SelectedDef);
                                        currentBiomeAnimalDensity = Main.VanillaDensities[SelectedDef];
                                        var selectedBiome = BiomeDef.Named(SelectedDef);
                                        var cachedCommonailtiesTraverse = Traverse.Create(selectedBiome)
                                            .Field("cachedAnimalCommonalities");
                                        if (cachedCommonailtiesTraverse.GetValue() == null)
                                        {
                                            _ = selectedBiome.CommonalityOfAnimal(Main.AllAnimals.First());
                                        }

                                        var cachedAnimalCommonalities =
                                            (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                                        foreach (var animal in Main.AllAnimals)
                                        {
                                            var commonality = cachedAnimalCommonalities.GetValueOrDefault(animal, 0f);

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
                            }
                        }, "CWAS.reset.button".Translate(),
                        new Vector2(headerLabel.position.x + headerLabel.width - (buttonSize.x * 2),
                            headerLabel.position.y));
                }


                drawButton(delegate { copySpawnValues(SelectedDef); }, "CWAS.copy.button".Translate(),
                    headerLabel.position + new Vector2(frameRect.width - buttonSize.x, 0));
                if (Instance.Settings.CustomDensities?.ContainsKey(SelectedDef) == true)
                {
                    GUI.color = Color.green;
                }

                listingStandard.Gap();
                currentBiomeAnimalDensity =
                    (float)Math.Round((decimal)Widgets.HorizontalSlider(
                        listingStandard.GetRect(50),
                        currentBiomeAnimalDensity, 0,
                        6f, false,
                        currentBiomeAnimalDensity.ToString(),
                        "CWAS.density.label".Translate()), 3);
                GUI.color = Color.white;
                listingStandard.End();

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

                    if (Instance.Settings.CustomSpawnRates != null &&
                        Instance.Settings.CustomSpawnRates.ContainsKey(SelectedDef) && Instance.Settings
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
                    drawIcon(animal,
                        new Rect(rowRect.position, iconSize));
                }

                scrollListing.End();
                Widgets.EndScrollView();
                break;
            }
        }
    }

    private static void copySpawnValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var biome in Main.AllBiomes.Where(biomeDef => biomeDef.defName != originalDef))
        {
            list.Add(new FloatMenuOption(biome.LabelCap, action));
            continue;

            void action()
            {
                Main.LogMessage($"Copying overall animal density from {biome.defName} to {originalDef}");
                currentBiomeAnimalDensity = Main.VanillaDensities[biome.defName];
                if (Instance.Settings.CustomDensities.TryGetValue(biome.defName, out var density))
                {
                    currentBiomeAnimalDensity = density;
                }


                Main.LogMessage($"Fetching current animal spawnrates for {biome.defName}");
                var cachedCommonailtiesTraverse = Traverse.Create(biome)
                    .Field("cachedAnimalCommonalities");
                if (cachedCommonailtiesTraverse.GetValue() == null)
                {
                    _ = biome.CommonalityOfAnimal(Main.AllAnimals.First());
                }

                var cachedAnimalCommonalities =
                    (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                foreach (var animal in Main.AllAnimals)
                {
                    Main.LogMessage($"Setting spawnrate for {animal.defName}");
                    var commonality = cachedAnimalCommonalities.GetValueOrDefault(animal, 0f);

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
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void copyOtherAnimalValues(string originalDef)
    {
        var list = new List<FloatMenuOption>();

        foreach (var animal in Main.AllAnimals.Where(pawnKindDef => pawnKindDef.defName != originalDef))
        {
            list.Add(new FloatMenuOption(animal.LabelCap, action));
            continue;

            void action()
            {
                Main.LogMessage($"Setting spawnrate from {animal.defName}");

                foreach (var biomeDef in Main.AllBiomes)
                {
                    var cachedCommonailtiesTraverse = Traverse.Create(biomeDef)
                        .Field("cachedAnimalCommonalities");
                    if (cachedCommonailtiesTraverse.GetValue() == null)
                    {
                        _ = biomeDef.CommonalityOfAnimal(animal);
                    }

                    var cachedAnimalCommonalities =
                        (Dictionary<PawnKindDef, float>)cachedCommonailtiesTraverse.GetValue();

                    var commonality = cachedAnimalCommonalities.GetValueOrDefault(animal, 0f);

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
        }

        Find.WindowStack.Add(new FloatMenu(list));
    }

    private static void DrawTabsList(Rect rect)
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
        if (Instance.Settings.ReverseSettingsMode)
        {
            height = Main.AllAnimals.Count;
        }

        tabContentRect.height = (height * 27f) + listAddition;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listingStandard.Begin(tabContentRect);
        if (listingStandard.ListItemSelectable("CWAS.settings".Translate(), Color.yellow, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listingStandard.ListItemSelectable(null, Color.yellow);
        if (Instance.Settings.ReverseSettingsMode)
        {
            foreach (var animalDef in Main.AllAnimals)
            {
                var toolTip = $"{animalDef.defName} ({animalDef.modContentPack?.Name})\n{animalDef.race.description}";
                if (Instance.Settings.CustomSpawnRates?.Any(pair =>
                        pair.Value.dictionary.ContainsKey(animalDef.defName)) == true)
                {
                    GUI.color = Color.green;
                    toolTip = "CWAS.customexists".Translate();
                }

                if (listingStandard.ListItemSelectable(animalDef.label.CapitalizeFirst(), Color.yellow,
                        SelectedDef == animalDef.defName, false, toolTip))
                {
                    SelectedDef = SelectedDef == animalDef.defName ? null : animalDef.defName;
                }

                GUI.color = Color.white;
            }

            listingStandard.End();
            Widgets.EndScrollView();
            return;
        }

        foreach (var biomeDef in allBiomes)
        {
            var toolTip = $"{biomeDef.defName} ({biomeDef.modContentPack?.Name})\n{biomeDef.description}";
            if (Instance.Settings.CustomSpawnRates.ContainsKey(biomeDef.defName) ||
                Instance.Settings.CustomDensities.ContainsKey(biomeDef.defName))
            {
                GUI.color = Color.green;
                toolTip += "\n" + "CWAS.customexists".Translate();
            }

            if (listingStandard.ListItemSelectable(biomeDef.label.CapitalizeFirst(), Color.yellow,
                    SelectedDef == biomeDef.defName, false, toolTip))
            {
                SelectedDef = SelectedDef == biomeDef.defName ? null : biomeDef.defName;
            }

            GUI.color = Color.white;
        }

        listingStandard.End();
        Widgets.EndScrollView();
    }
}