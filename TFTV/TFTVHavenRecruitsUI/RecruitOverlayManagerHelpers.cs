using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Common.UI;
using PhoenixPoint.Common.View.ViewControllers.Inventory;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.View.ViewControllers.Inventory;
using System;
using System.Reflection;
using TFTV.TFTVHavenRecruitsUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TFTV.HavenRecruitsMain;
using static TFTV.HavenRecruitsMain.RecruitOverlayManager;
using Object = UnityEngine.Object;

namespace TFTV
{


    internal static class RecruitOverlayManagerHelpers
    {
        private const float InventorySlotIconOverlayScale = 1.5f;
        private const string MutationOverlayName = "MutationIconOverlay";
        private const string InventoryOverlayName = "InventoryIconOverlay";
        internal static void ClearTransformChildren(Transform transform)
        {
            if (transform == null)
            {
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        internal static Image MakeFixedIcon(Transform parent, Sprite sp, int px, Sprite backgroundSprite = null)
        {
            // Frame with RectTransform + LayoutElement fixes size for layout
            var (frame, frt) = NewUI(backgroundSprite != null ? "IconFrameAbility" : "IconFrame", parent);
            var frameImage = frame.AddComponent<Image>();
            frameImage.sprite = null;
            frameImage.color = new Color(1f, 1f, 1f, 0f);
            frameImage.raycastTarget = true;
            frameImage.type = Image.Type.Simple;

            var le = frame.AddComponent<LayoutElement>();
            le.preferredWidth = px; le.minWidth = px;
            le.preferredHeight = px; le.minHeight = px;
            frt.sizeDelta = new Vector2(px, px);

            // Padding used to make the background slightly larger and the image slightly smaller than the frame.
            int pad = Mathf.Max(2, Mathf.RoundToInt(px * 0.12f)); // ~12% of px, minimum 2px
            float inset = pad * 0.5f; // foreground inset (half of the background oversize)

            if (backgroundSprite != null)
            {
                var (bgGO, bgRT) = NewUI("Background", frame.transform);
                var bgImage = bgGO.AddComponent<Image>();
                bgImage.sprite = backgroundSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.raycastTarget = false;

                // Make the background slightly larger than the frame by expanding its offsets beyond the parent rect.
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = new Vector2(-pad, -pad); // extend left/bottom
                bgRT.offsetMax = new Vector2(pad, pad);   // extend right/top
            }

            // Child image inset a little so the background reads as larger
            var (imgGO, imgRT) = NewUI("Img", frame.transform);
            var img = imgGO.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;

            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = new Vector2(inset, inset);    // inward from left/bottom
            imgRT.offsetMax = new Vector2(-inset, -inset);  // inward from right/top

            var arf = imgGO.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (sp && sp.rect.height > 0f)
                arf.aspectRatio = sp.rect.width / sp.rect.height;

            return img;
        }


        internal static UIInventorySlot MakeMutationSlot(Transform parent, HavenRecruitsUtils.MutationIconData data, int size)
        {
            try
            {
                if (parent == null || !data.HasItem)
                {
                    return null;
                }

                UIInventorySlot template = EnsureMutationSlotTemplate(parent);
                if (template == null)
                {
                    return null;
                }

                GameObject slotGO = Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                slotGO.name = $"MutationSlot_{data.Item?.name ?? "Unknown"}";
                slotGO.SetActive(true);

                UIInventorySlot slot = slotGO.GetComponent<UIInventorySlot>();
                if (slot == null)
                {
                    Object.Destroy(slotGO);
                    return null;
                }
                PrepareSlotForDisplay(slot, size);

                GeoItem item = new GeoItem(data.Item);

                slot.Item = item;

                ApplyOversizedIconOverlay(slot, data.Icon, size, MutationOverlayName);

                var tooltip = EnsureOverlayItemTooltip();
                if (tooltip != null)
                {
                    ResetSlotHandlers(slot);

                  
                    GeoItem geoItem = slot.Item as GeoItem;
                   // TFTVLogger.Always($"geoItem: {geoItem?.ItemDef?.name}");

                    var forwarder = slotGO.GetComponent<TacticalItemSlotTooltipForwarder>() ?? slotGO.AddComponent<TacticalItemSlotTooltipForwarder>();
                    

                    forwarder.Initialize(slot, geoItem, tooltip);
                }
                return slot;

            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

        internal static UIInventorySlot MakeInventorySlot(Transform parent, ItemDef item, int size, string namePrefix)
        {
            try
            {
                if (parent == null || item == null)
                {
                    return null;
                }

                UIInventorySlot template = EnsureMutationSlotTemplate(parent);
                if (template == null)
                {
                    return null;
                }

                ViewElementDef view = item.ViewElementDef;
                if (view == null)
                {
                    return null;
                }

                Sprite icon = view.InventoryIcon ?? view.SmallIcon;
                if (icon == null)
                {
                    return null;
                }

                GameObject slotGO = Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                slotGO.name = $"{namePrefix}_{item.name}";
                slotGO.SetActive(true);

                UIInventorySlot slot = slotGO.GetComponent<UIInventorySlot>();
                if (slot == null)
                {
                    Object.Destroy(slotGO);
                    return null;
                }

                PrepareSlotForDisplay(slot, size);

                GeoItem geoItem = new GeoItem(item);
                slot.Item = geoItem;

                slot.AmmoInfoRoot.gameObject.SetActive(false);
               // slot.EmptyAmmoImageNode.gameObject.SetActive(false);
              //  slot.EmptyAmmoScaleNode.gameObject.SetActive(false);
                slot.AmmoImageNode.gameObject.SetActive(false);          
            
                ApplyOversizedIconOverlay(slot, icon, size, InventoryOverlayName);

                var tooltip = EnsureOverlayItemTooltip();
                if (tooltip != null)
                {
                    ResetSlotHandlers(slot);

                    var forwarder = slotGO.GetComponent<TacticalItemSlotTooltipForwarder>() ?? slotGO.AddComponent<TacticalItemSlotTooltipForwarder>();
                    forwarder.Initialize(slot, geoItem, tooltip);
                }

                return slot;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return null;
            }
        }

     
        private static void ApplyOversizedIconOverlay(UIInventorySlot slot, Sprite icon, int baseSize, string overlayName)
        {
            if (slot == null || icon == null)
            {
                return;
            }

            try
            {
                var existing = slot.transform.Find(overlayName);
                if (existing != null)
                {
                    Object.Destroy(existing.gameObject);
                }

                var (overlayGO, overlayRT) = NewUI(overlayName, slot.transform);
                overlayRT.anchorMin = new Vector2(0.5f, 0.5f);
                overlayRT.anchorMax = new Vector2(0.5f, 0.5f);
                overlayRT.pivot = new Vector2(0.5f, 0.5f);
                overlayRT.anchoredPosition = Vector2.zero;
                overlayRT.offsetMin = Vector2.zero;
                overlayRT.offsetMax = Vector2.zero;
                float overlaySize = baseSize * InventorySlotIconOverlayScale;
                overlayRT.sizeDelta = new Vector2(overlaySize, overlaySize);
                overlayRT.SetAsLastSibling();

                var overlayImage = overlayGO.AddComponent<Image>();
                overlayImage.sprite = icon;
                overlayImage.raycastTarget = false;

                var aspectFitter = overlayGO.AddComponent<AspectRatioFitter>();
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                if (icon.rect.height > 0f)
                {
                    aspectFitter.aspectRatio = icon.rect.width / icon.rect.height;
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private class TacticalItemSlotTooltipForwarder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private UIInventorySlot _slot;
            private UIGeoItemTooltip _tooltip;
            private GeoItem _item;
          

            internal void Initialize(UIInventorySlot slot, GeoItem geoItem, UIGeoItemTooltip tooltip)
            {
                _slot = slot;
                _item = geoItem;
                _tooltip = tooltip;  
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                try
                {
                  //  TFTVLogger.Always($"_tooltip == null? {_tooltip == null} is _slot null? {_slot == null} item {_item?.ItemDef?.name}");
                    if (_item != null && _tooltip != null)
                    {
                        //TFTVLogger.Always($"got here for item {_item.ItemDef.name}");

                        _tooltip.ShowStats(_item, _slot.transform);
                        PositionTooltip(eventData);
                    }

                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                HideTooltip();
            }

            private void OnDisable()
            {
                HideTooltip();
            }

            private void HideTooltip()
            {
                try
                {
                    if (_tooltip != null)
                    {
                        _tooltip.HideStats();
                    }
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }

            private void PositionTooltip(PointerEventData eventData)
            {
                try
                {
                    if (_tooltip == null)
                    {
                        return;
                    }

                    var tooltipRect = _tooltip.transform as RectTransform;

                    tooltipRect.anchoredPosition = new Vector2(-550, -600);

                
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                }
            }
        }
        

        private static void PrepareSlotForDisplay(UIInventorySlot slot, int size)
        {
            if (slot == null)
            {
                return;
            }
            try
            {
                slot.Item = null;

                var rect = slot.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(size, size);
                    rect.localScale = Vector3.one;
                }

                slot.transform.localScale = Vector3.one;

                var layout = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = size;
                layout.preferredHeight = size;
                layout.minWidth = size;
                layout.minHeight = size;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;

                foreach (var selectable in slot.GetComponentsInChildren<Selectable>(true))
                {
                    if (selectable != null)
                    {
                        selectable.interactable = false;
                    }
                }

                foreach (var behaviour in slot.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour == slot)
                    {
                        continue;
                    }

                    if (behaviour is IBeginDragHandler || behaviour is IDragHandler || behaviour is IEndDragHandler || behaviour is IDropHandler)
                    {
                        behaviour.enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        private static void ResetSlotHandlers(UIInventorySlot slot)
        {
            if (slot == null) return;

            try
            {
                var enterHandlers = slot.OnPointerEnteredHandlers;
                if (enterHandlers != null)
                {
                    MethodInfo clear = enterHandlers.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                    clear?.Invoke(enterHandlers, null);
                }
                var exitHandlers = slot.OnPointerExitedHandlers;
                if (exitHandlers != null)
                {
                    MethodInfo clear = exitHandlers.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                    clear?.Invoke(exitHandlers, null);
                }
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
            }
        }

        internal static (GameObject go, RectTransform rt) NewUI(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (go, (RectTransform)go.transform);
        }

        internal static Sprite GetFactionIcon(FactionFilter filter)
        {
            if (_factionIconCache.TryGetValue(filter, out var cached) && cached != null)
            {
                return cached;
            }

            string fileName = null;
            switch (filter)
            {
                case FactionFilter.Anu:
                    fileName = "FactionIcons_Anu.png";
                    break;
                case FactionFilter.NewJericho:
                    fileName = "FactionIcons_NewJericho.png";
                    break;
                case FactionFilter.Synedrion:
                    fileName = "FactionIcons_Synedrion.png";
                    break;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var sprite = Helper.CreateSpriteFromImageFile(fileName);
            if (sprite != null)
            {
                _factionIconCache[filter] = sprite;
            }

            return sprite;
        }
    }
}