using Base;
using Base.AI;
using Base.Audio;
using Base.Core;
using Base.Entities;
using Base.Entities.Effects;
using Base.Entities.Statuses;
using Base.Eventus;
using Base.Levels;
using Base.Rendering.ObjectRendering;
using Base.Utils.Maths;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical;
using PhoenixPoint.Tactical.AI;
using PhoenixPoint.Tactical.AI.Considerations;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Effects;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Entities.Weapons;
using PhoenixPoint.Tactical.Levels;
using PhoenixPoint.Tactical.Levels.FactionObjectives;
using PhoenixPoint.Tactical.UI.SoldierPortraits;
using PhoenixPoint.Tactical.View;
using PhoenixPoint.Tactical.View.ViewModules;
using PhoenixPoint.Tactical.View.ViewStates;
using PRMBetterClasses.SkillModifications;
using SETUtil.Common.Extend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PhoenixPoint.Tactical.Entities.Effects.DamageEffect;
using static PhoenixPoint.Tactical.Entities.SquadPortraitsDef;
using static PhoenixPoint.Tactical.Entities.TacticalActorViewBase;

namespace TFTV.TFTVVanillaFixes.Tactical
{
    internal class ToBeSortedOutTactical
    {
       

    
        internal class SoftlockOnGetStatusesByName
        {
            [HarmonyPatch(typeof(StatusComponent), nameof(StatusComponent.GetStatusesByName))]
            internal static class StatusComponentGetStatusesByNamePatch
            {
                public static bool Prefix(StatusComponent __instance, string statusName, ref IEnumerable<Status> __result)
                {
                    try
                    {
                        if (__instance == null || string.IsNullOrWhiteSpace(statusName))
                        {
                            __result = Enumerable.Empty<Status>();
                            return false;
                        }

                        string normalizedName = statusName.Trim();
                        if (normalizedName.Length == 0)
                        {
                            __result = Enumerable.Empty<Status>();
                            return false;
                        }

                        IEnumerable<Status> statuses = __instance.Statuses ?? Enumerable.Empty<Status>();
                        __result = statuses.Where(status =>
                            status?.Def?.EffectName != null &&
                            status.Def.EffectName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

                        return false;
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
}
