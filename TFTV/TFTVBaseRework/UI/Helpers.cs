using Base.Core;
using HarmonyLib;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static TFTV.TFTVBaseRework.PersonnelData;

namespace TFTV.TFTVBaseRework
{
    public static partial class PersonnelManagementUI
    {
        #region UI Helpers
        private static string GetPersonnelName(PersonnelInfo person)
        {
            return person?.Character?.DisplayName
                   ?? $"Personnel {person?.Id}";
        }

        /// <summary>Interior of the modal's framed panel; header, options and buttons hang off this.</summary>
        private static Transform _modalPanelContent;

        private const float ModalRowHeight = 64f;

        private static void AddSimpleButton(Transform parent, string caption, Action onClick)
        {
            CreateTextButton(parent, $"Btn_{caption}", caption, onClick,
                width: 260f, height: ModalRowHeight, fontSize: BodyFontSize);
        }

        /// <summary>
        /// One selectable row in a modal. Captions carrying their own line breaks - the training
        /// levels list, for one - get the extra height they need.
        /// </summary>
        private static void AddModalOptionButton(Transform parent, string caption, Action onClick)
        {
            int extraLines = caption?.Split('\n').Length - 1 ?? 0;
            float height = ModalRowHeight + extraLines * 34f;

            CreateTextButton(parent, $"Option_{caption}", caption, onClick,
                height: height, fontSize: BodyFontSize);
        }

        private static void AddDisabledLabel(Transform parent, string caption)
        {
            CreateTextButton(parent, $"Disabled_{caption}", caption, null,
                height: ModalRowHeight, fontSize: BodyFontSize, enabled: false);
        }

        private static void CloseModal()
        {
            if (_modalRoot != null)
            {
                UnityEngine.Object.Destroy(_modalRoot);
                _modalRoot = null;
                _modalPanelContent = null;
            }
        }

        /// <summary>
        /// A dimmed backdrop over the whole screen with one framed panel centred on it, built from
        /// the same widgets as the panels behind it so the two read as one screen.
        /// </summary>
        private static GameObject CreateModalRoot(string name)
        {
            if (_personnelPanel == null) return null;

            var modal = new GameObject(name, typeof(RectTransform));
            modal.transform.SetParent(_personnelPanel.transform, false);

            var canvas = modal.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            modal.AddComponent<GraphicRaycaster>();
            modal.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // The personnel panel arranges its children in a row. Without this the dialog is treated
            // as a fourth column and lands off the right edge of the screen, which is exactly where
            // it went. Ignoring the layout lets the anchors below cover the panel instead.
            modal.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch(modal.GetComponent<RectTransform>());

            GameObject frame = CreateFramedPanel(modal.transform, "ModalFrame", out Transform content,
                borderThickness: 3f, padding: 14, spacing: 8f);

            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.24f, 0.06f);
            frameRect.anchorMax = new Vector2(0.76f, 0.94f);
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;

            _modalPanelContent = content;
            return modal;
        }

        private static void AddModalHeader(string title)
        {
            if (_modalPanelContent == null) return;

            Text header = CreateLabel(_modalPanelContent, "Header", title, 44, AccentOrangeColor, TextAnchor.MiddleCenter);
            SetSize(header.gameObject, 0f, 72f);

            GameObject rule = CreateUIObject("Rule", _modalPanelContent);
            rule.AddComponent<Image>().color = PanelBorderColor;
            SetSize(rule, 0f, 2f);
        }

        /// <summary>
        /// Options go in a scroll list: the deploy picker can list the whole roster, which used to
        /// run off the bottom of the dialog.
        /// </summary>
        private static Transform CreateModalContentArea()
        {
            CreateScrollList(_modalPanelContent, "ModalOptions", out Transform content, spacing: 6f, padding: 6);
            return content;
        }

        private static void AddModalCloseButton()
        {
            if (_modalPanelContent == null) return;
            CreateTextButton(_modalPanelContent, "Close", "CLOSE", () => CloseModal(),
                height: ModalRowHeight, fontSize: BodyFontSize);
        }

        /// <summary>
        /// Wrapped body text for the message and confirmation dialogs. Its height comes from the text
        /// itself, so a five-line dismissal warning is as readable as a one-line notice.
        /// </summary>
        private static void AddModalMessage(Transform parent, string message)
        {
            Text label = CreateLabel(parent, "Message", message, BodyFontSize, TextPrimaryColor,
                TextAnchor.UpperCenter, wrap: true);
            LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
            element.flexibleHeight = 0f;
        }
        #endregion

