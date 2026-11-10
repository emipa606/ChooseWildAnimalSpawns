# GitHub Copilot Instructions for Choose Wild Animal Spawns Mod

## Mod Overview and Purpose
**Choose Wild Animal Spawns** is a RimWorld mod developed by Mlie. It is a successor to the "Disable Wild Animal Spawns" mod, offering greater flexibility in controlling wild animal spawn rates. The mod allows you to define the spawn rate for each animal per biome, including both vanilla and modded animals. Additionally, it includes features for adjusting the overall animal density per biome, copying spawn values between biomes, and selecting an animal to define spawn rates per biome.

## Key Features and Systems

1. **Spawn Rate Per Biome and Animal**: You can set the spawn rate for each animal in every biome.
2. **Overall Animal Density Slider**: A slider to adjust the overall animal density in each biome.
3. **Copy Values Between Biomes**: An option to copy spawn values from one biome to another.
4. **Reverse Selection**: Ability to select an animal and define spawn rates per biome.
5. **Integration with BetterSliders**: Enhanced accuracy with the BetterSliders mod.

## Coding Patterns and Conventions

- **Mod Structure**: The mod follows a standard RimWorld mod structure with C# files for logic and an XML file for metadata.
- **Namespaces**: The main namespace for the mod is `ChooseWildAnimalSpawns`.
- **Classes and Methods**: Classes are named meaningfully (e.g., `ChooseWildAnimalSpawns_Mod`, `ChooseWildAnimalSpawns_Settings`) and methods are documented with XML comments to describe their functionality.
- **Data Persistence**: The mod uses a `SaveableDictionary` class to handle saving and loading settings.
- **Settings Management**: The `ChooseWildAnimalSpawns_Settings` class manages the mod's settings, including methods for exposing data, resetting values, and saving settings.
- **Harmony Patching**: The mod uses Harmony to patch existing RimWorld code to integrate its functionality seamlessly.

## XML Integration

The mod includes an XML file named `About.xml` which provides metadata about the mod, including its dependencies:

xml
<About>
  <ModIdentifier>Mlie.ChooseWildAnimalSpawns</ModIdentifier>
  <Name>Choose Wild Animal Spawns</Name>
  <Author>Mlie</Author>
  <Version>1.0.0</Version>
  <Description>
    This mod allows you to define the spawn-rate per biome per animal.
  </Description>
  <Dependencies>
    <Dependency>brrainz.harmony</Dependency>
  </Dependencies>
</About>


## Harmony Patching

The mod uses Harmony for patching RimWorld code. The following is a simplified example of a Harmony patch used in the mod:

csharp
using HarmonyLib;

namespace ChooseWildAnimalSpawns
{
    public class HarmonyPatches
    {
        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn))]
        public static class Patch_PawnGenerator_GeneratePawn
        {
            public static void Postfix(Pawn __result)
            {
                // Custom logic to adjust spawn rates based on the biome and animal
            }
        }
    }
}


## Suggestions for Copilot

1. **Mod Configuration**: When generating code, consider providing detailed comments explaining the purpose and functionality of each method and class.
2. **Data Handling**: When working with settings and data persistence, ensure that the code is robust and handles edge cases, such as missing data or invalid input.
3. **Harmony Patches**: When creating Harmony patches, provide detailed explanations of the original code being patched and the changes being made to integrate the mod's functionality.
4. **User Interface**: When generating UI code, ensure that it follows the RimWorld UI conventions and is intuitive for users to interact with.
5. **Testing and Debugging**: Include comments and logging to help with testing and debugging, especially when dealing with complex interactions between different parts of the mod.

By following these guidelines, you can contribute effectively to the Choose Wild Animal Spawns mod, ensuring that the code is maintainable, efficient, and aligned with the mod's purpose and features.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

