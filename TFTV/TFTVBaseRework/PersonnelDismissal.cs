using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
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
            if (!BaseReworkUtils.BaseReworkEnabled || faction == null || character == null)
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

                TFTVUI.Personnel.Loadouts.UnequipButtonClicked();
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
                    if (!TFTVBaseRework.BaseReworkUtils.BaseReworkEnabled)
                    {
                        return true;
                    }


                    if (!ShouldConvertDismissedOperativeToCivilian(__instance, unit, reason))
                    {
                        return true;
                    }

                    bool converted = ConvertDismissedOperativeToCivilian(__instance, unit);
                    return !converted;
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
