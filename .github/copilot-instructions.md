# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- When information is missing, fetch the required code/context directly without asking the user to paste it, and proceed.
- Avoid using a 'deep breath' preface in responses.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- Prefer a single combined Harmony patch per target method when possible, and refactor large Prefix bodies into smaller helper methods for readability.
- Do not Harmony patch our own mod code; modify our own code directly instead.

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

## Affinities Design Rules
- Affinities are gained or advanced by resolving Incidents or via Drill at one rank below max; there are three ranks.
- Each affinity grants Geoscape and Tactical benefits, with incident resolution efficiency increased by 2?rank when affinity matches Approach.
- Upon reaching a new rank for the first time, choose one of two benefits per affinity; this choice applies to all characters with that affinity.
- Benefits are multiplied by rank, but bonuses do not stack (only the highest-ranked specialist's benefits apply).
- Geoscape benefits apply to aircraft where the character is a passenger, while tactical benefits grant abilities to the character.
- Each character can have only one affinity.
- For incident resolution, the cancel event should be the failure event; the only way to fail is canceling.
- Affinity award behavior: only grant/advance on success using the leader stored at start; if choice has multiple affinity approaches, use the leader's matching affinity if present, otherwise pick one at random.