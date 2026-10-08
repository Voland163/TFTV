using Base.Core;
using Base.UI;
using Base.UI.MessageBox;
using Base.UI.MessageBox.PromptControllers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TFTV
{
    /// <summary>
    /// The deployment phase's opening explanation, with a "Don't show again" checkbox that hides it for the
    /// rest of the playthrough.
    ///
    /// The game's message box has no checkbox, so one is added to the prompt once it is open (ShowSimplePrompt
    /// shows it synchronously) and removed again when the prompt closes, since the prompt window is shared
    /// with every other message. It goes in the button row, so it inherits the row's layout and the prompt's
    /// free cursor clicks it with a pad too.
    ///
    /// PromptHidden belongs to the campaign: it is saved with both the geoscape and the tactical data. It
    /// only ever goes from false to true, so each restore keeps it if either side says hidden; a box ticked
    /// during a mission survives the geoscape restoring its pre-mission snapshot. A new game and an explicit
    /// load reset it before the save is read.
    /// </summary>
    internal static partial class TFTVPlayerDeploymentPhase
    {
        internal static bool PromptHidden;

        private const string CheckboxName = "TFTV_DeploymentPromptDontShowAgain";

        private static bool _layoutLogged;

        private static readonly Func<MessageBoxPromptController, Button> GetActiveConfirmButton =
            AccessTools.MethodDelegate<Func<MessageBoxPromptController, Button>>(AccessTools.Method(typeof(MessageBoxPromptController), "GetActiveConfirmButton"));

        internal static void ShowDeploymentPrompt()
        {
            if (PromptHidden)
            {
                return;
            }

            MessageBox messageBox = GameUtl.GetMessageBox();
            // Without a reserve this mission, the explanation of the reserve is left out.
            string text = TFTVCommonMethods.ConvertKeyToString(_pending != null && _pending.ReserveEnabled
                ? "TFTV_DEPLOYMENT_PHASE_PROMPT"
                : "TFTV_DEPLOYMENT_PHASE_PROMPT_NO_RESERVE");

            DontShowAgainToggle toggle = null;

            messageBox.ShowSimplePrompt(text, MessageBoxIcon.Information, MessageBoxButtons.OK, result =>
            {
                try
                {
                    if (toggle != null)
                    {
                        if (toggle.IsOn)
                        {
                            PromptHidden = true;
                            TFTVLogger.Always("[DeploymentPhase] Opening prompt hidden for the rest of this playthrough.");
                        }

                        toggle.ReleaseNavigation();

                        // Destroy is deferred to the end of the frame; detach first so nothing that lists the
                        // prompt's children this frame still finds the checkbox.
                        toggle.transform.SetParent(null, false);
                        UnityEngine.Object.Destroy(toggle.gameObject);
                    }
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            });

            try
            {
                toggle = AddDontShowAgainToggle(messageBox, text);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static DontShowAgainToggle AddDontShowAgainToggle(MessageBox messageBox, string text)
        {
            MessageBoxPromptController prompt = messageBox.GetComponentsInChildren<MessageBoxPromptController>(true)
                .FirstOrDefault(p => p.gameObject.activeInHierarchy && p.TextContent != null && p.TextContent.Text == text);

            Button okButton = prompt != null ? GetActiveConfirmButton(prompt) : null;

            if (okButton == null)
            {
                TFTVLogger.Always("[DeploymentPhase] Opening prompt not found; no 'Don't show again' checkbox this time.");
                return null;
            }

            // A leftover from a prompt that closed some other way.
            Transform stale = prompt.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CheckboxName);
            if (stale != null)
            {
                stale.GetComponent<DontShowAgainToggle>()?.ReleaseNavigation();
                stale.SetParent(null, false);
                UnityEngine.Object.Destroy(stale.gameObject);
            }

            Transform row = okButton.transform.parent;
            RectTransform okRect = (RectTransform)okButton.transform;
            Text okLabel = okButton.GetComponentInChildren<Text>(true);

            if (!_layoutLogged)
            {
                _layoutLogged = true;
                LogPromptLayout(prompt, okButton);
            }

            GameObject checkbox = new GameObject(CheckboxName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            RectTransform rect = (RectTransform)checkbox.transform;
            rect.SetParent(row, false);
            rect.SetSiblingIndex(okButton.transform.GetSiblingIndex());

            float height = Mathf.Max(okRect.rect.height, 40f);
            int fontSize = okLabel != null ? okLabel.fontSize : 28;
            float width = 60f + fontSize * 9f;

            Image hitArea = checkbox.GetComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.01f);

            HorizontalLayoutGroup layout = checkbox.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement element = checkbox.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;

            // Without a layout group on the button row, sit just left of the OK button.
            if (row.GetComponent<LayoutGroup>() == null)
            {
                rect.anchorMin = okRect.anchorMin;
                rect.anchorMax = okRect.anchorMax;
                rect.pivot = new Vector2(1f, okRect.pivot.y);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = okRect.anchoredPosition + new Vector2(-okRect.rect.width * okRect.pivot.x - 24f, 0f);
            }

            float boxSize = Mathf.Round(height * 0.55f);
            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            box.transform.SetParent(rect, false);
            box.GetComponent<Image>().color = okLabel != null ? okLabel.color : Color.white;
            LayoutElement boxElement = box.GetComponent<LayoutElement>();
            boxElement.preferredWidth = boxSize;
            boxElement.preferredHeight = boxSize;

            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(box.transform, false);
            RectTransform innerRect = (RectTransform)inner.transform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            inner.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);

            GameObject tick = new GameObject("Tick", typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(inner.transform, false);
            RectTransform tickRect = (RectTransform)tick.transform;
            tickRect.anchorMin = Vector2.zero;
            tickRect.anchorMax = Vector2.one;
            tickRect.offsetMin = new Vector2(3f, 3f);
            tickRect.offsetMax = new Vector2(-3f, -3f);
            tick.GetComponent<Image>().color = okLabel != null ? okLabel.color : Color.white;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            labelObject.transform.SetParent(rect, false);
            Text label = labelObject.GetComponent<Text>();
            label.text = TFTVCommonMethods.ConvertKeyToString("TFTV_DEPLOYMENT_PHASE_DONT_SHOW_AGAIN");
            label.font = okLabel != null ? okLabel.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.color = okLabel != null ? okLabel.color : Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            labelObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            DontShowAgainToggle toggle = checkbox.AddComponent<DontShowAgainToggle>();
            toggle.Tick = tick;
            toggle.SetOn(false);

            // The whole row toggles on a mouse click. The pad selects the box itself: navigation puts the
            // cursor on the selected element, and the row is stretched by the button row's layout, so its
            // centre is past the end of the label.
            checkbox.GetComponent<Button>().onClick.AddListener(() => toggle.SetOn(!toggle.IsOn));

            Button boxButton = box.AddComponent<Button>();
            boxButton.targetGraphic = box.GetComponent<Image>();
            boxButton.transition = Selectable.Transition.None;
            boxButton.onClick.AddListener(() => toggle.SetOn(!toggle.IsOn));

            toggle.JoinNavigation(prompt.NavHolder, boxButton, okButton);

            return toggle;
        }

        /// <summary>
        /// The prompt's real hierarchy around the button row, once, so the checkbox can be fitted to it.
        /// </summary>
        private static void LogPromptLayout(MessageBoxPromptController prompt, Button okButton)
        {
            Transform row = okButton.transform.parent;
            string rowLayout = row.GetComponent<LayoutGroup>()?.GetType().Name ?? "none";
            RectTransform okRect = (RectTransform)okButton.transform;
            string siblings = string.Join(", ", Enumerable.Range(0, row.childCount).Select(i => row.GetChild(i)).Select(c => $"{c.name}{(c.gameObject.activeSelf ? string.Empty : " (inactive)")}"));

            TFTVLogger.Always($"[DeploymentPhase] Prompt layout: prompt '{prompt.name}', OK button '{okButton.name}' {okRect.rect.size} in row '{row.name}' (layout {rowLayout}); row children: {siblings}");
        }

        private sealed class DontShowAgainToggle : MonoBehaviour
        {
            public GameObject Tick;
            public bool IsOn { get; private set; }

            // The prompt's holder as the game set it up, handed back when the prompt closes: every other
            // message box reuses it.
            private UINavigationalElementsHolder _holder;
            private List<GameObject> _originalContainers;
            private List<Selectable> _originalFixedElements;

            /// <summary>
            /// The pad only reaches what the prompt's navigation holder lists, and the holder built its list
            /// when the prompt opened, before the checkbox existed. Publish its list again with the checkbox
            /// just before OK, matching the screen; OnShowReady already put the cursor on OK.
            /// </summary>
            public void JoinNavigation(UINavigationalElementsHolder holder, Selectable checkbox, Selectable okButton)
            {
                try
                {
                    if (holder == null || checkbox == null)
                    {
                        TFTVLogger.Always("[DeploymentPhase] Prompt has no navigation holder; the checkbox is mouse-only.");
                        return;
                    }

                    List<Selectable> elements = holder.InteractiveList != null
                        ? holder.InteractiveList.Where(e => e != null).ToList()
                        : new List<Selectable>();

                    TFTVLogger.Always($"[DeploymentPhase] Prompt navigation: mode {holder.NavigationMode}, " +
                        $"containers {holder.InteractableContainers?.Count ?? 0}, elements [{string.Join(", ", elements.Select(e => e.name))}]");

                    if (elements.Contains(checkbox))
                    {
                        return;
                    }

                    int okIndex = elements.IndexOf(okButton);
                    elements.Insert(okIndex >= 0 ? okIndex : 0, checkbox);

                    _holder = holder;
                    _originalContainers = holder.InteractableContainers;
                    _originalFixedElements = holder.FixedInteractableElements != null
                        ? new List<Selectable>(holder.FixedInteractableElements)
                        : null;

                    // A holder fed by containers rebuilds its list from them and ignores a fixed list.
                    holder.InteractableContainers = null;
                    holder.SetFixedInteractableElements(elements);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public void ReleaseNavigation()
            {
                try
                {
                    if (_holder == null)
                    {
                        return;
                    }

                    _holder.InteractableContainers = _originalContainers;
                    _holder.FixedInteractableElements = _originalFixedElements;
                    _holder = null;
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }

            public void SetOn(bool on)
            {
                IsOn = on;

                if (Tick != null)
                {
                    Tick.SetActive(on);
                }
            }
        }
    }
}
