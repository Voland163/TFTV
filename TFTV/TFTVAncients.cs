using Base;
using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Linq;
using static UnityStandardAssets.Utility.TimedObjectActivator;

namespace TFTV
{
    internal class TFTVAncients
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        /*
         public static void AddDrillBack(TacticalLevelController controller)
         {
             try
             {
                 if (controller.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                 {
                     TFTVLogger.Always("Found ancients");

                     BashAbilityDef drillBash =DefCache.GetDef<BashAbilityDef>("Guardian_Drill_AbilityDef");
                     ShootAbilityDef beam = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");

                     WeaponDef drill = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
                     EquipmentDef leftShield = DefCache.GetDef<EquipmentDef>("HumanoidGuardian_LeftShield_EquipmentDef");
                    WeaponDef beamHead = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                     Equipment head = new Equipment();

                     foreach (TacticalActor tacticalActor in controller.GetFactionByCommandName("anc").TacticalActors)
                     {
                         bool foundLeftShield = false;

                         foreach (Equipment item in tacticalActor.Equipments.Equipments)
                         {
                             if (item.TacticalItemDef.Equals(leftShield))
                             {
                                 TFTVLogger.Always("Found leftShield");
                                 foundLeftShield = true;

                             }

                             if (item.TacticalItemDef.Equals(beamHead))
                             {
                                 TFTVLogger.Always("Found beam");
                                 head = item;

                             }

                         }

                         if (!foundLeftShield) 
                         {
                            TFTVLogger.Always("Found a driller");
                          //   if (drill == null)
                          //   {
                                TFTVLogger.Always("Should add a drill");
                                tacticalActor.Equipments.AddItem(drillSave);
                         //    }


                         }
                        
                     }
                 }
             }
             catch (Exception e)
             {
                 TFTVLogger.Error(e);
             }
         }

        private static Equipment drillSave = new Equipment(); 
        [HarmonyPatch(typeof(TacticalLevelController), "ActorEnteredPlay")]
        public static class TacticalLevelController_ActorEnteredPlay_Ancients_Patch
        {
            public static void Postfix(TacticalActorBase actor, TacticalLevelController __instance)
            {
                try
                {
                    if (__instance.Factions.Any(f => f.Faction.FactionDef.MatchesShortName("anc")))
                    {
                        TacticalFaction ancients = __instance.GetFactionByCommandName("anc");

                        if (actor is TacticalActor && actor.TacticalFaction == ancients) 
                        {
                            BashAbilityDef drillBash = DefCache.GetDef<BashAbilityDef>("Guardian_Drill_AbilityDef");
                            ShootAbilityDef beam = DefCache.GetDef<ShootAbilityDef>("Guardian_Beam_ShootAbilityDef");

                            WeaponDef drillDef = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Drill_WeaponDef");
                            WeaponDef beamDef = DefCache.GetDef<WeaponDef>("HumanoidGuardian_Head_WeaponDef");
                            Equipment head = new Equipment();
                            Equipment drill = new Equipment();


                            TacticalActor tacticalActor = actor as TacticalActor;

                            bool foundDriller = false;

                            foreach (Equipment item in tacticalActor.Equipments.Equipments)
                            {
                                if (item.TacticalItemDef.Equals(drillDef))
                                {
                                    TFTVLogger.Always("Found driller");
                                    foundDriller = true;
                                    drill = item;

                                }

                                if (item.TacticalItemDef.Equals(beamDef))
                                {
                                    TFTVLogger.Always("Found beam");
                                    head = item;

                                }

                            }

                            if (foundDriller)
                            {
                              
                                TFTVLogger.Always("Drill removed");
                                if (drill != null)
                                {
                                    drillSave = drill;
                                    drill.DestroyAll();
                                }

                            }




                        }
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
        */

    }
}
