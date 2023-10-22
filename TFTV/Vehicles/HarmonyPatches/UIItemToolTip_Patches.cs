using Base.Core;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Equipments;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Tactical.Entities.DamageKeywords;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Reflection;
using UnityEngine;

namespace TFTVVehicleRework.HarmonyPatches
{
    [HarmonyPatch(typeof(UIItemTooltip), "SetTacItemStats")]
    internal static class UIItemTooltip_SetModuleStats_Patch
    {
        public static LocalizedTextBind HealthText = new LocalizedTextBind("UI_HITPOINTS");
        public static void Postfix(UIItemTooltip __instance, TacticalItemDef tacItemDef, bool secondObject, int subItemIndex = -1)
        {
            GroundVehicleModuleDef moduledef = tacItemDef as GroundVehicleModuleDef;
            if (moduledef == null)
            {
                return;
            }

            Type[] typeParameters = new Type[] {typeof(LocalizedTextBind), typeof(bool), typeof(object), typeof(object), typeof(Sprite), typeof(int)};
            MethodInfo SetStat = AccessTools.Method(typeof(UIItemTooltip), "SetStat", typeParameters);
            if (SetStat == null)
            {
                return;
            } 
            object[] parameters;
            SharedDamageKeywordsDataDef sharedDamageKeywords = GameUtl.GameComponent<SharedData>().SharedDamageKeywords;

            if (moduledef.BodyPartAspectDef.Endurance > 1f)
            {
                string text = $"{moduledef.BodyPartAspectDef.Endurance * 10f}";
                parameters = new object[] {HealthText, secondObject, text, text, null, subItemIndex};
                SetStat.Invoke(__instance, parameters);
            }
            float UnitsInside = moduledef.BodyPartAspectDef.GetStatModification(StatModificationTarget.UnitsInside).Value;
            if(UnitsInside != 0)
            {
                parameters = new object[] {__instance.SeatsName, secondObject, UIUtil.StatsWithSign(UnitsInside), UnitsInside, null, subItemIndex};
                SetStat.Invoke(__instance, parameters);
            }
        }
    }
}