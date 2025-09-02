using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewModules;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV.TFTVDrills
{
    internal class DrillsUI
    {
        private const int SwapSpCost = 0;

        [HarmonyPatch(typeof(AbilityTrackSkillEntryElement), "OnPointerClick")]
        public static class AbilityTrackSkillEntryElement_OnPointerClick_Patch
        {
            public static bool Prefix(AbilityTrackSkillEntryElement __instance)
            {
                try
                {

                    TFTVLogger.Always($"AbilityTrackSkillEntryElement OnPointerClick invoked");
                    // Find the owning progression UI
                    var ui = __instance.GetComponentInParent<UIModuleCharacterProgression>();
                    if (ui == null) return true;

                    // character + current ability
                    var character = GetPrivate<GeoCharacter>(ui, "_character");
                    if (character?.Progression == null) return true;

                    var ability = __instance.AbilityDef;
                    if (ability == null)
                    {
                        // fallback: some builds store ability only on slot
                        var slotFromElem = FindSlotFieldOnElement(__instance);
                        ability = slotFromElem?.Ability;
                    }
                    if (ability == null) return true;

                    // Only intercept if the ability is already learned
                    if (!character.Progression.Abilities.Contains(ability)) return true;

                    // Build a slot if we can (nice-to-have, not strictly required)
                    var (_, slot) = FindTrackSlotForAbility(character, ability);

                    // Build/ensure your pool (_drills) if needed
                    if (DrillsAbilities.Drills == null || DrillsAbilities.Drills.Count == 0)
                    {
                        // no-op if you populate elsewhere; or build by tag here
                        // _drills = SharedData.GetSharedDataFromGame().DefRepository.GetAllDefs<TacticalAbilityDef>()...
                    }

                    // We don't care about source here; your ShowReplacementPopup ignores it.
                    AbilityTrackSource dummySource = AbilityTrackSource.Personal;

                    // Reuse your existing popup
                    ShowReplacementPopup(ui, slot, ability, dummySource, DrillsAbilities.Drills);

                    // Swallow vanilla click
                    return false;
                }
                catch (Exception ex)
                {
                    TFTVLogger.Error(ex);
                    throw; // fallback to vanilla on error
                }
            }

            static (AbilityTrack track, AbilityTrackSlot slot) FindTrackSlotForAbility(GeoCharacter c, TacticalAbilityDef def)
            {
                foreach (var tr in c.Progression.AbilityTracks)
                    foreach (var s in tr.AbilitiesByLevel)
                        if (s?.Ability == def) return (tr, s);
                return (null, null);
            }

            static AbilityTrackSlot FindSlotFieldOnElement(AbilityTrackSkillEntryElement elem)
            {
                var f = elem.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .FirstOrDefault(fi => typeof(AbilityTrackSlot).IsAssignableFrom(fi.FieldType));
                return f != null ? (AbilityTrackSlot)f.GetValue(elem) : null;
            }

            static T GetPrivate<T>(object obj, string field)
            {
                var f = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                return f != null ? (T)f.GetValue(obj) : default;
            }

            private static void ShowReplacementPopup(
UIModuleCharacterProgression ui,
AbilityTrackSlot slot,
TacticalAbilityDef original,
AbilityTrackSource source,
List<TacticalAbilityDef> choices)
            {
                // If the built-in DualClass popup is present, use it
                var popupGO = GetPrivate<GameObject>(ui, "DualClassPopupWindow");
                var container = GetPrivate<GameObject>(ui, "DualClassButtonsContainer");
                var prefab = GetPrivate<GameObject>(ui, "DualClassButtonsPrefab");

                if (popupGO != null && container != null && prefab != null)
                {
                    foreach (Transform child in container.transform)
                        UnityEngine.Object.Destroy(child.gameObject);

                    AddHeader(container.transform, original);

                    foreach (var def in choices)
                    {
                        var go = UnityEngine.Object.Instantiate(prefab, container.transform);
                        WireButton(go, def, () =>
                        {
                            TryPerformSwap(ui, slot, original, def, source);
                            popupGO.SetActive(false);
                        });
                    }

                    AddCancel(container.transform, () => popupGO.SetActive(false));
                    popupGO.SetActive(true);
                    return;
                }

                // ---- Fallback: build a lightweight modal on the fly ----
                BuildSimplePopup(ui, slot, original, source, choices);
            }


            private static void BuildSimplePopup(
                UIModuleCharacterProgression ui,
                AbilityTrackSlot slot,
                TacticalAbilityDef original,
                AbilityTrackSource source,
                List<TacticalAbilityDef> choices)
            {
                // ---- overlay ----
                var overlay = new GameObject("TFTV_SwapOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                var ort = (RectTransform)overlay.transform;
                ort.SetParent(ui.transform, false);
                ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
                ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;

                var obg = overlay.GetComponent<Image>(); obg.color = new Color(0, 0, 0, 0.55f);
                var oclk = overlay.GetComponent<Button>(); oclk.transition = Selectable.Transition.None;
                oclk.onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));

                // ---- panel ----
                float sw = Screen.width, sh = Screen.height;
                float pw = Mathf.Clamp(sw * 0.60f, 800f, 1400f);
                float ph = Mathf.Clamp(sh * 0.70f, 560f, 900f);

                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button));
                var prt = (RectTransform)panel.transform; prt.SetParent(overlay.transform, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = new Vector2(pw, ph);
                panel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.10f, 0.96f);
                panel.GetComponent<Button>().onClick.AddListener(() => { /* swallow */ });

                // ---- title ----
                var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                var trt = (RectTransform)title.transform; trt.SetParent(panel.transform, false);
                trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
                trt.pivot = new Vector2(0.5f, 1); trt.sizeDelta = new Vector2(0, 42); trt.anchoredPosition = new Vector2(0, -10);
                var ttxt = title.GetComponent<Text>(); ttxt.alignment = TextAnchor.MiddleCenter; ttxt.fontSize = 22;
                ttxt.text = $"Replace: {(original.ViewElementDef?.DisplayName1?.Localize() ?? original.name)}";

                // ---- scroll view (Viewport + Content) ----
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                var vrt = (RectTransform)viewport.transform; vrt.SetParent(panel.transform, false);
                vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
                vrt.offsetMin = new Vector2(16, 64); vrt.offsetMax = new Vector2(-16, -64);
                viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);

                var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
                var crt = (RectTransform)content.transform; crt.SetParent(viewport.transform, false);
                // IMPORTANT: top-anchored content that stretches horizontally
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(0, 0); // height will be set below

                var grid = content.GetComponent<GridLayoutGroup>();
                const int ICON = 128;                                  // big icons
                var cell = new Vector2(ICON + 56, ICON + 40);          // room for label
                grid.cellSize = cell;
                grid.spacing = new Vector2(12, 12);
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

                // compute column count from available width
                float innerW = pw - 32f;                               // subtract viewport padding
                int cols = Mathf.Max(1, Mathf.FloorToInt((innerW + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
                grid.constraintCount = cols;

                // populate cards
                foreach (var def in choices)
                {
                    var card = CreateChoiceCard(def, ICON, () =>
                    {
                        TryPerformSwap(ui, slot, original, def, source);
                        UnityEngine.Object.Destroy(overlay);
                    });
                    card.transform.SetParent(content.transform, false);
                }

                // set content height so vertical scroll works
                int rows = Mathf.CeilToInt((float)choices.Count / cols);
                float h = rows * grid.cellSize.y + (rows - 1) * grid.spacing.y + 16f; // + top padding
                crt.sizeDelta = new Vector2(0, h);

                // wire ScrollRect
                var scroll = panel.AddComponent<ScrollRect>();
                scroll.viewport = vrt;
                scroll.content = crt;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 30f;

                // ---- cancel button ----
                var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                var crt2 = (RectTransform)cancel.transform; crt2.SetParent(panel.transform, false);
                crt2.anchorMin = new Vector2(0.5f, 0); crt2.anchorMax = new Vector2(0.5f, 0);
                crt2.pivot = new Vector2(0.5f, 0); crt2.anchoredPosition = new Vector2(0, 12); crt2.sizeDelta = new Vector2(160, 36);
                cancel.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
                var cLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var clrt = (RectTransform)cLabel.transform; clrt.SetParent(cancel.transform, false);
                clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one; clrt.offsetMin = Vector2.zero; clrt.offsetMax = Vector2.zero;
                var ctxt = cLabel.GetComponent<Text>(); ctxt.alignment = TextAnchor.MiddleCenter; ctxt.text = "Cancel";
                cancel.GetComponent<Button>().onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));

                overlay.transform.SetAsLastSibling();
            }

            private static GameObject CreateChoiceCard(TacticalAbilityDef def, int iconSize, System.Action onChoose)
            {
                var card = new GameObject(def?.name ?? "Ability", typeof(RectTransform), typeof(Image), typeof(Button));
                var rt = (RectTransform)card.transform;
                rt.sizeDelta = new Vector2(iconSize + 56, iconSize + 40);

                var bg = card.GetComponent<Image>(); bg.color = new Color(1, 1, 1, 0.08f);
                var btn = card.GetComponent<Button>(); btn.onClick.AddListener(() => onChoose?.Invoke());

                // icon
                var ico = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var irt = (RectTransform)ico.transform; irt.SetParent(card.transform, false);
                irt.anchorMin = new Vector2(0.5f, 1); irt.anchorMax = new Vector2(0.5f, 1);
                irt.pivot = new Vector2(0.5f, 1); irt.anchoredPosition = new Vector2(0, -8);
                irt.sizeDelta = new Vector2(iconSize, iconSize);

                var iconImg = ico.GetComponent<Image>();
                iconImg.sprite = def?.ViewElementDef?.LargeIcon ?? def?.ViewElementDef?.SmallIcon;
                iconImg.preserveAspect = true;

                // label
                var lab = new GameObject("Label", typeof(RectTransform), typeof(Text));
                var lrt = (RectTransform)lab.transform; lrt.SetParent(card.transform, false);
                lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
                lrt.pivot = new Vector2(0.5f, 0); lrt.anchoredPosition = new Vector2(0, 6);
                lrt.sizeDelta = new Vector2(0, 22);

                var txt = lab.GetComponent<Text>();
                txt.alignment = TextAnchor.MiddleCenter;
                txt.resizeTextForBestFit = true; txt.resizeTextMinSize = 12; txt.resizeTextMaxSize = 18;
                txt.text = def?.ViewElementDef?.DisplayName1?.Localize() ?? def?.name ?? "Ability";

                return card;
            }





            private static void AddHeader(Transform parent, TacticalAbilityDef original)
            {
                var header = new GameObject("TFTV_SwapHeader", typeof(RectTransform), typeof(Text));
                header.transform.SetParent(parent, false);
                var text = header.GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 18;
                text.raycastTarget = false;
                string name = original.ViewElementDef?.DisplayName1?.Localize() ?? original.name;
                text.text = $"Replace: {name}";
            }

            private static void AddCancel(Transform parent, System.Action onClick)
            {
                var go = new GameObject("TFTV_Cancel", typeof(RectTransform), typeof(Button), typeof(Image));
                go.transform.SetParent(parent, false);
                var img = go.GetComponent<Image>(); // give it some bg so it's clickable
                var btn = go.GetComponent<Button>();

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var label = labelGo.GetComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.text = "Cancel";

                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            private static void WireButton(GameObject buttonGO, TacticalAbilityDef def, System.Action onClick)
            {
                // Try to find a PhoenixGeneralButton if prefab uses it, otherwise fall back to Unity Button
                var pgb = buttonGO.GetComponentInChildren<PhoenixGeneralButton>();
                if (pgb != null)
                {
                    var text = pgb.GetComponentInChildren<Text>();
                    if (text != null)
                        text.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name;

                    // Optional: set icon if the prefab has an Image under it
                    var img = pgb.GetComponentInChildren<Image>();
                    if (img != null && def.ViewElementDef?.LargeIcon != null)
                        img.sprite = def.ViewElementDef.LargeIcon;

                    pgb.BaseButton.onClick.AddListener(() => onClick?.Invoke());
                    return;
                }

                // Fallback
                var btn = buttonGO.GetComponentInChildren<Button>() ?? buttonGO.AddComponent<Button>();
                var label = buttonGO.GetComponentInChildren<Text>();
                if (label == null)
                {
                    var lg = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    lg.transform.SetParent(buttonGO.transform, false);
                    label = lg.GetComponent<Text>();
                }
                label.text = def.ViewElementDef?.DisplayName1?.Localize() ?? def.name;
                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            private static void TryPerformSwap(
                UIModuleCharacterProgression ui,
                AbilityTrackSlot slot,
                TacticalAbilityDef original,
                TacticalAbilityDef replacement,
                AbilityTrackSource source)
            {
                try
                {
                    var character = GetPrivate<GeoCharacter>(ui, "_character");
                    var phoenixFaction = GetPrivate<GeoPhoenixFaction>(ui, "_phoenixFaction");

                    if (character == null || character.Progression == null)
                        return;

                    // (Optional) cost handling
                    if (SwapSpCost > 0)
                    {
                        // Pay with soldier first then faction, like base stat purchase does.
                        var currSP = GetPrivate<int>(ui, "_currentSkillPoints");
                        var currFP = GetPrivate<int>(ui, "_currentFactionPoints");
                        int remaining = SwapSpCost;

                        if (currSP >= remaining)
                        {
                            SetPrivate(ui, "_currentSkillPoints", currSP - remaining);
                        }
                        else
                        {
                            remaining -= currSP;
                            SetPrivate(ui, "_currentSkillPoints", 0);
                            if (currFP >= remaining)
                                SetPrivate(ui, "_currentFactionPoints", currFP - remaining);
                            else
                            {
                                Debug.LogWarning("[TFTV] Not enough SP/FS for swap; aborting.");
                                return;
                            }
                        }
                    }



                    List<TacticalAbilityDef> abilities = Traverse.Create(character.Progression).Field("_abilities").GetValue<List<TacticalAbilityDef>>();


                    if (abilities.Contains(original))
                        abilities.Remove(original);


                    if (!abilities.Contains(replacement))
                        abilities.Add(replacement);

                    if (replacement.name.Contains("fieldpromotion"))
                    {
                        TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points before Field Promotion");
                        UIModuleCharacterProgression uIModuleCharacterProgression = GameUtl.CurrentLevel().GetComponent<GeoLevelController>().View.GeoscapeModules.CharacterProgressionModule;

                        FieldInfo fieldInfo = typeof(UIModuleCharacterProgression).GetField("_currentSkillPoints", BindingFlags.Instance | BindingFlags.NonPublic);
                        fieldInfo.SetValue(uIModuleCharacterProgression, character.Progression.SkillPoints + 30);

                        uIModuleCharacterProgression.CommitStatChanges();

                        TFTVLogger.Always($"{character?.DisplayName} has {character.Progression.SkillPoints} skill points after Field Promotion");

                    }

                    // Update the slot mapping to show the new one in the track UI
                    slot.Ability = replacement;



                    // Refresh UI
                    // Commit changes like the module does after purchases
                    CallPrivate(ui, "CommitStatChanges");
                    CallPrivate(ui, "RefreshStatPanel");
                    CallPrivate(ui, "SetAbilityTracks");
                    CallPrivate(ui, "RefreshAbilityTracks");
                }
                catch (Exception e)
                {
                    TFTVLogger.Always($"[TFTV] Ability swap failed: {e}");
                }
            }

            // ---- small reflection helpers ----


            private static void SetPrivate<T>(object obj, string field, T value)
            {
                var f = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(obj, value);
            }

            private static void CallPrivate(object obj, string method)
            {
                obj.GetType()
                   .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                   ?.Invoke(obj, null);
            }
        }
    }
}
    

