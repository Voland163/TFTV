using Base.Defs;
using Base.Entities.Abilities;
using Base.Entities.Effects;
using Base.Entities.Effects.ApplicationConditions;
using Base.Entities.Statuses;
using Base.UI;
using Code.PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Statuses;
using System.Collections.Generic;

namespace TFTVVehicleRework.Armadillo
{
    public static class Purgatory
    {
        private static readonly DefRepository Repo = ArmadilloMain.Repo;

        internal static SharedDamageKeywordsDataDef keywords = VehiclesMain.keywords;

        //"E_Status [ArmourBreak_AbilityDef]"
        internal static readonly AddAttackBoostStatusDef ArmourBreak = (AddAttackBoostStatusDef)Repo.GetDef("291db698-9274-0e11-761a-9a9438cd246f");
        public static void Change()
        {
            // "NJ_Armadillo_Purgatory_GroundVehicleWeaponDef"
            GroundVehicleWeaponDef VanillaPurg = (GroundVehicleWeaponDef)Repo.GetDef("3986d735-5c23-ef24-6983-7d0132068f1b");
            VanillaPurg.ChargesMax = 8;
            VanillaPurg.DamagePayload.DamageValue = 0f;
            VanillaPurg.DamagePayload.DamageKeywords = new List<DamageKeywordPair>
            {
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.BlastKeyword,
                    Value = 20f
                },
                new DamageKeywordPair
                {
                    DamageKeywordDef = keywords.ShreddingKeyword,
                    Value = 5f
                },
            };

            //"LaunchGrenade_ShootAbilityDef"
            ShootAbilityDef LaunchGrenade = (ShootAbilityDef)Repo.GetDef("81fbb5db-1b12-b8f4-998e-6591f0771a2d");
            VanillaPurg.Abilities = new AbilityDef[]
            {
                LaunchGrenade,
                DefaultAmmoAbility(),
                LoadIncendiaryAbility(),
                LoadConcussionAbility(),
            };
        }

        private static ApplyStatusAbilityDef DefaultAmmoAbility()
        {
            ApplyStatusAbilityDef DefaultAmmo = (ApplyStatusAbilityDef)Repo.GetDef("b4ea1c6f-0107-422c-a3f7-8c60ab4609ab");
            if (DefaultAmmo == null)
            {
                ApplyStatusAbilityDef FastUse = (ApplyStatusAbilityDef)Repo.GetDef("3f8b32f5-6084-f544-aba0-c98af7db93c3"); //"FastUse_AbilityDef"
                DefaultAmmo = Repo.CreateDef<ApplyStatusAbilityDef>("b4ea1c6f-0107-422c-a3f7-8c60ab4609ab", FastUse);
                DefaultAmmo.name = "DefaultIncendiaryAmmo_AbilityDef";
                DefaultAmmo.ViewElementDef = null;
                DefaultAmmo.AnimType = -1;
                DefaultAmmo.StatusDef = IncendiaryAmmoStatus();
            }
            return DefaultAmmo;
        }

        private static ApplyEffectAbilityDef LoadConcussionAbility()
        {
            //"Hacking_Cancel_AbilityDef" has Self_TargetingDataDef
            ApplyEffectAbilityDef HackingCancel = (ApplyEffectAbilityDef)Repo.GetDef("cf51a6d9-dad7-08a4-eb1e-063076f63b8b");
            ApplyEffectAbilityDef LoadConcussionAmmo = Repo.CreateDef<ApplyEffectAbilityDef>("4944a4d3-18fb-4bc3-a573-c17d8445affb", HackingCancel);
            LoadConcussionAmmo.ActionPointCost = 0.25f;
            LoadConcussionAmmo.ViewElementDef = ConcussionVED();         
            LoadConcussionAmmo.EffectDef = ConcussionEffects();
            LoadConcussionAmmo.DisablingStatuses = new StatusDef[]
            {
                ConcussionAmmoStatus()
            };
            return LoadConcussionAmmo;
        }

