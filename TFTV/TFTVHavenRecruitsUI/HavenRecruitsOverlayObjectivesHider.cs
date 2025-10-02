using PhoenixPoint.Geoscape.View;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal class HavenRecruitsOverlayObjectivesHider
    {
        private sealed class ObjectivesVisibilityState
        {
            public bool HeaderActive { get; }

            public bool ContainerActive { get; }

            public bool PrimaryContainerActive { get; }

            public bool SecondaryContainerActive { get; }

            public bool SeparatorActive { get; }

            public bool DiplomacyIconActive { get; }

            public ObjectivesVisibilityState(UIModuleGeoObjectives module)
            {
                HeaderActive = GetActive(module.ObjectivesHeader);
                ContainerActive = GetActive(module.ObjectivesContainer);
                PrimaryContainerActive = GetActive(module.PrimaryObjectivesContainer);
                SecondaryContainerActive = GetActive(module.SecondaryObjectivesContainer);
                SeparatorActive = GetActive(module.Separator);
                DiplomacyIconActive = GetActive(module.DiplomacyMissionsIconContainer);
            }
        }

        private static readonly ConditionalWeakTable<UIModuleGeoObjectives, ObjectivesVisibilityState> _visibilityStates = new ConditionalWeakTable<UIModuleGeoObjectives, ObjectivesVisibilityState>();

        /// <summary>
        /// Sets the visibility of the objectives module when the recruits overlay is toggled.
        /// </summary>
        /// <param name="view">The geoscape view owning the objectives module.</param>
        /// <param name="hidden">Whether the objectives UI should be hidden.</param>
        public static void SetObjectivesHiddenForRecruitsOverlay(GeoscapeView view, bool hidden)
        {
            if (view == null)
            {
                return;
            }

            SetObjectivesHiddenForRecruitsOverlay(view.GeoscapeModules?.ObjectivesModule, hidden);
        }

        /// <summary>
        /// Sets the visibility of the objectives module when the recruits overlay is toggled.
        /// </summary>
        /// <param name="objectivesModule">The objectives module to toggle.</param>
        /// <param name="hidden">Whether the objectives UI should be hidden.</param>
        public static void SetObjectivesHiddenForRecruitsOverlay(UIModuleGeoObjectives objectivesModule, bool hidden)
        {
            if (objectivesModule == null)
            {
                return;
            }

            if (hidden)
            {
                _ = _visibilityStates.GetValue(objectivesModule, m => new ObjectivesVisibilityState(m));

                SetActive(objectivesModule.ObjectivesHeader, false);
                SetActive(objectivesModule.ObjectivesContainer, false);
                SetActive(objectivesModule.PrimaryObjectivesContainer, false);
                SetActive(objectivesModule.SecondaryObjectivesContainer, false);
                SetActive(objectivesModule.Separator, false);
                SetActive(objectivesModule.DiplomacyMissionsIconContainer, false);
            }
            else
            {
                if (_visibilityStates.TryGetValue(objectivesModule, out ObjectivesVisibilityState state))
                {
                    SetActive(objectivesModule.ObjectivesHeader, state.HeaderActive);
                    SetActive(objectivesModule.ObjectivesContainer, state.ContainerActive);
                    SetActive(objectivesModule.PrimaryObjectivesContainer, state.PrimaryContainerActive);
                    SetActive(objectivesModule.SecondaryObjectivesContainer, state.SecondaryContainerActive);
                    SetActive(objectivesModule.Separator, state.SeparatorActive);
                    SetActive(objectivesModule.DiplomacyMissionsIconContainer, state.DiplomacyIconActive);
                    _visibilityStates.Remove(objectivesModule);
                }
                else
                {
                    SetActive(objectivesModule.ObjectivesHeader, true);
                    SetActive(objectivesModule.ObjectivesContainer, true);
                    SetActive(objectivesModule.PrimaryObjectivesContainer, true);
                    SetActive(objectivesModule.SecondaryObjectivesContainer, true);
                    SetActive(objectivesModule.Separator, true);
                    SetActive(objectivesModule.DiplomacyMissionsIconContainer, true);
                }
            }

            objectivesModule.NavHolder?.RefreshInteractableList();
        }

        private static bool GetActive(GameObject go)
        {
            return go != null && go.activeSelf;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
