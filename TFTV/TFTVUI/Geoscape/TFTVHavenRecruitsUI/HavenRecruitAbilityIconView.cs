using Base.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVHavenRecruitsUI
{
    internal sealed class AbilityIconView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _defaultIconColor = Color.white;
        [SerializeField] private Color _defaultBackgroundColor = Color.white;
        [SerializeField] private HavenRecruitAbilityTooltipTrigger _tooltip;
        [SerializeField] private AspectRatioFitter _aspectFitter;

        private void Awake()
        {
            CacheReferences();
        }

        internal void Initialize(Image iconImage, Image backgroundImage, HavenRecruitAbilityTooltipTrigger tooltip)
        {
            _iconImage = iconImage;
            _backgroundImage = backgroundImage;
            _tooltip = tooltip;
            CacheReferences();
        }

        private void CacheReferences()
        {
            if (_iconImage == null)
            {
                var iconTransform = transform.Find("Img") ?? transform;
                _iconImage = iconTransform.GetComponent<Image>();
            }

            if (_backgroundImage == null)
            {
                var backgroundTransform = transform.Find("Background");
                if (backgroundTransform != null)
                {
                    _backgroundImage = backgroundTransform.GetComponent<Image>();
                }
            }

            if (_tooltip == null && _iconImage != null)
            {
                _tooltip = _iconImage.GetComponent<HavenRecruitAbilityTooltipTrigger>();
            }

            if (_aspectFitter == null && _iconImage != null)
            {
                _aspectFitter = _iconImage.GetComponent<AspectRatioFitter>();
            }

            if (_iconImage != null)
            {
                _defaultIconColor = _iconImage.color;
            }

            if (_backgroundImage != null)
            {
                _defaultBackgroundColor = _backgroundImage.color;
            }
        }

        internal void Prepare(HavenRecruitsUtils.AbilityIconData abilityData, Sprite backgroundSprite)
        {
            CacheReferences();

            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = backgroundSprite;
                _backgroundImage.color = _defaultBackgroundColor;
                bool hasBackground = backgroundSprite != null;
                _backgroundImage.enabled = hasBackground;
                _backgroundImage.gameObject.SetActive(hasBackground);
            }

            if (_iconImage != null)
            {
                bool hasIcon = abilityData.Icon != null;
                _iconImage.enabled = hasIcon;
                _iconImage.gameObject.SetActive(hasIcon);
                _iconImage.sprite = abilityData.Icon;
                _iconImage.color = _defaultIconColor;
                _iconImage.raycastTarget = true;

                if (_aspectFitter != null && abilityData.Icon != null && abilityData.Icon.rect.height > 0f)
                {
                    _aspectFitter.aspectRatio = abilityData.Icon.rect.width / abilityData.Icon.rect.height;
                }
            }

            if (_tooltip != null)
            {
                _tooltip.Initialize(abilityData);
            }

            name = abilityData.Icon != null ? $"Ability_{abilityData.Icon.name}" : "Ability_Empty";
        }

        internal void Release()
        {
            CacheReferences();

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.color = _defaultIconColor;
                _iconImage.enabled = false;
                _iconImage.gameObject.SetActive(false);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = null;
                _backgroundImage.color = _defaultBackgroundColor;
                _backgroundImage.enabled = false;
                _backgroundImage.gameObject.SetActive(false);
            }

            if (_tooltip != null)
            {
                _tooltip.Initialize(default);
            }
        }
    }
}
