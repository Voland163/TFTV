using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static TFTV.TFTVBaseRework.PersonnelData;

namespace TFTV.TFTVBaseRework
{
    #region MonoBehaviours

    /// <summary>
    /// Handles click-to-select / click-to-deselect on a personnel slot.
    /// Left-click on Training column slots opens the deploy prompt instead of selecting.
    /// Right-click opens deploy/redeploy on any slot.
    /// </summary>
    internal class PersonnelSlotSelector : MonoBehaviour, IPointerClickHandler
    {
        public int PersonnelId;
        public PersonnelAssignment Column;
        public Image BackgroundImage;

        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (Column == PersonnelAssignment.Training)
                    {
                        // Training slots: left-click opens deploy prompt directly
                        PersonnelInfo person = GetPersonnelByUnitId(PersonnelId);
                        if (person != null)
                        {
                            PersonnelManagementUI.ShowSlotContextMenu(person);
                        }
                    }
                    else
                    {
                        PersonnelManagementUI.ToggleSelection(PersonnelId, Column);
                        UpdateVisual();
                        RefreshSiblingVisuals();
                    }
                }
                else if (eventData.button == PointerEventData.InputButton.Right)
                {
                    // Right-click: open deploy/context menu for this personnel
                    PersonnelInfo person = GetPersonnelByUnitId(PersonnelId);
                    if (person != null)
                    {
                        PersonnelManagementUI.ShowSlotContextMenu(person);
                    }
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        internal void UpdateVisual()
        {
            if (BackgroundImage != null)
            {
                BackgroundImage.color = PersonnelManagementUI.IsSelected(PersonnelId)
                    ? new Color(0.25f, 0.45f, 0.70f, 0.90f)
                    : new Color(0.12f, 0.14f, 0.18f, 0.85f);
            }
        }

        private void RefreshSiblingVisuals()
        {
            Transform parent = transform.parent;
            if (parent == null) return;
            foreach (var selector in parent.GetComponentsInChildren<PersonnelSlotSelector>())
            {
                if (selector != this)
                {
                    selector.UpdateVisual();
                }
            }
        }
    }

    /// <summary>
    /// Handles drag of personnel slots between columns.
    /// Creates a ghost visual during drag, and routes to the drop zone on release.
    /// Not attached to Training column slots (personnel cannot be moved out of training).
    /// </summary>
    internal class PersonnelSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int PersonnelId;
        public PersonnelAssignment Column;
        public ScrollRect ParentScrollRect;

        private GameObject _ghost;
        private Canvas _ghostCanvas;
        private bool _isDragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            try
            {
                // If this item is not selected, clear selection and select only this one
                if (!PersonnelManagementUI.IsSelected(PersonnelId))
                {
                    PersonnelManagementUI.ClearSelection();
                    PersonnelManagementUI.ToggleSelection(PersonnelId, Column);
                    // Refresh all visuals
                    var selector = GetComponent<PersonnelSlotSelector>();
                    if (selector != null)
                    {
                        selector.UpdateVisual();
                    }
                }

                // Disable the parent scroll rect so it doesn't interfere
                if (ParentScrollRect != null)
                {
                    ParentScrollRect.enabled = false;
                }

                // Create ghost
                var selectedList = PersonnelManagementUI.GetSelectedPersonnel();
                int count = selectedList.Count;

                _ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
                // Parent to the top-level personnel panel
                Transform panelRoot = GetPanelRoot();
                if (panelRoot != null)
                {
                    _ghost.transform.SetParent(panelRoot, false);
                }

                _ghostCanvas = _ghost.GetComponentInParent<Canvas>();

                var ghostRect = _ghost.GetComponent<RectTransform>();
                ghostRect.sizeDelta = new Vector2(200, 40);

                var ghostImg = _ghost.AddComponent<Image>();
                ghostImg.color = new Color(0.25f, 0.45f, 0.70f, 0.75f);
                ghostImg.raycastTarget = false;

                var ghostCG = _ghost.GetComponent<CanvasGroup>();
                ghostCG.blocksRaycasts = false;
                ghostCG.alpha = 0.85f;

                var txtGO = new GameObject("GhostText", typeof(RectTransform));
                txtGO.transform.SetParent(_ghost.transform, false);
                var txt = txtGO.AddComponent<Text>();
                txt.font = PersonnelManagementUI.PuristaSemibold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontSize = 22;
                txt.raycastTarget = false;
                var txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                if (count == 1)
                {
                    PersonnelInfo single = selectedList.FirstOrDefault();
                    txt.text = single?.Character?.DisplayName ?? $"Personnel {PersonnelId}";
                }
                else
                {
                    txt.text = $"{count} personnel";
                }

                _ghost.transform.position = eventData.position;
                _isDragging = true;
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnDrag(PointerEventData eventData)
        {
            try
            {
                if (_ghost != null && _isDragging)
                {
                    _ghost.transform.position = eventData.position;
                }
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            try
            {
                _isDragging = false;

                if (_ghost != null)
                {
                    Object.Destroy(_ghost);
                    _ghost = null;
                }

                // Re-enable parent scroll rect
                if (ParentScrollRect != null)
                {
                    ParentScrollRect.enabled = true;
                }

                // Check what we dropped on
                // Use eventData.pointerCurrentRaycast to find the drop target
                GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
                if (hitObject != null)
                {
                    PersonnelColumnDropZone dropZone = hitObject.GetComponentInParent<PersonnelColumnDropZone>();
                    if (dropZone != null)
                    {
                        PersonnelManagementUI.HandleDropOnColumn(dropZone.ColumnAssignment);
                        return;
                    }
                }

                // Dropped outside any column — just deselect
                PersonnelManagementUI.ClearSelection();
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        private Transform GetPanelRoot()
        {
            // Walk up to find the TFTV_PersonnelContainer
            Transform current = transform;
            while (current != null)
            {
                if (current.name == "TFTV_PersonnelContainer")
                {
                    return current;
                }
                current = current.parent;
            }
            return transform.root;
        }
    }

    /// <summary>
    /// Attached to each column's viewport (except Training). Receives drops and highlights during drag-over.
    /// </summary>
    internal class PersonnelColumnDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public PersonnelAssignment ColumnAssignment;
        private Image _image;
        private Color _originalColor;
        private bool _highlighted;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (_image != null)
            {
                _originalColor = _image.color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            try
            {
                ClearHighlight();
                PersonnelManagementUI.HandleDropOnColumn(ColumnAssignment);
            }
            catch (Exception e) { TFTVLogger.Error(e); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Only highlight when something is being dragged
            if (eventData.dragging && _image != null && !_highlighted)
            {
                _highlighted = true;
                _image.color = new Color(
                    _originalColor.r + 0.10f,
                    _originalColor.g + 0.15f,
                    _originalColor.b + 0.05f,
                    Mathf.Min(1f, _originalColor.a + 0.20f));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHighlight();
        }

        private void ClearHighlight()
        {
            if (_highlighted && _image != null)
            {
                _image.color = _originalColor;
                _highlighted = false;
            }
        }
    }

    #endregion
}