        #region Deploy or Train Selection
        private static void ShowDeployOrTrainSelection(GeoLevelController level, PersonnelInfo person, GeoPhoenixFaction phoenix, Action refresh)
        {
            if (level == null || person == null || phoenix == null) return;

            CloseModal();
            _modalRoot = CreateModalRoot("DeployOrTrainModal");
            AddModalHeader($"Choose Action for {GetPersonnelName(person)}");
            var content = CreateModalContentArea();

            var specs = ResolveAvailableMainSpecs(level);
            bool isDismissed = PersonnelRestrictions.IsDismissedOperative(person.Character);

            // Option 1: Deploy Now (immediate class selection + base selection)
            AddModalOptionButton(content, "Deploy Now", () =>
            {
                ShowDeploymentSelection(level, person, phoenix, specs, refresh);
            });

            // Option 2: Train First (only if training slots are available)
            int providedSlots = TrainingFacilityRework.GetProvidedTrainingSlots(phoenix);
            int usedSlots = TrainingFacilityRework.GetUsedTrainingSlots();
            if (usedSlots < providedSlots)
            {
                int maxLevel = TrainingFacilityRework.GetMaxTargetLevel(phoenix, person.Character);
                float minDuration = TrainingFacilityRework.GetEffectiveDurationHours(phoenix, 2);
                float maxDuration = TrainingFacilityRework.GetEffectiveDurationHours(phoenix, maxLevel);
                int freeSlots = providedSlots - usedSlots;
                string durationLabel = minDuration == maxDuration
     ? FormatDuration(minDuration)
     : $"{FormatDuration(minDuration)}–{FormatDuration(maxDuration)}";
                AddModalOptionButton(content, $"Train First ({durationLabel}, {freeSlots} slot{(freeSlots != 1 ? "s" : "")} free)", () =>
                {
                    if (isDismissed)
                    {
                        // Dismissed operatives keep their existing class — skip class selection.
                        SpecializationDef existingSpec = ResolveExistingSpecialization(person.Character);
                        if (existingSpec != null)
                        {
                            ShowTrainingLevelSelection(level, person, existingSpec, refresh);
                        }
                        else
                        {
                            ShowMessage($"Could not determine class for {person.Character?.DisplayName}.");
                        }
                    }
                    else
                    {
                        ShowTrainingSelection(level, person, specs, refresh);
                    }
                });
            }
            else
            {
                AddDisabledLabel(content, "Train First (no training slots available)");
            }

            AddModalCloseButton();
        }

        private static SpecializationDef ResolveExistingSpecialization(GeoCharacter character)
        {
            try
            {
                if (character?.ClassTag == null) return null;

                // Match the character's current class tag against available specializations.
                var allSpecs = TrainingFacilityRework.GetAvailableTrainingSpecializations(
                    GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>()?.PhoenixFaction);

                foreach (var spec in allSpecs)
                {
                    if (spec?.ClassTag != null && character.ClassTag == spec.ClassTag)
                    {
                        return spec;
                    }
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
            return null;
        }

        #endregion

        #region Deployment / Training Selection
        private static void ShowDeploymentSelection(GeoLevelController level, PersonnelInfo person, GeoPhoenixFaction faction, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || faction == null || person == null) return;

            bool isTraining = person.Assignment == PersonnelAssignment.Training;
            bool trainingComplete = isTraining && TrainingFacilityRework.IsRecruitTrainingComplete(person.Character, level);
            bool isDismissedOperative = PersonnelRestrictions.IsDismissedOperative(person.Character);

            CloseModal();
            _modalRoot = CreateModalRoot("DeploymentSelectionModal");
            AddModalHeader("Select Deployment Base");
            var content = CreateModalContentArea();

            foreach (var baseObj in faction.Bases)
            {
                GeoPhoenixBase geoBase = baseObj.GetComponent<GeoPhoenixBase>();
                string label = baseObj.Site?.Name ?? baseObj.name;
                AddModalOptionButton(content, label, () =>
                {
                    if (isTraining)
                    {
                        var session = TrainingFacilityRework.GetRecruitSession(person.Character);
                        bool wasDismissed = session?.WasDismissed ?? false;

                        Action finalize = () =>
                        {
                            var character = TrainingFacilityRework.FinalizeRecruitTraining(level, person.Character, geoBase, early: !trainingComplete);
                            if (character != null)
                            {
                                PersonnelData.RemovePersonnel(faction, person);
                                RefreshResourceInfo(faction);
                            }
                            refresh();
                            CloseModal();
                        };

                        if (!trainingComplete)
                        {
                            int currentLevel = session?.VirtualLevelAchieved ?? 1;
                            int refund = session != null ? Math.Max(0, session.SpPaid - (10 * Math.Max(0, currentLevel - 1))) : 0;
                            string earlyMsg = $"Training incomplete for {person.Character?.DisplayName}.\nCurrent level: {currentLevel}\nSP refund: {refund}";
                            if (wasDismissed)
                            {
                                int redeployCost = PersonnelRestrictions.GetRedeployCost(person.Character);
                                earlyMsg += $"\n\nRedeploy cost: {redeployCost} SP";
                            }
                            earlyMsg += "\n\nDeploy early?";
                            ShowConfirmation(earlyMsg, finalize, () => CloseModal());
                        }
                        else
                        {
                            finalize();
                        }
                    }
                    else if (isDismissedOperative)
                    {
                        int cost = PersonnelRestrictions.GetRedeployCost(person.Character);
                        if (faction.Skillpoints < cost)
                        {
                            ShowMessage($"Not enough shared skill points to redeploy {person.Character?.DisplayName}.\nRequired: {cost}\nAvailable: {faction.Skillpoints}");
                            return;
                        }

                        ShowConfirmation(
                            $"Redeploy {person.Character?.DisplayName} to {label} for {cost} shared skill points?",
                            () =>
                            {
                                var character = TrainingFacilityRework.RedeployDismissedOperative(level, person.Character, geoBase);
                                if (character != null)
                                {
                                    RemovePersonnel(faction, person);
                                    RefreshResourceInfo(faction);

                                }
                                else
                                {
                                    TFTVLogger.Always($"{LogPrefix} Redeploy failed.");
                                }

                                refresh();
                                CloseModal();
                            },
                            () => CloseModal());
                    }
                    else
                    {
                        if (person.TrainingSpec == null)
                        {
                            ShowClassSelectionForImmediateDeploy(level, person, geoBase, specs, refresh);
                        }
                        else
                        {
                            DeployNow(level, person, geoBase, person.TrainingSpec, refresh);
                        }
                    }
                });
            }
            AddModalCloseButton();
        }

        private static void ShowMessage(string message)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("MessageModal");
            AddModalHeader("Notice");
            var content = CreateModalContentArea();

            AddModalMessage(content, message);

            AddModalCloseButton();
        }

