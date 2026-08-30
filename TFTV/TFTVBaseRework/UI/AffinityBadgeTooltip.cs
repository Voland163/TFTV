using Base.UI;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using TFTV.TFTVIncidents;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Hover tooltip for the Affinity icon on a personnel row: names the Affinity and its rank, then
    /// lists every benefit it grants, base duty included. The tooltip itself is the shared one every
    /// icon on this screen uses; only the content is built here.
    /// </summary>
    internal sealed class AffinityBadgeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal LeaderSelection.AffinityApproach Approach;
        internal int Rank;

        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                PersonnelTooltip.Show(BuildContent(), transform);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PersonnelTooltip.Hide();
        }

        private void OnDisable()
        {
            PersonnelTooltip.Hide();
        }

        private string BuildContent()
        {
            string header = null;

            PassiveModifierAbilityDef ability = LeaderSelection.GetAffinityAbility(Approach, Rank);
            if (ability?.ViewElementDef?.DisplayName1 != null)
            {
                header = ability.ViewElementDef.DisplayName1.Localize();
            }

            if (string.IsNullOrEmpty(header))
            {
                header = $"{LeaderSelection.GetApproachDisplayName(Approach)} {Rank}";
            }

            string benefitsKey = LeaderSelection.GetAllBenefitsLocalizationKey(Approach);
            string benefits = string.IsNullOrEmpty(benefitsKey)
                ? string.Empty
                : new LocalizedTextBind() { LocalizationKey = benefitsKey }.Localize();

            return string.IsNullOrEmpty(benefits) ? header : $"{header}\n\n{benefits}";
        }
    }
}
