using HarmonyLib;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.PhoenixBases.FacilityComponents;
using PhoenixPoint.Geoscape.Entities.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Living Space framework: Living Quarters provide capacity; other facilities consume it.
    /// Shortage => efficiency multiplier (Provided / Required) applied to selected outputs (to be implemented).
    /// </summary>
    internal static class FacilityLivingSpace
    {
        internal class LivingSpaceState
        {
            public int Provided;
            public int Required;
            public float EfficiencyMultiplier;
        }

        private static readonly Dictionary<int, LivingSpaceState> LivingSpaceCache = new Dictionary<int, LivingSpaceState>();

        private static readonly Dictionary<string, int> FacilityLivingSpaceRequirements = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Populate with real facility Def names:
            // { "PX_ResearchLab_Def", 6 },
            // { "PX_ManufacturingPlant_Def", 8 },
            // { "PX_TrainingFacility_Def", 4 },
            // { "PX_MedBay_Def", 5 },
            // { "PX_MutationLab_Def", 7 },
        };

        private static int GetLivingSpaceProvidedBy(GeoPhoenixFacility facility)
        {
            var container = facility.GetComponent<ContainerFacilityComponent>();
            return container == null ? 0 : container.SoldiersCapacity;
        }

        private static int GetLivingSpaceRequiredBy(GeoPhoenixFacility facility)
        {
            if (facility?.Def == null) return 0;
            return FacilityLivingSpaceRequirements.TryGetValue(facility.Def.name, out int required) ? required : 0;
        }

        private static bool IsLivingQuarters(GeoPhoenixFacility facility)
        {
            if (facility == null) return false;
            var container = facility.GetComponent<ContainerFacilityComponent>();
            return container != null && container.SoldiersCapacity > 0;
        }

        private static LivingSpaceState Recalculate(GeoPhoenixBaseLayout layout)
        {
            LivingSpaceState state = new LivingSpaceState();
            if (layout == null)
            {
                state.Provided = 0;
                state.Required = 0;
                state.EfficiencyMultiplier = 1f;
                return state;
            }

            var facilities = layout.Facilities.Where(f => f != null && f.IsWorking).ToList();

            state.Provided = facilities.Where(IsLivingQuarters)
                                       .Sum(GetLivingSpaceProvidedBy);

            state.Required = facilities.Where(f => !IsLivingQuarters(f))
                                       .Sum(GetLivingSpaceRequiredBy);

            state.EfficiencyMultiplier = state.Required <= 0
                ? 1f
                : Math.Max(0f, Math.Min(1f, (float)state.Provided / state.Required));

            return state;
        }

        private static void ApplyEfficiencyPenalty(PhoenixBaseStats baseStats, LivingSpaceState state)
        {
            if (state == null || baseStats == null) return;

            float multiplier = state.EfficiencyMultiplier;
            if (multiplier >= 1f) return;

            // TODO: Implement concrete scaling of outputs here.
        }

        internal static LivingSpaceState GetStateForBase(GeoSite site)
        {
            if (site == null) return null;
            LivingSpaceCache.TryGetValue(site.SiteId, out LivingSpaceState state);
            return state;
        }

        private static GeoPhoenixBase TryGetBaseFromFacilities(GeoPhoenixBaseLayout layout)
        {
            if (layout == null) return null;

            foreach (GeoPhoenixFacility fac in layout.Facilities)
            {
                if (fac == null) continue;

                return fac.PxBase;
               
            }
            return null;
        }

        [HarmonyPatch(typeof(PhoenixBaseStats), "Update")]
        internal static class PhoenixBaseStats_Update_LivingSpace
        {
            private static void Postfix(PhoenixBaseStats __instance, GeoPhoenixBaseLayout ____layout)
            {
                try
                {
                    LivingSpaceState state = Recalculate(____layout);

                    // Obtain GeoPhoenixBase using any facility since layout itself does not expose base.
                    GeoPhoenixBase geoBase = TryGetBaseFromFacilities(____layout);
                    GeoSite site = geoBase?.Site;

                    if (site != null)
                    {
                        LivingSpaceCache[site.SiteId] = state;
                    }

                    ApplyEfficiencyPenalty(__instance, state);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /*
        [HarmonyPatch(typeof(UIModuleInfoBar), "UpdateSoldierData")]
        internal static class UIModuleInfoBar_UpdateSoldierData_LivingSpace
        {
            [HarmonyPostfix]
            private static void Postfix(UIModuleInfoBar __instance)
            {
                try
                {
                    var contextField = AccessTools.Field(typeof(UIModuleInfoBar), "_context");
                    var context = (PhoenixPoint.Geoscape.View.GeoscapeViewContext)contextField.GetValue(__instance);
                    var site = context?.Base?.Site;
                    var state = GetStateForBase(site);
                    if (state != null && __instance.SoldiersLabel != null)
                    {
                        // __instance.SoldiersLabel.text += $"  LS:{state.Provided}/{state.Required} ({state.EfficiencyMultiplier:P0})";
                    }
                }
                catch { }
            }
        }
        */
    }
}