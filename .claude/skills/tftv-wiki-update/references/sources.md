# Where the facts are

All paths are relative to the release checkout (`$WORK/src`, the release tag), **not** master.

## Order of trust

1. **Code at the release tag.** It is what players run. TFTV mostly changes game defs in C# at
   startup (`def.X = value`), and changes behaviour with Harmony patches. A stat the code never touches
   keeps its vanilla value.
2. **Localization shipped with the release** (`Dist/Assets/Localization/*.csv`, column `English`).
   This is the text players read in game, and the best wording for names and descriptions. Descriptions
   can lag behind code changes; if a description gives a number that contradicts the code, follow the code
   and note it in CHANGES.md.
3. **Discord #announcements.** It has the intent and the explanation, and it covers most changes. It
   describes deltas between TFTV versions, and later posts often revise earlier ones.
4. **The vanilla pages of wiki.phoenixpoint.com** (Weapons, Combat, Damage_types, faction research
   pages, Kaos_Engines, …). Use them for the "Vanilla:" side of a comparison. They can be older than the
   last vanilla patch. `TFTVRevertSGPatchBalance.cs` shows which balance changes of a late vanilla patch
   TFTV reverts.
5. **The existing TFTV page.** It is a description of 1.0 (April 2024), touched up since. Treat its
   numbers as claims to check, not as facts.

The `CHANGELOG_*_Internal_Beta.md` files in the repo root describe builds after the latest release. Do
not use them unless the user has explicitly chosen a beta target.

## Localization files (`Dist/Assets/Localization/`)

| File | Holds |
|---|---|
| `TFTV_Phoenixpedia.csv` | in-game Phoenixpedia guides (`KEY_PHOENIXPEDIA_GUIDE_*`): the mod's own explanations of mechanics. Read first for any mechanic. |
| `TFTV_Options_Localization.csv` | names and descriptions of every mod option and new-game option |
| `TFTV_AbilitiesEffectsStatusesTactical_Localization.csv` | ability, perk and status names and descriptions |
| `TFTV_CharactersItemsFacilities_Localization.csv`, `Vehicles.csv` | items, weapons, armour, facilities, vehicles and modules |
| `TFTV_Research_Localization.csv` | research names and texts |
| `TFTV_AircraftRework_Localization.csv`, `TFTV_BaseReworkPersonnel_Localization.csv` | Aircraft Rework Beta: aircraft, modules, drills, personnel, base activation |
| `TFTV_Incidents_*_Localization.csv` | Incidents (beta) per faction |
| `TFTV_VoidOmensAndODI_Localization.csv` | Void Omens and Oneiric Delirium events |
| `TFTV_HintsGeo_Localization.csv`, `TFTV_HintsTactical_Localization.csv`, `TFTV_LoreAndTips_Localization.csv` | hints and tips: compact rule statements |
| `TFTV_Events_Localization.csv`, `TFTV_NJStoryMissions_Localization.csv`, `TFTV_MissionObjectives_Localization.csv` | story, events and objectives (spoilers) |
| `PR_BC_Localization.csv` | Better Classes texts |

Search them with `scripts/loc_find.py "$WORK/src" <terms>` (or `--key <regex>`), which prints each hit
as a ledger citation `[loc: FILE:KEY]` with its English text. They are CSV with newlines and commas
inside fields, so do not grep them line by line for anything longer than a word.
`Dist/Assets/Presets/AbilityDefToNameDict.json` maps ability def names to display names, which helps
connect code to the texts players see.

## Code map (`TFTV/`)

