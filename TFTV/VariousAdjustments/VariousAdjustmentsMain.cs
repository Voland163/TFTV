using Assets.Code.PhoenixPoint.Geoscape.Entities.Sites.TheMarketplace;
using Base.Core;
using Base.Defs;
using Base.Entities.Statuses;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Linq;
using TFTV;

namespace PRMBetterClasses.VariousAdjustments
{
    internal class VariousAdjustmentsMain
    {
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        public static void ApplyChanges()
        {
            try
            {
                // Changes coding down from here
                VariousAdjustments.ApplyChanges();

                WeaponModifications.ApplyChanges();

                // Marketplace settings
                // All prices set to multiplier 1
                //TheMarketplaceSettingsDef marketplaceSettings = Repo.GetAllDefs<TheMarketplaceSettingsDef>().FirstOrDefault(msd => msd.name.Equals("TheMarketplaceSettingsDef"));
                //for (int i = 0; i < marketplaceSettings.TheMarketplaceItemPriceMultipliers.Length; i++)
                //{
                //    marketplaceSettings.TheMarketplaceItemPriceMultipliers[i].PriceMultiplier = 1;
                //}
                //// Change KE Buggy price
                //GeoMarketplaceItemOptionDef geoMarketplaceBuggy = Repo.GetAllDefs<GeoMarketplaceItemOptionDef>().FirstOrDefault(mio => mio.name.Equals(""));
                //geoMarketplaceBuggy.MinPrice = 600;
                //geoMarketplaceBuggy.MaxPrice = 700;
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
            }
        }

        // -------------------------------------------------------------------------
        // Harmony patch(es) to fix that Project Hekate deletes the viral resistance ability of Mutoids and Resistor head mutation
        // Cause is that all use the same ability and so Hekate faction status deletes the one from Mutoids and Resistor head
        // AddAbilityStatusDef.OnApply() ff
        // or create (clone) a new virus resistance ability for Hekate or Mutoids
        // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
        // Harmony patch to fix double reduction when resistances are present (mainly Nanotech)
        [HarmonyPatch(typeof(DamageOverTimeStatus), "LowerDamageOverTimeLevel")]
        internal static class BC_DamageOverTimeStatus_LowerDamageOverTimeLevel_Patch
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static bool Prefix(DamageOverTimeStatus __instance, float amount = 1f)
            {
                // This part doubles the reduction if any resistance is given (damage multiplier < 1)
                //if (Utl.LesserThan(__instance.GetDamageMultiplier(), 1f, 1E-05f))
                //{
                //    amount *= 2f;
                //}
                __instance.AddDamageOverTimeLevel(-amount);
                if (__instance.IntValue <= 0)
                {
                    __instance.RequestUnapply(__instance.StatusComponent);
                    return false;
                }
                _ = AccessTools.Method(typeof(TacStatus), "OnValueChanged").Invoke(__instance, null);
                return false;
            }
        }
        // -------------------------------------------------------------------------

        // -------------------------------------------------------------------------
        // Harmony patches to deactivate automatic standby and switch to another character in tactical missions
        [HarmonyPatch(typeof(TacticalActor), "TrySetStandBy")]
        internal static class BC_TacticalActor_TryGetStandBy_Patch
        {
            public static bool Prepare()
            {
                return TFTVMain.Main.Config.DeactivateTacticalAutoStandby;
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            // If actor NOT has ended turn (manually, OW, HD) set result to false and don't excecute original method TrySetStandBy() (return false)
            private static bool Prefix(TacticalActor __instance, ref bool __result)
            {
                return __instance.HasEndedTurn || (__result = false);
            }
        }
        [HarmonyPatch(typeof(TacticalActorBase), "CanAct", new Type[0])]
        internal static class BC_TacticalActorBase_CanAct_Patch
        {
            public static bool Prepare()
            {
                return TFTVMain.Main.Config.DeactivateTacticalAutoStandby;
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
            private static void Postfix(TacticalActorBase __instance, ref bool __result)
            {
                StatusDef panicked = DefCache.GetDef<StatusDef>("Panic_StatusDef");
                StatusDef overWatch = DefCache.GetDef<StatusDef>("Overwatch_StatusDef");
                StatusDef hunkerDown = DefCache.GetDef<StatusDef>("E_Status [HunkerDown_AbilityDef]");
                // Check if actor is from viewer faction (= player) and several conditions are not met
                SharedData Shared = GameUtl.GameComponent<SharedData>();
                if (__instance.IsFromViewerFaction && !(__instance.IsDead
                                                        || __instance.Status.HasStatus(Shared.SharedGameTags.StandByStatusDef)
                                                        || __instance.Status.HasStatus(Shared.SharedGameTags.ParalyzedStatus)
                                                        || __instance.Status.HasStatus(panicked)
                                                        || __instance.Status.HasStatus(overWatch)
                                                        || __instance.Status.HasStatus(hunkerDown)
                                                        || __instance.Status.HasStatus<EvacuatedStatus>()))
                {
                    //  Set return value __result = true => no auto switch to other character after any action
                    __result = true;
                }
            }
        }
        // -------------------------------------------------------------------------
    }
}
