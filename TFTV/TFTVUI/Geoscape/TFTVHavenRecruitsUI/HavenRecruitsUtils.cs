using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.TFTVHavenRecruitsUI.HavenRecruitsPrice;


namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsUtils
    {
        //TFTV_HUMAN_ENEMIES_NJ
        //TFTV_HUMAN_ENEMIES_SYN
        //TFTV_HUMAN_ENEMIES_ANU

        internal static string AnuFactionName = "";
        internal static string NJFactionName = "";
        internal static string SynFactionName = "";

        internal static void PopulateFactionNames()
        {
            try
            {
                if (AnuFactionName == "")
                {
                    AnuFactionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_ANU")?.Trim() ?? "";
                    NJFactionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_NJ")?.Trim() ?? "";
                    SynFactionName = TFTVCommonMethods.ConvertKeyToString("TFTV_HUMAN_ENEMIES_SYN")?.Trim() ?? "";
                }

            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private const string Ellipsis = "...";

        /// <summary>
        /// Sets a label's text, cut short with an ellipsis if it would be wider than
        /// <paramref name="maxWidth"/>.
        ///
        /// Unity's Text has no truncation of its own worth using: HorizontalWrapMode.Wrap moves the
        /// overflow to a second line, Truncate cuts it mid-word with no ellipsis to say it did, and
        /// Overflow - what the recruit labels use - simply draws the rest of the name over whatever
        /// is next to it. Recruit names are three parts long (given name, nickname in quotes, family
        /// name) and routinely outrun the space they have.
        ///
        /// The measurement is the label's own font metrics, which is what Text.preferredWidth is
        /// built on, so it accounts for the font and size actually in use.
        /// </summary>
        internal static void SetEllipsizedText(Text label, string value, float maxWidth)
        {
            if (label == null)
            {
                return;
            }

            label.text = Ellipsize(label, value, maxWidth);
        }

        /// <summary>
        /// The shortened string on its own, measured with <paramref name="metrics"/>'s font.
        ///
        /// Use this rather than <see cref="SetEllipsizedText"/> whenever the label's text is built up
        /// out of parts with rich-text markup around them: cutting the finished string can land in
        /// the middle of a colour tag, whereas cutting each part before it is wrapped cannot. Markup
        /// costs no width, so measuring the bare part is also the honest measurement.
        /// </summary>
        internal static string Ellipsize(Text metrics, string value, float maxWidth)
        {
            try
            {
                value = value ?? string.Empty;

                if (metrics == null || maxWidth <= 0f || value.Length == 0 || MeasureTextWidth(metrics, value) <= maxWidth)
                {
                    return value;
                }

                // Longest prefix that still fits once the ellipsis is on it. Width grows with every
                // character kept, so the answer can be bisected instead of walked.
                int shortest = 0;
                int longest = value.Length - 1;
                string best = Ellipsis;

                while (shortest <= longest)
                {
                    int keep = (shortest + longest) / 2;
                    string candidate = value.Substring(0, keep).TrimEnd() + Ellipsis;

                    if (MeasureTextWidth(metrics, candidate) <= maxWidth)
                    {
                        best = candidate;
                        shortest = keep + 1;
                    }
                    else
                    {
                        longest = keep - 1;
                    }
                }

                return best;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return value ?? string.Empty;
            }
        }

        private static float MeasureTextWidth(Text label, string value)
        {
            TextGenerationSettings settings = label.GetGenerationSettings(Vector2.zero);

            // Measuring, not laying out: let the generator report the width the string wants rather
            // than the width the label currently has.
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = false;

            return label.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings) / label.pixelsPerUnit;
        }

        /// <summary>
        /// World-space x of a rect's left and right edges. Rects that overlap in a UI are only
        /// comparable in world space - they can sit under different parents, anchors and pivots.
        /// </summary>
        internal static float WorldLeft(RectTransform rect) => WorldEdge(rect, 0);

        internal static float WorldRight(RectTransform rect) => WorldEdge(rect, 2);

        private static float WorldEdge(RectTransform rect, int corner)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[corner].x;
        }

        /// <summary>
        /// Horizontal room a label has before it reaches <paramref name="boundaryWorldX"/>, in the
        /// label's own units - the world distance converted back through the label's scale, so the
        /// answer is right whatever the canvas is scaled to.
        ///
        /// Returns zero when the label has no scale yet, which <see cref="SetEllipsizedText"/> reads
        /// as "no limit known" and leaves the text whole.
        /// </summary>
        internal static float MeasureRoomBefore(RectTransform label, float boundaryWorldX, float gutter)
        {
            try
            {
                if (label == null)
                {
                    return 0f;
                }

                float scale = Mathf.Abs(label.lossyScale.x);
                if (scale <= 0.0001f)
                {
                    return 0f;
                }

                return (boundaryWorldX - WorldLeft(label)) / scale - gutter;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return 0f;
            }
        }

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
                List<GeoHaven> havens = faction.Havens.Where(s => s != null
                && s.AvailableRecruit != null
                && s.Site.GetInspected(geoPhoenixFaction)
                && s.Leader.CanRecruitWithFaction(geoPhoenixFaction)
                && s.Zones.Any((GeoHavenZone z) =>
                    z?.Def != null
                    && (z.Def.ProvidesRecruitment || z.Def.ProvidesEliteRecruitment)
                    && (z.IsOperational || z.State == GeoHavenZoneState.Building))
               ).ToList();

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
            if (recruit == null) return TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_HAVEN_RECRUITS_UNKNOWN_CLASS");
            try
            {
                // Fallback: from tags
                var tagName = recruit.ClassTag;
                return tagName.className;
            }
            catch (Exception ex) { TFTVLogger.Error(ex); }
            return TFTVCommonMethods.ConvertKeyToString("KEY_TFTV_HAVEN_RECRUITS_UNKNOWN_CLASS");
        }

        internal static bool IsVehicleOrMutog(GeoUnitDescriptor recruit)
        {
            try
            {
                var template = recruit?.UnitType?.TemplateDef;
                if (template == null)
                {
                    return false;
                }

                return template.IsVehicle || template.IsMutog;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return false;
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

            if (IsVehicleOrMutog(recruit))
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

        internal static IEnumerable<ItemDef> GetVehicleOrMutogWeapons(GeoUnitDescriptor recruit)
        {
            if (recruit?.ArmorItems == null)
            {
                // TFTVLogger.Always($"{recruit.Identity?.Name} recruit.Equipment is null");
                yield break;
            }

            //  TFTVLogger.Always($"{recruit.Identity?.Name} recruit.Equipment is not null");

            foreach (var item in recruit.ArmorItems)
            {
                if (!LooksLikeWeapon(item))
                {
                    continue;
                }
                //  TFTVLogger.Always($"item= {item.name}");
                yield return item;
            }
        }

        private static bool LooksLikeWeapon(ItemDef item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                if (item is WeaponDef)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }

            return false;
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
