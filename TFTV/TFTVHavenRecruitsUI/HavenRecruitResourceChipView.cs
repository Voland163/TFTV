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
    internal class HavenRecruitResourceChipView
    {
        internal sealed class ResourceChipView : MonoBehaviour
        {
            [SerializeField] private GameObject _iconRoot;
            [SerializeField] private Image _iconImage;
            [SerializeField] private Text _typeLabel;
            [SerializeField] private Text _amountLabel;
            [SerializeField] private Color _defaultIconColor = Color.white;
            [SerializeField] private Color _defaultTypeColor = Color.white;
            [SerializeField] private Color _defaultAmountColor = Color.white;

            internal Text AmountLabel => _amountLabel;

            private void Awake()
            {
                CacheReferences();
            }

            internal void Initialize(Image iconImage, Text typeLabel, Text amountLabel, GameObject iconRoot)
            {
                _iconRoot = iconRoot;
                _iconImage = iconImage;
                _typeLabel = typeLabel;
                _amountLabel = amountLabel;
                CacheReferences();
            }

            private void CacheReferences()
            {
                if (_iconRoot == null && _iconImage != null)
                {
                    _iconRoot = _iconImage.transform.parent != null
                        ? _iconImage.transform.parent.gameObject
                        : _iconImage.gameObject;
                }

                if (_iconImage == null)
                {
                    var iconTransform = transform.Find("Icon");
                    if (iconTransform != null)
                    {
                        _iconRoot = iconTransform.gameObject;
                        _iconImage = iconTransform.GetComponentInChildren<Image>(true);
                        if (_iconImage == null)
                        {
                            _iconImage = iconTransform.GetComponent<Image>();
                        }
                    }
                }

                if (_typeLabel == null)
                {
                    var typeTransform = transform.Find("TypeLabel");
                    if (typeTransform != null)
                    {
                        _typeLabel = typeTransform.GetComponent<Text>();
                    }
                }

                if (_amountLabel == null)
                {
                    var amountTransform = transform.Find("Amount");
                    if (amountTransform != null)
                    {
                        _amountLabel = amountTransform.GetComponent<Text>();
                    }
                }

                if (_iconImage != null)
                {
                    _defaultIconColor = _iconImage.color;
                }

                if (_typeLabel != null)
                {
                    _defaultTypeColor = _typeLabel.color;
                }

                if (_amountLabel != null)
                {
                    _defaultAmountColor = _amountLabel.color;
                }
            }

            internal void Prepare(HavenRecruitsPrice.ResourceVisual visual, string fallbackLabel, int amount)
            {
                CacheReferences();

                if (_iconImage != null)
                {
                    bool hasVisual = visual != null && visual.Icon != null;
                    _iconImage.sprite = hasVisual ? visual.Icon : null;
                    _iconImage.color = hasVisual ? visual.Color : _defaultIconColor;
                    _iconImage.enabled = hasVisual;
                    if (_iconRoot != null)
                    {
                        _iconRoot.SetActive(hasVisual);
                    }
                    else
                    {
                        _iconImage.gameObject.SetActive(hasVisual);
                    }
                }

                if (_typeLabel != null)
                {
                    _typeLabel.text = (visual == null || visual.Icon == null) ? fallbackLabel : string.Empty;
                    _typeLabel.color = _defaultTypeColor;
                    _typeLabel.gameObject.SetActive(!string.IsNullOrEmpty(_typeLabel.text));
                }

                if (_amountLabel != null)
                {
                    _amountLabel.text = amount.ToString();
                    _amountLabel.color = _defaultAmountColor;
                }

                name = string.IsNullOrEmpty(fallbackLabel) ? "Res" : $"Res_{fallbackLabel}";
            }

            internal void Release()
            {
                CacheReferences();

                if (_iconImage != null)
                {
                    _iconImage.sprite = null;
                    _iconImage.color = _defaultIconColor;
                    _iconImage.enabled = false;
                    if (_iconRoot != null)
                    {
                        _iconRoot.SetActive(false);
                    }
                    else
                    {
                        _iconImage.gameObject.SetActive(false);
                    }
                }

                if (_typeLabel != null)
                {
                    _typeLabel.text = string.Empty;
                    _typeLabel.color = _defaultTypeColor;
                    _typeLabel.gameObject.SetActive(false);
                }

                if (_amountLabel != null)
                {
                    _amountLabel.text = string.Empty;
                    _amountLabel.color = _defaultAmountColor;
                }
            }

        }
    }
}
