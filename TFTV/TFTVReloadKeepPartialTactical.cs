using Base.Core;
using Base.Entities;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TFTV
{
   /* internal class TFTVReloadKeepPartialTactical
    {
        [HarmonyPatch(typeof(ReloadAbility), "ReloadCrt")]
        public static class ReloadAbilityPartialMagPatch
        {
            private static readonly MethodInfo ChooseEquipmentAndAmmoMethod = AccessTools.Method(typeof(ReloadAbility), "ChooseEquipmentAndAmmo");

            public static void Postfix(ReloadAbility __instance, PlayingAction action, ref IEnumerator<NextUpdate> __result)
            {
                if (__result == null)
                {
                    return;
                }
                __result = new ReloadCrtWrapper(__instance, action, __result);
            }

            private sealed class ReloadCrtWrapper : IEnumerator<NextUpdate>
            {
                private readonly ReloadAbility _ability;
                private readonly PlayingAction _action;
                private readonly IEnumerator<NextUpdate> _inner;
                private bool _didPreUnload;

                public ReloadCrtWrapper(ReloadAbility ability, PlayingAction action, IEnumerator<NextUpdate> inner)
                {
                    _ability = ability;
                    _action = action;
                    _inner = inner;
                }

                public NextUpdate Current
                {
                    get
                    {
                        return _inner.Current;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public bool MoveNext()
                {
                    if (!_didPreUnload)
                    {
                        _didPreUnload = true;
                        TryPreUnloadMagazines();
                    }
                    return _inner.MoveNext();
                }

                public void Reset()
                {
                    _inner.Reset();
                }

                public void Dispose()
                {
                    _inner.Dispose();
                }

                private void TryPreUnloadMagazines()
                {
                    if (_ability == null || _action == null)
                    {
                        return;
                    }
                    TacticalAbilityTarget abilityTarget = _action.Param as TacticalAbilityTarget;
                    if (abilityTarget == null)
                    {
                        return;
                    }
                    if (_ability.ReloadAbilityDef.TargetingDataDef.Origin.TargetResult == TargetResult.Actor)
                    {
                        abilityTarget = _ability.GetTargets().FirstOrDefault((TacticalAbilityTarget x) => x.Actor == abilityTarget.Actor);
                        if (abilityTarget == null)
                        {
                            return;
                        }
                    }
                    Equipment equipment = null;
                    TacticalItem ammoClip = null;
                    if (ChooseEquipmentAndAmmoMethod != null)
                    {
                        object[] parameters = new object[]
                        {
                        abilityTarget,
                        null,
                        null
                        };
                        ChooseEquipmentAndAmmoMethod.Invoke(_ability, parameters);
                        equipment = parameters[1] as Equipment;
                        ammoClip = parameters[2] as TacticalItem;
                    }
                    if (equipment == null || ammoClip == null)
                    {
                        return;
                    }
                    AmmoManager ammo = equipment.CommonItemData.Ammo;
                    if (ammo == null || ammo.CurrentCharges <= 0)
                    {
                        return;
                    }
                    List<ICommonItem> unloadedMagazines = ammo.UnloadMagazines();
                    TryStoreUnloadedMagazines(_ability, unloadedMagazines);
                }
            }

            private static void TryStoreUnloadedMagazines(ReloadAbility ability, IEnumerable<ICommonItem> unloadedMagazines)
            {
                if (ability == null || unloadedMagazines == null)
                {
                    return;
                }
                TacticalActor tacticalActor = ability.TacticalActor;
                if (tacticalActor == null)
                {
                    return;
                }
                TacticalView view = tacticalActor.TacticalLevel.View;
                if (view == null)
                {
                    return;
                }
                EquipmentComponent equipments = tacticalActor.Equipments;
                InventoryComponent inventory = tacticalActor.Inventory;
                int readySlotsAvailable = view.TacticalEquipmentSize - equipments.Items.Count((Item x) => view.CanStayInReadyItems(x.ItemDef));
                int inventorySlotsAvailable = view.TacticalInventorySize - inventory.Items.Count<Item>();
                foreach (ICommonItem commonItem in unloadedMagazines)
                {
                    TacticalItem tacticalItem = commonItem as TacticalItem;
                    if (tacticalItem == null || tacticalItem.CommonItemData.CurrentCharges <= 0)
                    {
                        continue;
                    }
                    bool canStayInReadyItems = view.CanStayInReadyItems(tacticalItem.ItemDef);
                    if (canStayInReadyItems && readySlotsAvailable > 0)
                    {
                        equipments.AddItem(tacticalItem, ability);
                        readySlotsAvailable--;
                    }
                    else if (inventorySlotsAvailable > 0)
                    {
                        inventory.AddItem(tacticalItem, ability);
                        inventorySlotsAvailable--;
                    }
                    else
                    {
                        tacticalItem.Drop(tacticalActor.TacticalLevel.SharedData.FallDownItemContainerDef, tacticalActor);
                    }
                }
            }
        }

    }*/
}
