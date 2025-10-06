using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static TFTV.HavenRecruitsMain;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsPrice;


namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsUtils
    {


        internal static void SortRecruits(List<RecruitAtSite> list)
        {
            switch (_sortMode)
            {
                case SortMode.Class:
                    list.Sort((a, b) => string.Compare(GetClassName(a.Recruit), GetClassName(b.Recruit), StringComparison.Ordinal));
                    break;
                case SortMode.Level:
                    list.Sort((a, b) => b.Recruit.Level.CompareTo(a.Recruit.Level)); // high to low
                    break;
                case SortMode.Distance:
                    list.Sort((a, b) =>
                    {
                        float ta = RecruitOverlayManager.GetDistanceScore(a.Site);
                        float tb = RecruitOverlayManager.GetDistanceScore(b.Site);

                        // Put unreachable (+∞) at the end
                        bool aInf = float.IsPositiveInfinity(ta);
                        bool bInf = float.IsPositiveInfinity(tb);
                        if (aInf && !bInf) return 1;
                        if (!aInf && bInf) return -1;

                        int cmp = ta.CompareTo(tb);
                        if (cmp != 0) return cmp;

                        // Tie-breakers
                        return string.Compare(a.Recruit?.GetName(), b.Recruit?.GetName(), StringComparison.Ordinal);
                    });
                    break;

            }
        }


        internal static List<RecruitAtSite> GetRecruitsForFaction(GeoFaction faction)
        {
            var list = new List<RecruitAtSite>();
            try
            {
                if (faction == null)
                {
                    return list;
                }


                GeoPhoenixFaction geoPhoenixFaction = faction.GeoLevel.PhoenixFaction; // player faction wrapper
                                                                                       // All sites with havens, owned by factionDef, revealed to player
                List<GeoHaven> havens = faction.Havens.Where(s => s != null && s.AvailableRecruit != null && s.Site.GetVisible(geoPhoenixFaction)).ToList();

                foreach (var haven in havens)
                {

                    list.Add(new RecruitAtSite
                    {
                        Recruit = haven.AvailableRecruit,
                        Site = haven.Site,
                        Haven = haven,
                        HavenOwner = haven.Site.Owner
                    });
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
            return list.OrderBy(r => r.Recruit?.GetName()).ToList();
        }

        internal static string GetClassName(GeoUnitDescriptor recruit)
        {
            if (recruit == null) return "Unknown Class";
            try
            {
                // Fallback: from tags
                var tagName = recruit.ClassTag;
                return tagName.className;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
            return "Unknown Class";
        }
        private static readonly Func<AbilityTrackSlot, int> AbilitySlotSkillPointCostGetter = CreateSkillPointCostGetter();

        internal readonly struct AbilityIconData
        {
            public AbilityIconData(AbilityTrackSlot slot)
            {
                Slot = slot;
                View = slot?.Ability?.ViewElementDef;
                Icon = View?.SmallIcon;
                SkillPointCost = GetAbilitySlotSkillPointCost(slot);
            }

            public AbilityTrackSlot Slot { get; }

            public ViewElementDef View { get; }

            public Sprite Icon { get; }

            public int SkillPointCost { get; }
        }


        internal readonly struct MutationIconData
        {
            public MutationIconData(TacticalItemDef item, ViewElementDef view)
            {
                Item = item;
                View = view;
                Icon = view?.InventoryIcon ?? view?.SmallIcon;
            }
            public TacticalItemDef Item { get; }
            public ViewElementDef View { get; }

            public Sprite Icon { get; }
            public bool HasItem => Item != null;
        }

        internal static IEnumerable<AbilityIconData> GetSelectedAbilityIcons(GeoUnitDescriptor recruit)
        {
            if (recruit == null)
            {
                yield break;
            }


            var track = recruit.GetPersonalAbilityTrack();
            var abilities = track?.AbilitiesByLevel?.ToList();
            if (abilities == null || abilities.Count == 0)
            {
                yield break;
            }

            int[] desiredIndexes = { 0, 3, 4 };
            foreach (int index in desiredIndexes)
            {
                if (index < 0 || index >= abilities.Count)
                {
                    continue;
                }

                var slot = abilities[index];
                if (slot == null)
                {
                    continue;
                }

                var data = new AbilityIconData(slot);
                if (data.Icon != null)
                {
                    yield return data;
                }
            }

        }

        internal static IEnumerable<AbilityIconData> GetClassAbilityIcons(GeoUnitDescriptor recruit)
        {
            if (recruit?.Progression?.MainSpecDef?.AbilityTrack?.AbilitiesByLevel == null)
            {
                yield break;
            }

            foreach (var slot in recruit.Progression.MainSpecDef.AbilityTrack.AbilitiesByLevel)
            {
                if (slot == null)
                {
                    continue;
                }

                var data = new AbilityIconData(slot);
                if (data.Icon != null)
                {
                    yield return data;
                }
            }
        }

        internal static IEnumerable<AbilityIconData> GetPersonalAbilityIcons(GeoUnitDescriptor recruit)
        {
            if (recruit == null)
            {
                yield break;
            }

            var track = recruit.GetPersonalAbilityTrack();
            var slots = track?.AbilitiesByLevel;
            if (slots == null)
            {
                yield break;
            }

            foreach (var slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                var data = new AbilityIconData(slot);
                if (data.Icon != null)
                {
                    yield return data;
                }
            }
        }

        private static Func<AbilityTrackSlot, int> CreateSkillPointCostGetter()
        {
            try
            {
                var getter = AccessTools.PropertyGetter(typeof(AbilityTrackSlot), "SkillPointCost");
                if (getter != null)
                {
                    return AccessTools.MethodDelegate<Func<AbilityTrackSlot, int>>(getter);
                }

                var field = AccessTools.Field(typeof(AbilityTrackSlot), "SkillPointCost");
                if (field != null)
                {
                    return slot => slot != null ? (int)field.GetValue(slot) : 0;
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return null;
        }

        private static int GetAbilitySlotSkillPointCost(AbilityTrackSlot slot)
        {
            if (slot == null)
            {
                return 0;
            }

            if (AbilitySlotSkillPointCostGetter != null)
            {
                try
                {
                    return AbilitySlotSkillPointCostGetter(slot);
                }
                catch (Exception ex)
                {

                    TFTVLogger.Error(ex);
                }
            }

            return 0;
        }
        internal static IEnumerable<MutationIconData> GetMutationIcons(GeoUnitDescriptor recruit)
        {
            if (recruit?.ArmorItems == null)
            {
                yield break;
            }


            var mutationTag = Shared?.SharedGameTags?.AnuMutationTag;
            if (mutationTag == null)
            {
                yield break;
            }

            foreach (var def in recruit.ArmorItems.Where(i => i != null))
            {

                if (def.Tags == null || !def.Tags.Contains(mutationTag))
                {
                    continue;
                }

                if (!(def is TacticalItemDef tactical))
                {
                    continue;
                }

                var ve = tactical.ViewElementDef;
                if (ve == null)
                {
                    continue;
                }

                var data = new MutationIconData(tactical, ve);
                if (data.Icon != null)
                {
                    yield return data;

                }
            }

        }

        internal static Dictionary<ResourceType, int> GetRecruitCost(GeoHaven haven, GeoPhoenixFaction phoenix)
        {
            try
            {

                var costs = new Dictionary<ResourceType, int>();
                ResourcePack cost = haven.GetRecruitCost(phoenix);

                foreach (var type in _resourceDisplayOrder)
                {
                    var unit = cost.ByResourceType(type);
                    AddCost(type, unit.Value);
                }

                foreach (var unit in cost)
                {


                    var normalizedType = NormalizeResourceType(unit.Type);
                    if (costs.ContainsKey(normalizedType))
                    {
                        continue;
                    }

                    AddCost(normalizedType, unit.Value);

                }

                return costs;

                void AddCost(ResourceType type, float value)
                {
                    int amount = Mathf.RoundToInt(value);
                    if (amount <= 0)
                    {
                        return;
                    }

                    costs[NormalizeResourceType(type)] = amount;
                }
            }
            catch (Exception ex) { TFTVLogger.Error(ex); throw; }

        }

        private static ResourceType NormalizeResourceType(ResourceType type)
        {
            if (string.Equals(type.ToString(), "Food", StringComparison.OrdinalIgnoreCase))
            {
                return ResourceType.Supplies;
            }

            return type;
        }

        internal static Sprite GetClassIcon(GeoUnitDescriptor recruit)
        {
            try
            {
                // Preferred: class def view icon
                var ve = recruit?.GetClassViewElementDefs()?.FirstOrDefault();
                if (ve != null)
                {
                    if (ve.SmallIcon != null) return ve.SmallIcon;

                }
            }
            catch { }

            // Fallback: sometimes the ClassTag has a ViewElementDef; try reflection
            try
            {
                var tag = recruit?.ClassTag;
                if (tag != null)
                {
                    var vedProp = tag.GetType().GetProperty("ViewElementDef", BindingFlags.Public | BindingFlags.Instance);
                    var ved = vedProp?.GetValue(tag) as ViewElementDef;
                    if (ved != null)
                    {
                        if (ved.SmallIcon != null) return ved.SmallIcon;
                        if (ved.InventoryIcon != null) return ved.InventoryIcon;
                    }
                }
            }
            catch { }

            return null; // no icon available; header will just show Level + Name
        }

    }
}
