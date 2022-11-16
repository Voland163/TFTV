using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static UnityStandardAssets.Utility.TimedObjectActivator;

namespace TFTV
{
    internal class TFTVExperimental
    {


        public static void CheckForMutoids(TacticalLevelController controller)
        {
            try
            {

                DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Medkit_AbilityDef]").Origin.CullTargetTags.Clear();
                // DefCache.GetDef<TacticalTargetingDataDef>("E_TargetingData [Mutoid_CannotBeHealed_TacticalAbilityDef]").Origin.Range = 0;

               /* TacticalFaction phoenix = controller.GetFactionByCommandName("PX");

                foreach (TacticalActorBase actor in phoenix.Actors)
                {
                    if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef")) &&
                         actor.GetAbilityWithDef<TacticalAbility>(DefCache.GetDef<PassiveModifierAbilityDef>("Mutoid_CannotBeHealed_TacticalAbilityDef")) != null)

                    {
                        TFTVLogger.Always("This mutoid " + actor.DisplayName + " passed the checks");
                        TacticalActor tacticalActor = actor as TacticalActor;

                        tacticalActor.RemoveAbility(DefCache.GetDef<PassiveModifierAbilityDef>("Mutoid_CannotBeHealed_TacticalAbilityDef"));

                    }

                }*/
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }


        }

        public static TacticalActor mutoidReceivingHealing = null;

        [HarmonyPatch(typeof(HealAbility), "HealTargetCrt")]

        public static class HealAbility_HealTargetCrt_Mutoid_Patch
        {

            public static void Prefix(PlayingAction action)
            {
                try
                {
                    if ((TacticalActor)((TacticalAbilityTarget)action.Param).Actor != null) 
                    {
                        TacticalActor actor = (TacticalActor)((TacticalAbilityTarget)action.Param).Actor;
                        if (actor.HasGameTag(DefCache.GetDef<GameTagDef>("Mutoid_ClassTagDef"))) 
                        {
                            mutoidReceivingHealing = actor;
                        }
                    }
                       
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(HealAbility), "get_GeneralHealAmount")]

        public static class HealAbility_Mutoid_Patch
        {

            public static void Postfix(HealAbility __instance, ref float __result)
            {
                try
                { 

                    if (mutoidReceivingHealing != null) 
                    {
                        __result = 0;
                    }

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        [HarmonyPatch(typeof(DamageAccumulation), "GenerateStandardDamageTargetData")]
        class DamageMultiplier_BugFix
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> listInstructions = new List<CodeInstruction>(instructions);
                IEnumerable<CodeInstruction> insert = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Div)
            };

                // insert after each of the first 3 divide opcodes
                int divs = 0;
                for (int index = 0; index < instructions.Count(); index++)
                {
                    if (listInstructions[index].opcode == OpCodes.Div)
                    {
                        listInstructions.InsertRange(index + 1, insert);
                        index += 2;
                        divs++;
                        if (divs == 3)
                        {
                            break;
                        }
                    }
                }

                if (divs != 3)
                {
                    return instructions; // didn't find three, function signature changed, abort
                }
                return listInstructions;
            }

        }

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        /* [HarmonyPatch(typeof(GeoMission), "ModifyMissionData")]
         public static class GeoMission_ModifyMissionData_Patch
         {

             public static void Postfix(TacMissionData missionData)
             {
                 try
                 {
                     List <FactionObjectiveDef> listOfFactionObjectives = missionData.MissionType.CustomObjectives.ToList();
                     listOfFactionObjectives.Add(DefCache.GetDef<FactionObjectiveDef>("KillRevenant_Objective"));
                     missionData.MissionType.CustomObjectives = listOfFactionObjectives.ToArray();                    
                 }
                 catch (Exception e)
                 {
                     TFTVLogger.Error(e);
                 }
             }
         }*/

        //   [Tooltip("This is the number of items unlocked by the player added to each chest")]
        // public RangeDataInt FactionItemsRange = new RangeDataInt(-1, -1);


    }


}

