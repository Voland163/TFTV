using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using System;
using TFTV.TFTVBaseRework;

namespace TFTV.AgendaTracker
{
    internal static class AgendaRefresh
    {
        internal static void RefreshCustomSiteTracker(GeoSite site)
        {
            try
            {
                if (AgendaConstants.factionTracker == null || site == null) return;

                var existing = AgendaHelpers.FindTrackedElement(site);

                if (!AgendaHelpers.HasCustomSiteTracker(site))
                {
                    if (existing != null)
                    {
                        AgendaHelpers.RemoveTrackerElement(existing);
                        AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                    }
                    return;
                }

                GeoFaction viewer = site.GeoLevel?.ViewerFaction ?? site.Owner;

                if (existing == null)
                {
                    string text = AgendaHelpers.GetCustomSiteTrackerText(site, viewer);
                    var el = AgendaHelpers.AddTrackerElement(site, text, AgendaHelpers.GetCustomSiteViewElement(site));
                    AgendaHelpers.ApplyCustomSiteTrackerText(el, site, viewer);
                }
                else
                {
                    AgendaHelpers.ApplyCustomSiteTrackerText(existing, site, viewer);
                }

                AgendaHelpers.RefreshTracker();
                AgendaHelpers.ReapplyResolvedTrackerTexts(AgendaConstants.factionTracker, viewer);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        internal static void RefreshRecruitTrainingTracker(GeoCharacter character)
        {
            try
            {
                if (AgendaConstants.factionTracker == null || character == null) return;

                var existing = AgendaHelpers.FindTrackedElementById(character);
                var session = TrainingFacilityRework.GetRecruitSession(character);

                if (session == null || session.Completed)
                {
                    if (existing != null)
                    {
                        AgendaHelpers.RemoveTrackerElement(existing);
                        AgendaConstants.UpdateData.Invoke(AgendaConstants.factionTracker, null);
                    }
                    return;
                }

                string text = AgendaHelpers.GetRecruitTrainingTrackerText(character);

                if (existing == null)
                {
                    var el = AgendaHelpers.AddTrackerElement(character, text, AgendaHelpers.GetTrainingViewElement());
                    AgendaHelpers.ApplyRecruitTrainingTrackerText(el, character);
                }
                else
                {
                    AgendaHelpers.ApplyRecruitTrainingTrackerText(existing, character);
                }

                AgendaHelpers.RefreshTracker();
                AgendaHelpers.ReapplyResolvedTrackerTexts(AgendaConstants.factionTracker, null);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
    }
}