        private static ApplyEffectAbilityDef LoadIncendiaryAbility()
        {
            //"Hacking_Cancel_AbilityDef" has Self_TargetingDataDef
            ApplyEffectAbilityDef HackingCancel = (ApplyEffectAbilityDef)Repo.GetDef("cf51a6d9-dad7-08a4-eb1e-063076f63b8b");
            ApplyEffectAbilityDef LoadIncendiaryAmmo = Repo.CreateDef<ApplyEffectAbilityDef>("af0b60cb-cb4b-4a54-9f1d-e58269fe4ea1", HackingCancel);
            LoadIncendiaryAmmo.name = "LoadIncendiaryAmmo_AbilityDef";
            LoadIncendiaryAmmo.ActionPointCost = 0.25f;
            LoadIncendiaryAmmo.ViewElementDef = IncendiaryVED();
            LoadIncendiaryAmmo.EffectDef = IncendiaryEffects();
            LoadIncendiaryAmmo.DisablingStatuses = new StatusDef[]
            {
                IncendiaryAmmoStatus()
            };
            return LoadIncendiaryAmmo;
        }

        
        private static MultiEffectDef IncendiaryEffects()
        {
            MultiEffectDef Effects = Repo.CreateDef<MultiEffectDef>("13632ea9-443d-477a-bb09-33357e788643");
            Effects.name = "LoadIncendiaryAmmo_MultiEffectDef";
            Effects.ApplicationConditions = new EffectConditionDef[]{};
            Effects.PreconsiderApplicationConditions = false;
            Effects.EffectDefs = new EffectDef[]
            {
                IncendiaryStatusEffect(),
                RemoveConcussion(),
            };
            return Effects;
        }

        private static StatusEffectDef IncendiaryStatusEffect()
        {
            StatusEffectDef IncendiaryStatusEffect = Repo.CreateDef<StatusEffectDef>("5041dc05-a6a4-443a-ab5a-c6e27b35eb00");
            IncendiaryStatusEffect.ApplicationConditions = new EffectConditionDef[]{};
            IncendiaryStatusEffect.name = "LoadIncendiaryAmmo_StatusEffectDef";
            IncendiaryStatusEffect.StatusDef = IncendiaryAmmoStatus();
            return IncendiaryStatusEffect;
        }

        private static AddAttackBoostStatusDef IncendiaryAmmoStatus()
        {
            AddAttackBoostStatusDef IncendiaryStatus = (AddAttackBoostStatusDef)Repo.GetDef("a396e688-03c3-404c-a5ab-6534e8628e27");
            if (IncendiaryStatus == null)
            {
                IncendiaryStatus = Repo.CreateDef<AddAttackBoostStatusDef>("a396e688-03c3-404c-a5ab-6534e8628e27", ArmourBreak);
                IncendiaryStatus.name = "IncendiaryAmmo_AttackBoostStatusDef";
                IncendiaryStatus.EffectName = "IncendiaryAmmo";
                IncendiaryStatus.Visuals = IncendiaryVED();
                IncendiaryStatus.ExpireOnEndOfTurn = false;
                IncendiaryStatus.DurationTurns = -1;
                IncendiaryStatus.WeaponTagFilter = null;
                IncendiaryStatus.NumberOfAttacks = -1;
                IncendiaryStatus.DamageKeywordPairs = new DamageKeywordPair[]
                {
                    new DamageKeywordPair
                    {
                        DamageKeywordDef = keywords.BurningKeyword,
                        Value = 40f
                    }
                };
            }
            return IncendiaryStatus;
        }

        private static StatusRemoverEffectDef RemoveConcussion()
        {
            StatusRemoverEffectDef RemoveConcussion = Repo.CreateDef<StatusRemoverEffectDef>("fc914ee1-8162-4d20-aba3-d957b8d5bc13");
            RemoveConcussion.ApplicationConditions = new EffectConditionDef[]{};
            RemoveConcussion.StatusToRemove = "ConcussionAmmo";
            return RemoveConcussion;
        }

        private static MultiEffectDef ConcussionEffects()
        {
            MultiEffectDef Effects = Repo.CreateDef<MultiEffectDef>("ccc8590a-6bea-4c54-9641-0cdd45ed6b17");
            Effects.name = "LoadConcussionAmmo_MultiEffectDef";
            Effects.ApplicationConditions = new EffectConditionDef[]{};
            Effects.PreconsiderApplicationConditions = false;
            Effects.EffectDefs = new EffectDef[]
            {
                ConcussionStatusEffect(),
                RemoveIncendiary(),
            };
            return Effects;
        }