        private static void RefreshResourceInfo(GeoPhoenixFaction faction)
        {
            try
            {
                GeoLevelController level = GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
                UIModuleInfoBar infoBar = level?.View?.GeoscapeModules?.ResourcesModule;
                MethodInfo update = AccessTools.Method(typeof(UIModuleInfoBar), "UpdateResourceInfo");
                if (faction != null && infoBar != null && update != null)
                {
                    update.Invoke(infoBar, new object[] { faction, false });
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static void ShowTrainingSelection(GeoLevelController level, PersonnelInfo person, List<SpecializationDef> specs, Action refresh)
        {
            if (level == null || person == null) return;

            CloseModal();
            _modalRoot = CreateModalRoot("TrainingSelectionModal");
            AddModalHeader("Select Class");
            var content = CreateModalContentArea();

            int providedSlots = TrainingFacilityRework.GetProvidedTrainingSlots(level.PhoenixFaction);
            int usedSlots = TrainingFacilityRework.GetUsedTrainingSlots();
            bool anyFacilityAvailable = providedSlots > usedSlots;

            if (!anyFacilityAvailable)
            {
                AddModalOptionButton(content, "No training facility slot available", () => CloseModal());
            }
            else
            {
                foreach (var spec in specs)
                {
                    string specName = spec.ViewElementDef.DisplayName1.Localize();
                    AddModalOptionButton(content, specName, () =>
                    {
                        ShowTrainingLevelSelection(level, person, spec, refresh);
                    });
                }
            }
            AddModalCloseButton();
        }

        private static void ShowTrainingLevelSelection(GeoLevelController level, PersonnelInfo person, SpecializationDef spec, Action refresh)
        {
            if (level == null || person == null || spec == null) return;

            GeoPhoenixFaction faction = level.PhoenixFaction;
            int maxLevel = TrainingFacilityRework.GetMaxTargetLevel(faction, person.Character);
            bool isDismissed = PersonnelRestrictions.IsDismissedOperative(person.Character);

            // Dismissed operatives start from their current level; civilians start from level 1.
            int currentCharLevel = isDismissed
                ? (person.Character?.LevelProgression?.Level ?? 1)
                : 1;
            int minTargetLevel = Math.Max(2, currentCharLevel + 1);

            CloseModal();
            _modalRoot = CreateModalRoot("TrainingLevelSelectionModal");
            AddModalHeader("Select Training Level");
            var content = CreateModalContentArea();

            if (minTargetLevel > maxLevel)
            {
                ShowMessage($"{person.Character?.DisplayName} is already at level {currentCharLevel} and cannot train further.");
                return;
            }

            for (int targetLevel = minTargetLevel; targetLevel <= maxLevel; targetLevel++)
            {
                int levelsGained = targetLevel - currentCharLevel;
                int spCost = TrainingFacilityRework.GetTrainingSpCost(targetLevel);
                float duration = TrainingFacilityRework.GetEffectiveDurationHours(faction, targetLevel, currentCharLevel);
                string statGains = TrainingFacilityRework.GetStatGainDescription(levelsGained);
                string label = $"Level {targetLevel} — {spCost} SP — {FormatDuration(duration)}\n{statGains}";

                bool canAfford = faction.Skillpoints >= spCost;
                int capturedLevel = targetLevel;

                AddModalOptionButton(content, canAfford ? label : $"{label}\n(Not enough SP)", () =>
                {
                    if (!canAfford)
                    {
                        ShowMessage($"Not enough shared skill points.\nRequired: {spCost}\nAvailable: {faction.Skillpoints}");
                        return;
                    }

                    string confirmMsg = $"Train {person.Character?.DisplayName} as {spec.ViewElementDef.DisplayName1.Localize()} to Level {capturedLevel}?\nCost: {spCost} SP ({FormatDuration(duration)})\n{statGains}";
                    if (isDismissed)
                    {
                        confirmMsg += "\n\nThis operative is dismissed. Completing training will clear the dismissed status.";
                    }

                    ShowConfirmation(confirmMsg, () =>
                    {
                        if (TrainingFacilityRework.QueueCharacterTrainingAutoFacility(level, person.Character, spec, capturedLevel))
                        {
                            AssignPersonnelToTraining(person, faction, spec);
                        }
                        else
                        {
                            TFTVLogger.Always($"{LogPrefix} Failed to queue training.");
                        }
                        refresh();
                        CloseModal();
                    }, () => CloseModal());
                });
            }
            AddModalCloseButton();
        }

        private static void ShowConfirmation(string message, Action onConfirm, Action onCancel)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("ConfirmationModal");
            AddModalHeader("Confirm");
            var content = CreateModalContentArea();

            AddModalMessage(content, message);

            // The answer buttons sit under the scrolling body, where they cannot be scrolled away
            // from a long warning.
            GameObject buttonsRow = CreateUIObject("ButtonsRow", _modalPanelContent);
            var layout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            SetSize(buttonsRow, 0f, ModalRowHeight + 8f);

            CreateTextButton(buttonsRow.transform, "Yes", "YES", () => onConfirm?.Invoke(),
                width: 260f, height: ModalRowHeight, fontSize: BodyFontSize,
                fillColor: AccentOrangeColor, captionColor: Color.black);

            CreateTextButton(buttonsRow.transform, "No", "NO", () => onCancel?.Invoke(),
                width: 260f, height: ModalRowHeight, fontSize: BodyFontSize);
        }

        private static void ShowClassSelectionForImmediateDeploy(GeoLevelController level, PersonnelInfo person, GeoPhoenixBase baseObj, List<SpecializationDef> specs, Action refresh)
        {
            CloseModal();
            _modalRoot = CreateModalRoot("ClassSelectionForDeploy");
            AddModalHeader("Select Class");
            var content = CreateModalContentArea();

            foreach (var spec in specs)
            {
                string label = spec.ViewElementDef.DisplayName1.Localize();
                AddModalOptionButton(content, label, () =>
                {
                    DeployNow(level, person, baseObj, spec, refresh);
                });
            }

            AddModalCloseButton();
        }

        private static void DeployNow(GeoLevelController level, PersonnelInfo person, GeoPhoenixBase baseObj, SpecializationDef spec, Action refresh)
        {
            var character = TrainingFacilityRework.PromoteCivilianToOperative(level, person.Character, baseObj, spec);
            if (character != null)
            {
                person.TrainingSpec = spec;
                PersonnelData.RemovePersonnel(level.PhoenixFaction, person);
            }
            else
            {
                TFTVLogger.Always($"{LogPrefix} Immediate deploy failed.");
            }
            refresh();
            CloseModal();
        }

        private static string FormatDuration(double hours)
        {
            // Match the vanilla TimeUnit-based countdown used by the Agenda tracker
            // (UIFactionDataTrackerElement.UpdateData), which rounds remaining time UP
            // so it never visually shows less time than actually remains.
            int totalHours = (int)Math.Ceiling(hours);
            int d = totalHours / 24;
            int h = totalHours % 24;
            if (d > 0 && h > 0) return $"{d}d {h}h";
            if (d > 0) return $"{d}d";
            return $"{h}h";
        }

        #endregion
    }
}