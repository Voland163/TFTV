# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- When information is missing, fetch the required code/context directly without asking the user to paste it, and proceed.
- Avoid using a 'deep breath' preface in responses.
- Explicitly cover each issue raised and address all reported cases; point out any omissions in proposed fixes.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- Prefer a single combined Harmony patch per target method when possible, and refactor large Prefix bodies into smaller helper methods for readability.
- Do not Harmony patch our own mod code; modify our own code directly instead.
- Prefer persisting tactical mission state via `TFTVTacInstanceData`, consistent with existing save/load patterns in this codebase.

## Project-Specific Rules
- Scarab Gemini is a standalone `GroundVehicleWeaponDef` (not attached as a `Weapon` subaddon of a `GroundVehicleModuleDef`). 
- Vehicle module ammo Harmony patches in `Vehicles\Ammo\HarmonyPatches.cs` are intended for Junker weapons that are `GroundVehicleWeaponDef` subaddons on a `GroundVehicleModuleDef`.
- For Ordnance Resupply behavior, choose option A: reload one selected weapon only.
- When implementing Marketplace ammo auto-purchase via the `UIInventorySlotSideButton`, ensure that ammo is purchasable only if an explicit ammo `GeoEventChoice` exists in `GeoMarketplace.MarketplaceChoices`. Purchase exactly 1 clip, remove one matching choice from `MarketplaceChoices`, and support Junker and KaosGun weapons. The Marketplace UI does not need to be open when the side button is pressed.
- Vest tiers should be additive per research (each research adds a tier), not set by highest completed tier; e.g., Hazmat: Swarmer adds a tier, Acheron adds a tier.
- Incident reward mapping should be hardcoded in code (not parsed at runtime) and implemented faction by faction (AN then NJ then SY). 'Haven leader' refers to leader attitude toward Phoenix; faction short name (e.g., ANU +4) refers to faction attitude toward Phoenix; inter-faction rewards change diplomacy between those two factions. Incident outcomes with 'extra/additional personnel' should grant doubled special-personnel counts via TryGetIncidentRewardCount.
- Apply incident requirements should be hardcoded in code (not parsed at runtime), implemented per faction in `GeoscapeEvents.ApplyIncidentRequirements`.
- Confirmed that loot items do have icons; missing inventory slot icons is not caused by missing item icons.
- In incident chains, "same haven" refers to the haven where the previous incident in that chain succeeded; tag that haven on the prior incident’s success and require the tag for the next incident.
- For the dismissed-operative feature, do not store extra dismissed/redeploy metadata in PersonnelInfo if it can be derived from GeoCharacter; redeploy cost formula is 10 SP per character level above 1 (10 * (Level - 1)).
- For BaseRework operative generation, adjust the source TacCharacterDef's class tag to the requested class immediately before character generation and restore it immediately after generation.
- For BaseRework personnel markers, replace both hidden and dismissed tags with token PassiveModifierAbilityDef markers checked by ability presence rather than GameTags.

## Affinities Design Rules
- Affinities feature 6 affinities, each with 3 ranks. Each affinity has two benefit tracks (Geoscape and Tactical) with two player-selectable options each; choices are global/shared.
- Affinities are gained or advanced by resolving Incidents or via Drill at one rank below max; there are three ranks.
- Each affinity grants Geoscape and Tactical benefits, with incident resolution efficiency increased by 2?rank when affinity matches Approach.
- Upon reaching a new rank for the first time, choose one of two benefits per affinity; this choice applies to all characters with that affinity.
- Benefits are multiplied by rank, but bonuses do not stack (only the highest-ranked specialist's benefits apply).
- Geoscape benefits apply to aircraft where the character is a passenger, while tactical benefits grant abilities to the character.
- Each character can have only one affinity.
- For incident resolution, the cancel event should be the failure event; the only way to fail is canceling.
- Affinity award behavior: only grant/advance on success using the leader stored at start; if choice has multiple affinity approaches, use the leader's matching affinity if present, otherwise pick one at random.
- Implementation should proceed Geoscape first, starting with dynamic ability description updates.
- For Biotech medkit affinity effects, apply the benefit only when the TacticalActor using the medkit has the relevant affinity.
- For Compute tactical option 2, the Perception bonus scales by affinity rank: rank 1 = 1/3 of squad Delirium capped at 10, rank 2 = 2/3 capped at 20, rank 3 = 1x capped at 30.
- For Biotech geoscape option 2, haven leader attitude must not be repeatedly farmed; revisit grants should only increase when a higher-rank Biotech operative visits, applying only the rank difference, tracked via haven SiteTags per rank. Leave a comment for later haven-info UI display of the Biotech rank bonus next to leader attitude instead of implementing it now.
- Compute geoscape haven-attack warning is intended to scale with rank and be a visual indicator on affected havens; Exploration geoscape logic should be gated by BaseRework, not AircraftRework.