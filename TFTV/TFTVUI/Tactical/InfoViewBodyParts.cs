using Base.Defs;
using Base.Entities.Statuses;
using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using System;
using System.Collections.Generic;
using System.Linq;
using static PhoenixPoint.Tactical.Entities.TacticalActorViewBase;

namespace TFTV.TFTVUI.Tactical
{
    /// <summary>
    /// Body part naming and per-part status icons for the character info view (UIStateCharacterStatus).
    /// </summary>
    internal static class InfoViewBodyParts
    {
        /// <summary>
        /// Vanilla gives every arm slot KEY_ARM_NAME and every leg slot KEY_LEG_NAME, so a left and a right
        /// limb read the same ("ARM", "ARM") in the info view, the status forecasts and everywhere else a
        /// slot is named. Sided slots get their own key here ("L ARM", "R ARM", "L FRONT LEG"...), the way
        /// vanilla already does for the Swarmer's wings (KEY_LEFT_WING_NAME). Unsided slots ("Legs") keep
        /// the vanilla key.
        /// </summary>
        internal static class LimbSideLabels
        {
            private const string ArmKey = "KEY_ARM_NAME";
            private const string LegKey = "KEY_LEG_NAME";

            private static readonly HashSet<string> LegKeys = new HashSet<string> { LegKey };

            internal static void ApplyToSlotDefs()
            {
                try
                {
                    int renamed = 0;

                    foreach (ItemSlotDef slotDef in TFTVMain.Repo.GetAllDefs<ItemSlotDef>())
                    {
                        string key = slotDef?.DisplayName?.LocalizationKey;
                        bool isArm = key == ArmKey;
                        bool isLeg = key == LegKey;

                        if ((!isArm && !isLeg) || string.IsNullOrEmpty(slotDef.SlotName))
                        {
                            continue;
                        }

                        string side = slotDef.SlotName.Contains("Left") ? "LEFT_" : slotDef.SlotName.Contains("Right") ? "RIGHT_" : null;

                        if (side == null)
                        {
                            continue;
                        }

                        string position = string.Empty;

                        if (isLeg)
                        {
                            if (slotDef.SlotName.Contains("Front"))
                            {
                                position = "FRONT_";
                            }
                            else if (slotDef.SlotName.Contains("Rear") || slotDef.SlotName.Contains("Back"))
                            {
                                position = "REAR_";
                            }
                            else if (slotDef.SlotName.Contains("Middle"))
                            {
                                position = "MIDDLE_";
                            }
                        }

                        string newKey = $"TFTV_SLOT_{side}{position}{(isArm ? "ARM" : "LEG")}";
                        slotDef.DisplayName = new LocalizedTextBind(newKey);
                        renamed++;

                        if (isLeg)
                        {
                            LegKeys.Add(newKey);
                        }
                    }

                    TFTVLogger.Always($"[LimbSideLabels] Gave {renamed} left/right limb slots their own names.");
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            /// <summary>A leg slot of any side, by its key rather than its (language-dependent) text.</summary>
            internal static bool IsLegSlot(ItemSlot slot)
            {
                string key = slot?.ItemSlotDef?.DisplayName?.LocalizationKey;
                return key != null && LegKeys.Contains(key);
            }
        }

        /// <summary>
        /// The info view puts a status's icon on each body part row listed in its TargetSlots, and only for
        /// statuses flagged for the body part list. Burning is neither: it is one status on the whole body,
        /// so no row showed it even though fire damages every part. Here it is listed against every health
        /// slot. Only UIStateCharacterStatus.SetData calls this method.
        /// </summary>
        [HarmonyPatch(typeof(TacticalActorViewBase), nameof(TacticalActorViewBase.GetCharacterStatusBodypartStatuses))]
        internal static class TacticalActorViewBase_GetCharacterStatusBodypartStatuses_Patch
        {
            public static void Postfix(StatusComponent ____statusComponent, ref List<StatusInfo> __result)
            {
                try
                {
                    if (____statusComponent == null || __result == null)
                    {
                        return;
                    }

                    FireStatus fire = ____statusComponent.GetStatus<FireStatus>();

                    if (fire == null || fire.Value <= 0f || __result.Any(info => info.Def == fire.TacStatusDef))
                    {
                        return;
                    }

                    CharacterBodyState body = ____statusComponent.GetComponent<CharacterBodyState>();

                    if (body == null)
                    {
                        return;
                    }

                    __result.Add(new StatusInfo
                    {
                        Def = fire.TacStatusDef,
                        Value = fire.Value,
                        Limit = fire.Limit,
                        TargetSlots = body.GetHealthSlots().Select(slot => slot.GetSlotName()).ToList(),
                    });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
