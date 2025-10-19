using PhoenixPoint.Common.View.ViewControllers;
using PhoenixPoint.Geoscape.View.ViewControllers.Roster;
using PhoenixPoint.Geoscape.View.ViewModules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TFTV.TFTVDrills
{
    internal class DrillTooltip
    {
        public class DrillOverlayController : MonoBehaviour
        {
            // Token: 0x06004B5F RID: 19295 RVA: 0x000FFF24 File Offset: 0x000FE124
            public void Configure(GeoRosterItem slot, IEnumerable<GeoRosterAbilityDetailTooltip> tooltips)
            {
                this.RestoreSlotInteraction();
                this._sourceSlot = slot;
                this._slotButton = ((slot != null) ? slot.RowButton : null);
                if (this._slotButton != null)
                {
                    this._slotButtonWasInteractable = this._slotButton.IsInteractable();
                    this._slotButton.SetInteractable(false);
                }
                this._tooltips.Clear();
                if (tooltips == null)
                {
                    return;
                }
                foreach (GeoRosterAbilityDetailTooltip item in tooltips)
                {
                    if (item != null && !this._tooltips.Contains(item))
                    {
                        this._tooltips.Add(item);
                    }
                }
            }

            // Token: 0x06004B60 RID: 19296 RVA: 0x000FFF88 File Offset: 0x000FE188
            public void Close()
            {
                this.RestoreSlotInteraction();
                this._sourceSlot = null;
                foreach (GeoRosterAbilityDetailTooltip geoRosterAbilityDetailTooltip in this._tooltips)
                {
                    if (geoRosterAbilityDetailTooltip != null)
                    {
                        geoRosterAbilityDetailTooltip.Hide();
                    }
                }
                this._tooltips.Clear();
                Action closed = this.Closed;
                if (closed != null)
                {
                    closed();
                }
                UnityEngine.Object.Destroy(base.gameObject);
            }

            // Token: 0x1400008E RID: 142
            // (add) Token: 0x06004B61 RID: 19297 RVA: 0x000FFFE8 File Offset: 0x000FE1E8
            // (remove) Token: 0x06004B62 RID: 19298 RVA: 0x0000004D File Offset: 0x0000004D
            public event Action Closed;

            // Token: 0x040032AB RID: 12971
            private readonly List<GeoRosterAbilityDetailTooltip> _tooltips = new List<GeoRosterAbilityDetailTooltip>();

            // Token: 0x040032AC RID: 12972
            private GeoRosterItem _sourceSlot;

            // Token: 0x040032AD RID: 12973
            private PhoenixGeneralButton _slotButton;

            // Token: 0x040032AE RID: 12974
            private bool _slotButtonWasInteractable;

            private void RestoreSlotInteraction()
            {
                if (this._slotButton != null)
                {
                    if (this._slotButtonWasInteractable)
                    {
                        this._slotButton.SetInteractable(true);
                    }
                    this._slotButton = null;
                    this._slotButtonWasInteractable = false;
                }
            }
        }

        public class DrillSwapUI : MonoBehaviour
        {
            // Token: 0x06004B63 RID: 19299 RVA: 0x00010040 File Offset: 0x0000E240
            private void Awake()
            {
                if (this._progressionModule == null)
                {
                    this._progressionModule = base.GetComponentInParent<UIModuleCharacterProgression>();
                }
                if (this._overlayRoot == null)
                {
                    this._overlayRoot = base.transform;
                }
            }

            // Token: 0x06004B64 RID: 19300 RVA: 0x00010074 File Offset: 0x0000E274
            public DrillOverlayController BuildOverlay(GeoRosterItem slot)
            {
                return this.BuildOverlay(slot, null);
            }

            // Token: 0x06004B65 RID: 19301 RVA: 0x00010080 File Offset: 0x0000E280
            public DrillOverlayController BuildOverlay(GeoRosterItem slot, DrillOverlayController overlayInstance)
            {
                if (slot == null)
                {
                    throw new ArgumentNullException("slot");
                }
                DrillOverlayController drillOverlayController = overlayInstance;
                if (this._activeOverlay != null && this._activeOverlay != drillOverlayController)
                {
                    this._activeOverlay.Closed -= this.OnOverlayClosed;
                    this._activeOverlay.Close();
                    this._activeOverlay = null;
                }
                if (drillOverlayController == null)
                {
                    if (this._overlayPrefab == null)
                    {
                        throw new InvalidOperationException("Drill overlay prefab is not assigned.");
                    }
                    drillOverlayController = UnityEngine.Object.Instantiate<DrillOverlayController>(this._overlayPrefab, this._overlayRoot);
                }
                this._activeSlot = slot;
                this._hiddenTooltips.Clear();
                this.CollectTooltips(slot, this._hiddenTooltips);
                foreach (GeoRosterAbilityDetailTooltip geoRosterAbilityDetailTooltip in this._hiddenTooltips)
                {
                    if (geoRosterAbilityDetailTooltip != null)
                    {
                        geoRosterAbilityDetailTooltip.Hide();
                    }
                }
                drillOverlayController.Configure(slot, this._hiddenTooltips);
                drillOverlayController.Closed -= this.OnOverlayClosed;
                drillOverlayController.Closed += this.OnOverlayClosed;
                this._activeOverlay = drillOverlayController;
                return drillOverlayController;
            }

            // Token: 0x06004B66 RID: 19302 RVA: 0x00010124 File Offset: 0x0000E324
            private void CollectTooltips(GeoRosterItem slot, List<GeoRosterAbilityDetailTooltip> results)
            {
                if (results == null)
                {
                    return;
                }
                if (this._progressionModule != null)
                {
                    if (this._progressionModule.AbilityToolTipObject != null)
                    {
                        results.Add(this._progressionModule.AbilityToolTipObject);
                    }
                    if (this._progressionModule.MutoidAbilityToolTipObject != null)
                    {
                        results.Add(this._progressionModule.MutoidAbilityToolTipObject);
                    }
                }
                if (slot != null)
                {
                    GeoRosterAbilityDetailTooltip componentInChildren = slot.GetComponentInChildren<GeoRosterAbilityDetailTooltip>(true);
                    if (componentInChildren != null && !results.Contains(componentInChildren))
                    {
                        results.Add(componentInChildren);
                    }
                }
            }

            // Token: 0x06004B67 RID: 19303 RVA: 0x000101A0 File Offset: 0x0000E3A0
            private void OnOverlayClosed()
            {
                if (this._activeOverlay != null)
                {
                    this._activeOverlay.Closed -= this.OnOverlayClosed;
                    this._activeOverlay = null;
                }
                this._activeSlot = null;
                this._hiddenTooltips.Clear();
            }

            // Token: 0x1700114A RID: 4426
            // (get) Token: 0x06004B68 RID: 19304 RVA: 0x000101E8 File Offset: 0x0000E3E8
            // (set) Token: 0x06004B69 RID: 19305 RVA: 0x000101F0 File Offset: 0x0000E3F0
            public UIModuleCharacterProgression ProgressionModule
            {
                get
                {
                    return this._progressionModule;
                }
                set
                {
                    this._progressionModule = value;
                }
            }

            // Token: 0x040032AD RID: 12973
            [SerializeField]
            private UIModuleCharacterProgression _progressionModule;

            // Token: 0x040032AE RID: 12974
            [SerializeField]
            private DrillOverlayController _overlayPrefab;

            // Token: 0x040032AF RID: 12975
            [SerializeField]
            private Transform _overlayRoot;

            // Token: 0x040032B0 RID: 12976
            private DrillOverlayController _activeOverlay;

            // Token: 0x040032B1 RID: 12977
            private GeoRosterItem _activeSlot;

            // Token: 0x040032B2 RID: 12978
            private readonly List<GeoRosterAbilityDetailTooltip> _hiddenTooltips = new List<GeoRosterAbilityDetailTooltip>();
        }

    }
}
