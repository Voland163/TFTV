using HarmonyLib;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Base.Defs;
using PhoenixPoint.Common.Core;
using UnityEngine;

namespace TFTV
{
    internal class TFTVExperienceDistribution
    {

        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        [HarmonyPatch(typeof(TacticalContribution), "AddContribution")]
        public static class TFTV_TacticalContribution_AddContribution
        {
            public static void Postfix(TacticalContribution __instance, int cp, TacticalActorBase ____actor)
            {
                try
                {

                    if (cp <= 0)
                    {
                        return;
                    }

                    if (!____actor.Status.HasStatus<MindControlStatus>() || ____actor.Status.GetStatus<MindControlStatus>().ControllerActor == null)
                    {
                        return;
                    }

                    TacticalActor controllingActor = ____actor.Status.GetStatus<MindControlStatus>().ControllerActor;

                    // TFTVLogger.Always($"{controllingActor.name} has {controllingActor.Contribution.Contribution} CP");

                    FieldInfo contributionFieldInfo = typeof(TacticalContribution).GetField("_contribution", BindingFlags.NonPublic | BindingFlags.Instance);

                    TacticalContribution controllingActorContribution = controllingActor.Contribution;

                    int controllingActorContributionValue = controllingActorContribution.Contribution + cp / 2;

                    contributionFieldInfo.SetValue(controllingActorContribution, controllingActorContributionValue);

                    // TFTVLogger.Always($"{controllingActor.name} now has {controllingActor.Contribution.Contribution} CP");

                    Debug.Log($"+{cp} cp for {controllingActor.name} (through Mind Controlled Unit).");


                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }
        }
    }
}