| Wiki topic | Look in |
|---|---|
| Mod options, defaults, which can change mid-campaign | `TFTVConfig.cs`, `TFTVNewGameOptions.cs`, `TFTVNewGameMenu.cs`, `TFTVDefsWithConfigDependency.cs` |
| Difficulty levels (Story Mode … Etermes) | `TFTVSpecialDifficulties.cs`, `TFTVNewGameOptions.cs`, `TFTVDefsInjectedOnlyOnce.cs` |
| Special starts, starting squad and bases | `TFTVStarts.cs`, `TFTVBackgrounds*.cs` |
| Oneiric Delirium, Delirium perks, treatment | `TFTVDelirium.cs`, `TFTVDeliriumPerks.cs`, `TFTVODIandVoidOmenRoll.cs`, `TFTVStamina.cs` |
| Void Omens | `TFTVVoidOmens.cs`, `TFTVODIandVoidOmenRoll.cs` |
| Touched by the Void, Umbra | `TFTVTouchedByTheVoid.cs` |
| Revenants, Project Osiris | `TFTVRevenant.cs`, `TFTVProjectOsiris.cs` |
| Pandoran evolution, colonies, infestation | `TFTVPandoranProgress.cs`, `TFTVPandoranColonyRange.cs`, `TFTVInfestation.cs` |
| Capturing Pandorans, containment | `TFTVCapturePandorans*.cs` |
| Exploration, ambushes, scavenging | `TFTVAmbushes.cs`, `TFTVScavengers.cs` |
| Human enemies (squads, ranks, tactics) | `TFTVHumanEnemies.cs`, `TFTVHumanEnemiesNames.cs`, `TFTVBetterEnemies.cs` |
| Classes and skills | `SkillModifications/*Skills.cs`, `SkillModifications/FactionPerks.cs`, `BackgroundPerks.cs` |
| Weapons, armour, vests | `VariousAdjustments/WeaponModifications.cs`, `VariousAdjustments/*.cs`, `TFTVVests.cs`, `TFTVLaserWeapons/` |
| Augmentations: bionics, mutations | `TFTVAugmentations.cs` |
| Vehicles and modules (main branch) | `Vehicles/`, `TFTVVehicle*.cs`, `TFTVPassengerModules.cs` |
| Kaos Engines marketplace, Kaos guns, mercenaries | `TFTVMarketplace/` |
| Festering Skies: Behemoth, raids, air combat | `TFTVBehemothAndRaids.cs`, `TFTVGeoscape.cs` |
| Legacy of the Ancients | `TFTVAncients.cs`, `TFTVAncientsGeo.cs` |
| Corrupted Horizons, Pure and Forsaken, NJ questline, Palace | `TFTVChangesToDLC*Events.cs`, `TFTVPureAndForsaken.cs`, `TFTVNJQuestline.cs`, `TFTVPalaceMission.cs` |
| Base defence, base layout | `TFTVBaseDefense*.cs`, `TFTVPhoenixBaseLayout.cs` |
| Diplomacy, trading, haven recruits | `TFTVDiplomacyPenalties.cs`, `TFTVTradingRecruiting.cs`, `TFTVHavenRecruitsGenerationAdjustments.cs` |
| Healing, medbay, stamina | `TFTVHealing.cs`, `TFTVStamina.cs` |
| Damage types: acid, fire, poison, goo, shock | `TFTVAcid.cs`, `TFTVMeleeDamage.cs`, `TFTVBallistics.cs`, `VariousAdjustments/` |
| Suppression (option) | `TFTVSuppression/` |
| Choose Deployment Positions (option) | `TFTVPlayerDeployment*.cs` |
| Research changes, reverse engineering | `TFTVResearch.cs`, `TFTVReverseEngineering.cs` |
| Economy fixes, experience | `TFTVEconomyExploitsFixes.cs`, `TFTVExperienceDistribution.cs` |
| UI and quality of life (loadouts, agenda, helmet toggle, …) | `TFTVUI/`, `TFTVAAAgenda/`, `PortedAATweaks/`, `TFTVDragandDropFunctionality.cs` |
| Vanilla bug fixes | `TFTVVanillaFixes/` (one file per fix area); plus "(Vanilla)" items in announcements |
| **Aircraft Rework Beta**: mode switch | `TFTVAircraftRework/AircraftReworkMode.cs`; beta branches test `AircraftReworkOn` / `AircraftReworkMode.RunningOn` |
| Beta: aircraft, speed/range, maintenance, modules | `TFTVAircraftRework/` |
| Beta: drills | `TFTVDrills/DrillsDefs.cs`, `DrillsUnlock.cs` |
| Beta: personnel, base activation, outposts, training | `TFTVBaseRework/` |
| Beta: incidents and affinities | `TFTVIncidents/` |

The tree changes between releases. If a file in this table is missing at the tag, find the feature with
`git -C "$WORK/src" grep -n "<term>"` on a def name, a localization key or a word from the changelog.
When a localization key is known (e.g. from the CSV), grepping the code for it leads straight to the
feature.

## Discord

- Server id `858074440667365426`. Channels used: `announcements` (all public releases, dev blogs before
  1.0), `open-beta-aircraft-rework` (beta notes). The bot can read the others too (`faq`, `guides`), but
  they are player-written: use them only to find out what players find confusing.
- Patch notes use `-Bugfixes`, `-Changes`, `-New features` headings and `+` bullets, then an "Aircraft
  Rework Beta" part with its own headings. "(Vanilla)" or "(old Vanilla)" after a bugfix means a
  base-game bug that TFTV fixes.
- Release naming: before 1.0, "Update #N". From 1.0, versions 1.1.x.y, also named "Patch N", plus
  "Hotfix K for Patch N". Map them to GitHub tags (`v.1.1.4.7`) by date.