        private static StatusEffectDef ConcussionStatusEffect()
        {
            StatusEffectDef ConcussionStatusEffect = Repo.CreateDef<StatusEffectDef>("cdaa8404-1f5f-4f09-be62-734ed5cf5aee");
            ConcussionStatusEffect.name = "LoadConcussionAmmo_StatusEffectDef";
            ConcussionStatusEffect.ApplicationConditions = new EffectConditionDef[]{};
            ConcussionStatusEffect.StatusDef = ConcussionAmmoStatus();
            return ConcussionStatusEffect;
        }

        private static AddAttackBoostStatusDef ConcussionAmmoStatus()
        {
            AddAttackBoostStatusDef ConcussionBoost = (AddAttackBoostStatusDef)Repo.GetDef("1b378c88-9d5a-4118-afb9-4e594e6f7c71");
            if (ConcussionBoost == null)
            {
                ConcussionBoost = Repo.CreateDef<AddAttackBoostStatusDef>("1b378c88-9d5a-4118-afb9-4e594e6f7c71", ArmourBreak);
                ConcussionBoost.name = "ConcussionAmmo_AttackBoostStatusDef";
                ConcussionBoost.EffectName = "ConcussionAmmo";
                ConcussionBoost.Visuals = ConcussionVED();
                ConcussionBoost.ExpireOnEndOfTurn = false;
                ConcussionBoost.DurationTurns = -1;
                ConcussionBoost.WeaponTagFilter = null;
                ConcussionBoost.NumberOfAttacks = -1;
                ConcussionBoost.DamageKeywordPairs = new DamageKeywordPair[]
                {
                    new DamageKeywordPair
                    {
                        DamageKeywordDef = keywords.SonicKeyword,
                        Value = 30f
                    }
                };
            }
            return ConcussionBoost;
        }
        
        private static StatusRemoverEffectDef RemoveIncendiary()
        {
            StatusRemoverEffectDef RemoveIncendiary = Repo.CreateDef<StatusRemoverEffectDef>("bd465cb0-3672-43c0-b5a8-52096157d0a9");
            RemoveIncendiary.ApplicationConditions = new EffectConditionDef[]{};
            RemoveIncendiary.StatusToRemove = "IncendiaryAmmo";
            return RemoveIncendiary;
        }

        private static TacticalAbilityViewElementDef IncendiaryVED()
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("79a5af63-76d8-4e38-94f4-77d4d5461d4b");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("79a5af63-76d8-4e38-94f4-77d4d5461d4b", ArmourBreak.Visuals);
                VED.name = "E_ViewElement [LoadIncendiaryAmmo_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("NJ_INCENDIARY_NAME");
                VED.Description = new LocalizedTextBind("NJ_INCENDIARY_DESC");
                VED.ShowInInventoryItemTooltip = true;
                VED.SmallIcon = ((TacticalAbilityDef)Repo.GetDef("eba25483-d500-bf74-79a6-be50414706aa")).ViewElementDef.SmallIcon; //"Mutoid_FireExplode_AbilityDef"
                VED.LargeIcon = VED.SmallIcon;
            }
            return VED;
        }

        private static TacticalAbilityViewElementDef ConcussionVED()
        {
            TacticalAbilityViewElementDef VED = (TacticalAbilityViewElementDef)Repo.GetDef("1ecd9181-d635-43c7-9b41-a9f5e44d6bfa");
            if (VED == null)
            {
                VED = Repo.CreateDef<TacticalAbilityViewElementDef>("1ecd9181-d635-43c7-9b41-a9f5e44d6bfa", ArmourBreak.Visuals);
                VED.name = "E_ViewElement [LoadConcussionAmmo_AbilityDef]";
                VED.DisplayName1 = new LocalizedTextBind("NJ_CONCUSSION_NAME");
                VED.Description = new LocalizedTextBind("NJ_CONCUSSION_DESC");
                VED.ShowInInventoryItemTooltip = true;
                VED.SmallIcon = ((TacEffectStatusDef)Repo.GetDef("bc7c3977-a34f-0594-2aa0-6fc76c4351d4")).Visuals.SmallIcon; //ActorStunned_StatusDef
                VED.LargeIcon = VED.SmallIcon;
            }
            return VED;
        }
    }
}