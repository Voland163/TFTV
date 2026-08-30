using HarmonyLib;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFTV.TFTVBaseRework
{
    internal class PersonnelDismissal
    {
        private const string LogPrefix = "[PesonnelDismissal]";

        private static bool ShouldConvertDismissedOperativeToCivilian(
          GeoPhoenixFaction faction,
          GeoCharacter character,
          CharacterDeathReason reason)
        {
            if (!BaseReworkCheck.BaseReworkEnabled || faction == null || character == null)
            {
                return false;
            }

            if (reason != CharacterDeathReason.Dismissed)
            {
                return false;
            }

            if (character.Faction != faction)
            {
                return false;
            }

            if (character.TemplateDef == null || !character.TemplateDef.IsHuman)
            {
                return false;
            }

            if (GeoCharacterFilter.HiddenOperativeMarkerFilter.ShouldHide(character))
            {
                return false;
            }

            return true;
        }



        private static void MoveDismissedOperativeToSiteIfNeeded(GeoPhoenixFaction faction, GeoCharacter character)
        {
            try
            {
                if (faction == null || character == null)
                {
                    return;
                }

                GeoVehicle carrier = faction.Vehicles?
                    .FirstOrDefault(v => v != null && v.Units != null && v.Units.Contains(character));

                if (carrier == null)
                {
                    return;
                }


                GeoSite destination = carrier?.CurrentSite;

                if (destination != null && faction.Bases.Any(b => b.Site == destination))
                {

                }
                else
                {
                    destination = faction.Bases?.FirstOrDefault()?.Site;

                }

                if (destination == null)
                {
                    TFTVLogger.Always($"{LogPrefix} Could not find destination site for dismissed operative {character.DisplayName}.");
                    return;
                }

                carrier.RemoveCharacter(character);
                destination.AddCharacter(character);
                TFTVLogger.Always($"{LogPrefix} Moved dismissed operative {character.DisplayName} to site {destination?.LocalizedSiteName}.");

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }



        private static bool ConvertDismissedOperativeToCivilian(GeoPhoenixFaction faction, GeoCharacter character)
        {
            try
            {
                if (faction == null || character == null)
                {
                    return false;
                }

                //  TFTVUI.Personnel.Loadouts.UnequipButtonClicked();
                TryReturnLoadoutToStorage(faction, character);
                MoveDismissedOperativeToSiteIfNeeded(faction, character);
                PersonnelRestrictions.MarkDismissedOperative(character);
                PersonnelRestrictions.MarkHiddenFromOperatives(character);
                PersonnelData.UpdateDismissedPersonnelRecord(character);

                TFTVLogger.Always($"{LogPrefix} Converted dismissed operative {character.DisplayName} to civilian personnel. HiddenAfter={GeoCharacterFilter.HiddenOperativeMarkerFilter.ShouldHide(character)} DismissedAfter={PersonnelRestrictions.IsDismissedOperative(character)}");
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }


        /// <summary>
        /// Returns the operative's gear to storage through the data model rather than through the
        /// soldier equip screen's inventory lists.
        ///
        /// Those lists are only filled while that screen is showing a character; dismissing from
        /// anywhere else - the personnel screen, for one - left UIInventoryList.UnfilteredItems null
        /// and RemoveItem threw on it. The throw was swallowed one level up, the conversion reported
        /// failure, and the dismissal patch then let vanilla KillCharacter run: the operative was
        /// deleted instead of becoming personnel.
        ///
        /// Failing to hand the gear back is never worth losing the person over, so this reports
        /// trouble and lets the conversion carry on.
        /// </summary>
        private static void TryReturnLoadoutToStorage(GeoPhoenixFaction faction, GeoCharacter character)
        {
            try
            {
                if (faction == null || character == null)
                {
                    return;
                }

                List<GeoItem> keptArmour = character.ArmourItems
                    .Where(item => item.ItemDef.IsPermanentAugment)
                    .ToList();

                List<GeoItem> returned = GetLoadoutReturnedToStorage(character);

                if (returned.Count == 0)
                {
                    return;
                }

                ItemStorage storage = ResolveStorageFor(faction, character);
                if (storage == null)
                {
                    TFTVLogger.Always($"{LogPrefix} No item storage found for {character.DisplayName}; leaving their loadout on them.");
                    return;
                }

                character.SetItems(keptArmour, Enumerable.Empty<GeoItem>(), Enumerable.Empty<GeoItem>());

                foreach (GeoItem item in returned)
                {
                    storage.AddItem(item);
                }

                TFTVLogger.Always($"{LogPrefix} Returned {returned.Count} items from {character.DisplayName} to storage.");
            }
            catch (Exception e)
            {
                TFTVLogger.Always($"{LogPrefix} Could not return the loadout of {character?.DisplayName} to storage; dismissal continues.");
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Everything a dismissal would take off the character and hand back to storage: their gear,
        /// their inventory, and all armour except permanent augments. The dismissal prompt lists this
        /// so the player can see what they are giving back before they agree to it.
        /// </summary>
        internal static List<GeoItem> GetLoadoutReturnedToStorage(GeoCharacter character)
        {
            if (character == null)
            {
                return new List<GeoItem>();
            }

            return character.ArmourItems
                .Where(item => item?.ItemDef != null && !item.ItemDef.IsPermanentAugment)
                .Concat(character.EquipmentItems)
                .Concat(character.InventoryItems)
                .Where(item => item?.ItemDef != null)
                .ToList();
        }

        /// <summary>
        /// Phoenix keeps one shared storage, but a faction that does not gets the storage of the site
        /// the character is standing in.
        /// </summary>
        private static ItemStorage ResolveStorageFor(GeoPhoenixFaction faction, GeoCharacter character)
        {
            GeoSite site = faction.Bases?
                .FirstOrDefault(phoenixBase => phoenixBase?.Site != null
                    && phoenixBase.Site.GetAllCharacters().Any(c => c == character))?.Site;

            if (site == null)
            {
                site = faction.Vehicles?
                    .FirstOrDefault(vehicle => vehicle?.Units != null && vehicle.Units.Contains(character))?.CurrentSite;
            }

            return site != null ? faction.GetItemStorage(site) : faction.ItemStorage;
        }



        [HarmonyPatch(typeof(GeoPhoenixFaction), "KillCharacter", new Type[]
        {
            typeof(GeoCharacter),
            typeof(CharacterDeathReason)
        })]
        internal static class GeoPhoenixFaction_KillCharacter_DismissedOperativeToCivilian_Patch
        {
            private static bool Prefix(GeoPhoenixFaction __instance, GeoCharacter unit, CharacterDeathReason reason)
            {
                try
                {
                    if (!TFTVBaseRework.BaseReworkCheck.BaseReworkEnabled)
                    {
                        return true;
                    }


                    if (!ShouldConvertDismissedOperativeToCivilian(__instance, unit, reason))
                    {
                        return true;
                    }

                    bool converted = ConvertDismissedOperativeToCivilian(__instance, unit);

                    if (!converted)
                    {
                        // Letting vanilla run here would delete an operative the player only meant to
                        // take off field duty. Leaving them where they are is the recoverable failure.
                        TFTVLogger.Always($"{LogPrefix} Conversion of {unit?.DisplayName} failed; blocking the vanilla dismissal so the operative is not lost.");
                    }

                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    return true;
                }
            }
        }
    }
}
