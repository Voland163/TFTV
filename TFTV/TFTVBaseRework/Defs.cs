using Base.Defs;
using Base.UI;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;

namespace TFTV.TFTVBaseRework
{
    internal class Defs
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefRepository Repo = TFTVMain.Repo;

        public static void CreateAndModifyDefs()
        {
            try
            {

                CreateJustAGruntDef();
                CreateHiddenFromOperativesMarkerDef();
                CreateDismissedOperativeMarkerDef();
                TFTVIncidents.ComputeMountedAbility.Defs.CreateDefs();

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }

        private static void CreateJustAGruntDef()
        {
            try
            {
                const string abilityName = "JustAGrunt_AbilityDef";

                PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");
                PassiveModifierAbilityDef justAGrunt = Helper.CreateDefFromClone(
                    source,
                    "6e03f586-f5e0-4b97-bd89-4f3df26fcb8c",
                    abilityName);

                justAGrunt.ViewElementDef = Helper.CreateDefFromClone(
                    source.ViewElementDef,
                    "2dd44f1a-85be-4e24-8437-29402e7be0d5",
                    abilityName);

                justAGrunt.CharacterProgressionData = Helper.CreateDefFromClone(
                    source.CharacterProgressionData,
                    "f89f0b6c-8665-4323-a133-c790f0c587a3",
                    abilityName);

                justAGrunt.ViewElementDef.DisplayName1.LocalizationKey = "KEY_BASE_REWORK_JUST_A_GRUNT_NAME";
                justAGrunt.ViewElementDef.Description.LocalizationKey = "KEY_BASE_REWORK_JUST_A_GRUNT_DESCRIPTION";
                justAGrunt.ViewElementDef.LargeIcon = Helper.CreateSpriteFromImageFile("ability_just_a_grunt.png");
                justAGrunt.ViewElementDef.SmallIcon = justAGrunt.ViewElementDef.LargeIcon;

                justAGrunt.StatModifications = new ItemStatModification[0];
                justAGrunt.ItemTagStatModifications = new EquipmentItemTagStatModification[0];

                if (justAGrunt.CharacterProgressionData != null)
                {
                    justAGrunt.CharacterProgressionData.RequiredSpeed = 0;
                    justAGrunt.CharacterProgressionData.RequiredStrength = 0;
                    justAGrunt.CharacterProgressionData.RequiredWill = 0;
                }

                PersonnelRestrictions.JustAGruntAbility = justAGrunt;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateHiddenFromOperativesMarkerDef()
        {
            try
            {
                PersonnelRestrictions.HiddenFromOperativesAbility = CreateTokenMarkerAbilityDef(
                    "HiddenFromOperatives_AbilityDef",
                    "8d2b9677-2218-4e59-a5bb-a8ad8fbce101",
                    "17d75f63-c6a7-46f4-8673-d0ab0b09db11",
                    "10a6e8b1-2865-4b69-a6cf-1ec95bf2c8d1",
                    "Hidden From Operatives Marker",
                    "Internal BaseRework marker ability.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void CreateDismissedOperativeMarkerDef()
        {
            try
            {
                PersonnelRestrictions.DismissedOperativeAbility = CreateTokenMarkerAbilityDef(
                    "DismissedOperative_AbilityDef",
                    "116d17a6-cdd9-4b19-b7e6-6d7ee4a2d241",
                    "e6d06f55-f1b3-4f30-bd64-f23ac49b9011",
                    "1842a59c-8d16-42b0-9f3f-c51cb859e6a1",
                    "Dismissed Operative Marker",
                    "Internal BaseRework marker ability.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static PassiveModifierAbilityDef CreateTokenMarkerAbilityDef(
            string abilityName,
            string abilityGuid,
            string viewGuid,
            string progressionGuid,
            string displayName,
            string description)
        {
            PassiveModifierAbilityDef source = DefCache.GetDef<PassiveModifierAbilityDef>("Thief_AbilityDef");

            PassiveModifierAbilityDef marker = Helper.CreateDefFromClone(
                source,
                abilityGuid,
                abilityName);

            marker.ViewElementDef = Helper.CreateDefFromClone(
                source.ViewElementDef,
                viewGuid,
                abilityName);

            marker.CharacterProgressionData = Helper.CreateDefFromClone(
                source.CharacterProgressionData,
                progressionGuid,
                abilityName);

            marker.ViewElementDef.DisplayName1 = new LocalizedTextBind(displayName, true);
            marker.ViewElementDef.Description = new LocalizedTextBind(description, true);
            marker.StatModifications = new ItemStatModification[0];
            marker.ItemTagStatModifications = new EquipmentItemTagStatModification[0];

            if (marker.CharacterProgressionData != null)
            {
                marker.CharacterProgressionData.RequiredSpeed = 0;
                marker.CharacterProgressionData.RequiredStrength = 0;
                marker.CharacterProgressionData.RequiredWill = 0;
            }

            return marker;
        }
    }
}
