using Base.Core;
using Base.Defs;
using Base.Utils.GameConsole;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.Vehicles.Ammo
{
    /// <summary>
    /// Reports how ground vehicle weapon modules are actually wired to ammunition.
    ///
    /// The question this exists to answer: the game ships both KS_Buggy_Fullstop_WeaponDef and
    /// KS_Buggy_Fullstop_GroundVehicleWeaponDef (and the same pair for the Screamer, but not for the
    /// Vishnu), and this mod only gives ammo to the plain WeaponDef of each pair. GetSubWeapons()
    /// returns anything deriving from WeaponDef, so if the modules reference the GroundVehicleWeaponDef
    /// variants instead, that ammo is inert - which looks identical to a replenishment bug but is not
    /// one. Def contents cannot be read outside the game, so this asks the loaded defs directly.
    /// </summary>
    internal static class AmmoDiagnostics
    {
        private const string LogPrefix = "[TFTV][VehicleAmmo] ";
        private const string TracePrefix = "[TFTV][AmmoTrace] ";

        /// <summary>
        /// Off by default, and deliberately so.
        ///
        /// Every ammunition interaction worth following sits inside something that runs on a UI
        /// refresh or a per-frame update, so tracing them unconditionally buries the log - which is
        /// exactly what this file's predecessor did. Turn it on for the run where the question
        /// matters, read the answer, turn it off.
        /// </summary>
        internal static bool TraceEnabled { get; private set; }

        [ConsoleCommand(
            Command = "tftv_ammo_trace",
            Description = "Follows every ammunition interaction in the log. Usage: tftv_ammo_trace on|off")]
        public static void SetAmmoTrace(IConsole console, string state)
        {
            bool on = string.Equals(state, "on", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(state, "true", StringComparison.OrdinalIgnoreCase)
                   || state == "1";

            bool off = string.Equals(state, "off", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state, "false", StringComparison.OrdinalIgnoreCase)
                    || state == "0";

            if (!on && !off)
            {
                console.WriteLine("Usage: tftv_ammo_trace on|off");
                return;
            }

            TraceEnabled = on;
            string message = on
                ? "Ammunition tracing on - every load, reload, replenish and purchase goes to TFTV.log."
                : "Ammunition tracing off.";

            console.WriteLine(message);
            TFTVLogger.Always(TracePrefix + message);
        }

        /// <summary>
        /// One line of the ammunition trace. <paramref name="stage"/> names where in the chain this
        /// happened - post-mission, replenish screen, equip screen - so a log can be read as a
        /// sequence rather than a pile.
        /// </summary>
        internal static void Trace(string stage, string message)
        {
            if (!TraceEnabled) return;

            try
            {
                TFTVLogger.Always($"{TracePrefix}[{stage}] {message}");
            }
            catch
            {
                // Tracing must never be the thing that breaks a reload.
            }
        }

        /// <summary>
        /// A module or weapon's ammunition state, for one trace line rather than several.
        /// </summary>
        internal static string DescribeAmmo(ICommonItem item)
        {
            try
            {
                if (item?.ItemDef == null) return "<no item>";

                GroundVehicleModuleDef moduleDef = item.ItemDef as GroundVehicleModuleDef;
                if (moduleDef == null)
                {
                    int charges = item.CommonItemData?.CurrentCharges ?? -1;
                    return $"{item.ItemDef.name} {charges}/{item.ItemDef.ChargesMax}";
                }

                List<string> parts = new List<string>();
                foreach (TacticalItemDef ammoDef in VehicleModuleAmmoHarmonyPatches.GetModuleAmmoDefs(moduleDef))
                {
                    int have = VehicleModuleAmmoHarmonyPatches.GetAmmoChargesForDef(item.CommonItemData, ammoDef);
                    int max = VehicleModuleAmmoHarmonyPatches.GetAmmoCapacityForDef(moduleDef, ammoDef);
                    parts.Add($"{ammoDef.name} {have}/{max}");
                }

                return parts.Count > 0
                    ? $"{moduleDef.name} [{string.Join(", ", parts)}]"
                    : $"{moduleDef.name} [no ammo types]";
            }
            catch
            {
                return "<unreadable>";
            }
        }

        [ConsoleCommand(
            Command = "tftv_vehicle_ammo_report",
            Description = "Lists every ground vehicle weapon module, its sub-weapons and the ammo each is wired to.")]
        public static void ReportVehicleAmmoWiring(IConsole console)
        {
            try
            {
                DefRepository repo = GameUtl.GameComponent<DefRepository>();
                if (repo == null)
                {
                    console.WriteLine("No DefRepository available.");
                    return;
                }

                List<GroundVehicleModuleDef> modules = repo.GetAllDefs<GroundVehicleModuleDef>()
                    .Where(m => m != null)
                    .OrderBy(m => m.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Report(console, $"=== {modules.Count} GroundVehicleModuleDef(s) ===");

                int withWeapons = 0;
                List<string> unarmedSubWeapons = new List<string>();

                foreach (GroundVehicleModuleDef module in modules)
                {
                    List<WeaponDef> subWeapons = SafeSubWeapons(module);
                    if (subWeapons.Count == 0)
                    {
                        // Engines, hulls and plating - no ammo to report.
                        continue;
                    }

                    withWeapons++;
                    Report(console, $"{module.name}");

                    foreach (WeaponDef weapon in subWeapons)
                    {
                        // The concrete type is the whole point: WeaponDef vs GroundVehicleWeaponDef
                        // distinguishes the def this mod patched from the twin it did not.
                        string weaponType = weapon.GetType().Name;

                        List<TacticalItemDef> ammo = weapon.CompatibleAmmunition != null
                            ? weapon.CompatibleAmmunition.Where(a => a != null).ToList()
                            : new List<TacticalItemDef>();

                        string ammoText = ammo.Count > 0
                            ? string.Join(", ", ammo.Select(a => $"{a.name} (clip {a.ChargesMax})"))
                            : "<<< NO AMMO WIRED >>>";

                        Report(console, $"    {weapon.name} [{weaponType}] mag {weapon.ChargesMax}" +
                            $", freeReloadOnMissionEnd {weapon.FreeReloadOnMissionEnd} -> {ammoText}");

                        if (ammo.Count == 0)
                        {
                            unarmedSubWeapons.Add($"{module.name} / {weapon.name} [{weaponType}]");
                        }
                    }
                }

                Report(console, $"=== {withWeapons} module(s) carry sub-weapons ===");

                // The headline answer. Anything listed here is a sub-weapon a player can fire but
                // never reload, because nothing gave it a magazine.
                if (unarmedSubWeapons.Count == 0)
                {
                    Report(console, "Every sub-weapon has ammunition wired. The duplicate-weapon-def theory is dead.");
                }
                else
                {
                    Report(console, $"SUB-WEAPONS WITH NO AMMUNITION ({unarmedSubWeapons.Count}):");
                    foreach (string entry in unarmedSubWeapons)
                    {
                        Report(console, "    " + entry);
                    }
                }

                ReportDuplicateWeaponDefs(console, repo);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                console.WriteLine("Failed - see TFTV.log.");
            }
        }

        /// <summary>
        /// Names that exist as both a WeaponDef and a GroundVehicleWeaponDef, with which of the pair
        /// carries ammunition. If a module above referenced the unarmed twin, this is where it shows.
        /// </summary>
        private static void ReportDuplicateWeaponDefs(IConsole console, DefRepository repo)
        {
            try
            {
                Report(console, "=== Kaos buggy weapon defs, armed or not ===");

                List<WeaponDef> buggyWeapons = repo.GetAllDefs<WeaponDef>()
                    .Where(w => w != null && w.name.StartsWith("KS_Buggy", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(w => w.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (WeaponDef weapon in buggyWeapons)
                {
                    int ammoCount = weapon.CompatibleAmmunition != null ? weapon.CompatibleAmmunition.Length : 0;
                    string ammoNames = ammoCount > 0
                        ? string.Join(", ", weapon.CompatibleAmmunition.Where(a => a != null).Select(a => a.name))
                        : "none";

                    Report(console, $"    {weapon.name} [{weapon.GetType().Name}] -> {ammoNames}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static List<WeaponDef> SafeSubWeapons(GroundVehicleModuleDef module)
        {
            try
            {
                return module.GetSubWeapons() ?? new List<WeaponDef>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return new List<WeaponDef>();
            }
        }

        private static void Report(IConsole console, string line)
        {
            console.WriteLine(line);
            TFTVLogger.Always(LogPrefix + line);
        }
    }
}
