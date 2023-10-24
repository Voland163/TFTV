using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Statuses;
using Base.UI;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Effects.DamageTypes;
using PhoenixPoint.Tactical.Entities.Equipments;
using PRMBetterClasses;

namespace TFTVVehicleRework.Armadillo
{
    public static class GasTanks
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;
        public static void Change()
        {
            //"NJ_Armadillo_Supercharger_GroundVehicleModuleDef"
            GroundVehicleModuleDef SuperCharger = (GroundVehicleModuleDef)Repo.GetDef("ab7d5fb0-dea1-d724-395d-2ff291368d18");
            SuperCharger.ViewElementDef.DisplayName1 = new LocalizedTextBind("NJ_GASTANKS_NAME");
            SuperCharger.BodyPartAspectDef.Speed = 6f;
            SuperCharger.BodyPartAspectDef.StatModifications = new ItemStatModification[]
            {
                new ItemStatModification
                {
                    TargetStat = StatModificationTarget.UnitsInside,
                    Modification = StatModificationType.Add,
                    Value = -1f
                },
            };

            //"FireVulnerability_DamageMultiplierAbilityDef"
            DamageMultiplierAbilityDef FireVulnerability = (DamageMultiplierAbilityDef)Repo.GetDef("3e362406-d616-1984-1b1d-b472979e05ff");

            //"Blast_StandardDamageTypeEffectDef"
            StandardDamageTypeEffectDef BlastDamageType = (StandardDamageTypeEffectDef)Repo.GetDef("a406f7e6-874b-0b44-fa58-cf47b869fc7d");
            //Clone FireVulnerability for the rest of the stats
            DamageMultiplierAbilityDef BlastVulnerability = (DamageMultiplierAbilityDef)Repo.CreateDef("b684e615-87fd-4085-8556-51a4b5308aff", FireVulnerability);
            BlastVulnerability.name = "BlastVulnerability_DamageMultiplierAbilityDef";
            BlastVulnerability.DamageTypeDef = BlastDamageType;
            BlastVulnerability.ViewElementDef = BlastVulnerabilityVED(FireVulnerability.ViewElementDef);

            SuperCharger.Abilities = new AbilityDef[]
            {
                BlastVulnerability,
            };
        }

        private static TacticalAbilityViewElementDef BlastVulnerabilityVED(TacticalAbilityViewElementDef template)
        {
            TacticalAbilityViewElementDef VED = Repo.CreateDef<TacticalAbilityViewElementDef>("839bedb1-16c4-4ea7-9cb3-6b99424f9526", template);
            VED.name = "E_View [BlastVulnerability_DamageMultiplierAbilityDef]";
            VED.DisplayName1 = new LocalizedTextBind("NJ_BLASTVULN_NAME");
            VED.Description = new LocalizedTextBind("NJ_BLASTVULN_DESC");
            VED.SmallIcon = VED.LargeIcon = Helper.CreateSpriteFromImageFile("BlastVulnerability.png");
            return VED;
        }
    }
}