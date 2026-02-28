using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVIncidents
{
    internal class BiotechMedkit
    {
        [HarmonyPatch(typeof(HealAbility))]
        internal static class HealAbility_MedkitOrganicRestorePatch
        {
            private const string RestoreEnablerTrait = "MedkitOrganicRestore";
            private const float BodyPartRestoreAmount = 10f;

            [HarmonyPostfix]
            [HarmonyPatch("Activate")]
            private static void Activate_Postfix(HealAbility __instance, object parameter)
            {
                if (!(parameter is TacticalAbilityTarget tacticalAbilityTarget) || !(tacticalAbilityTarget.Actor is TacticalActor targetActor))
                {
                    return;
                }
                TacticalActor healer = __instance.TacticalActor;
                if (healer == null || !HasRestoreEnabler(healer))
                {
                    return;
                }
                Equipment equipment = __instance.GetSource<Equipment>() ?? healer.Equipments.SelectedEquipment;
                if (!IsMedkitEquipment(equipment))
                {
                    return;
                }
                ApplyOrganicBodypartRestore(targetActor);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ShouldReturnTarget")]
            private static void ShouldReturnTarget_Postfix(HealAbility __instance, TacticalActor healer, TacticalActor targetActor, ref bool __result)
            {
                if (__result || healer == null || targetActor == null || !HasRestoreEnabler(healer))
                {
                    return;
                }
                Equipment equipment = __instance.GetSource<Equipment>() ?? healer.Equipments.SelectedEquipment;
                if (!IsMedkitEquipment(equipment))
                {
                    return;
                }
                GameTagDef organicTag = CommonHelpers.GetSharedGameTags().Substances.OrganicTag;
                __result = targetActor.BodyState.GetHealthSlots().Any((ItemSlot slot) => slot.GetHealth().Value < slot.GetHealth().Max && (organicTag == null || slot.HasDirectGameTag(organicTag, true)));
            }

            private static bool HasRestoreEnabler(TacticalActor healer)
            {
                return healer.HasAbilityTrait(RestoreEnablerTrait);
            }

            private static bool IsMedkitEquipment(Equipment equipment)
            {
                return equipment != null && equipment.EquipmentDef != null && (equipment.EquipmentDef.name.IndexOf("Medkit", StringComparison.OrdinalIgnoreCase) >= 0 || equipment.EquipmentDef.Tags.Any((GameTagDef tag) => tag != null && tag.name.IndexOf("Medkit", StringComparison.OrdinalIgnoreCase) >= 0));
            }

            private static void ApplyOrganicBodypartRestore(TacticalActor targetActor)
            {
                GameTagDef organicTag = CommonHelpers.GetSharedGameTags().Substances.OrganicTag;
                foreach (ItemSlot itemSlot in targetActor.BodyState.GetHealthSlots())
                {
                    if (itemSlot.GetHealth().Value < itemSlot.GetHealth().Max && (organicTag == null || itemSlot.HasDirectGameTag(organicTag, true)))
                    {
                        itemSlot.GetHealth().Add(BodyPartRestoreAmount);
                    }
                }
            }
        }

    }
}
