using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTV.TFTVLaserWeapons
{
    internal class LaserWeaponsFilter
    {
        [HarmonyPatch(typeof(UIModuleSoldierEquip), "ClassFilter")]
        public static class UIModuleSoldierEquipClassFilterPatch
        {
            private static readonly AccessTools.FieldRef<UIModuleSoldierEquip, List<ClassTagDef>> ClassFilterRef = AccessTools.FieldRefAccess<UIModuleSoldierEquip, List<ClassTagDef>>("_classFilter");

            private static readonly AccessTools.FieldRef<UIModuleSoldierEquip, List<SpecializationDef>> FilterCharacterSpecializationsRef = AccessTools.FieldRefAccess<UIModuleSoldierEquip, List<SpecializationDef>>("_filterCharacterSpecializations");

            [HarmonyPrefix]
            private static bool Prefix(UIModuleSoldierEquip __instance, TacticalItemDef def, ref bool __result)
            {
                try
                {

                    // No TacticalItemDef: an aircraft module, which the Aircraft Rework keeps in the faction storage.
                    // It never belongs on the soldier equip screen, so it is filtered out without a warning.
                    if (def == null)
                    {
                        __result = false;
                        return false;
                    }
                    List<ClassTagDef> list = ClassFilterRef(__instance) ?? new List<ClassTagDef>();
                    List<SpecializationDef> list2 = FilterCharacterSpecializationsRef(__instance) ?? new List<SpecializationDef>();
                    bool flag = def.Tags != null && (def.Tags.Intersect(list).Any<GameTagDef>() || def.Tags.Contains(__instance.AllClassesTag) || IsAmmoForSelectedClass(def, list));
                    bool flag2 = list2.Count == list.Count;
                    __result = flag || flag2;
                    return false;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                    throw;
                }
            }

            private static bool IsAmmoForSelectedClass(TacticalItemDef ammoDef, List<ClassTagDef> selectedClasses)
            {
                try
                {

                    List<TacticalItemDef> list;
                    if (AmmoWeaponDatabase.AmmoToWeaponDictionary == null || !AmmoWeaponDatabase.AmmoToWeaponDictionary.TryGetValue(ammoDef, out list))
                    {
                        return false;
                    }
                    return list.Any((TacticalItemDef weaponDef) => weaponDef != null && weaponDef.Tags != null && weaponDef.Tags.Intersect(selectedClasses).Any<GameTagDef>());
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
