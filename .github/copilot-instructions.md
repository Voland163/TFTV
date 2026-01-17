# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- When information is missing, fetch the required code/context directly without asking the user to paste it, and proceed.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- Prefer a single combined Harmony patch per target method when possible, and refactor large Prefix bodies into smaller helper methods for readability.

## Project-Specific Rules
- Scarab Gemini is a standalone `GroundVehicleWeaponDef` (not attached as a `Weapon` subaddon of a `GroundVehicleModuleDef`). 
- Vehicle module ammo Harmony patches in `Vehicles\Ammo\HarmonyPatches.cs` are intended for Junker weapons that are `GroundVehicleWeaponDef` subaddons on a `GroundVehicleModuleDef`.
- For Ordnance Resupply behavior, choose option A: reload one selected weapon only.
- When implementing Marketplace ammo auto-purchase via the `UIInventorySlotSideButton`, ensure that ammo is purchasable only if an explicit ammo `GeoEventChoice` exists in `GeoMarketplace.MarketplaceChoices`. Purchase exactly 1 clip, remove one matching choice from `MarketplaceChoices`, and support Junker and KaosGun weapons. The Marketplace UI does not need to be open when the side button is pressed.