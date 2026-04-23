using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to the 3D section-transition model GameObject.
///
/// SectionManager calls SetupTransition() when the active section ends.
/// This script:
///   - Writes section names onto the 3D text labels.
///   - Activates/deactivates per-section button objects.
///   - Listens for mouse clicks and XR selects on those buttons.
///   - Calls SectionManager.PlaySection(index) when the user picks one.
/// </summary>
public class SectionTransitionController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    #region Inspector

    [Header("3D Labels (TextMeshPro — not UI)")]
    [Tooltip("Label showing the section that just finished.")]
    public TextMeshPro currentSectionLabel;

    [Tooltip("Label showing the next sequential section.")]
    public TextMeshPro nextSectionLabel;

    [Header("Per-Section Selection Buttons")]
    [Tooltip("One entry per possible section. Each buttonObject needs a Collider. "
           + "Add up to 9 entries; extras are hidden at runtime.")]
    public List<SectionButtonEntry> sectionButtons = new List<SectionButtonEntry>();

    [Header("Close / Back Button")]
    [Tooltip("Optional close button on the floor plan UI. Wire its OnClick to SectionManager.CloseFloorPlan().")]
    public GameObject closeButton;

    [Header("References")]
    public SectionManager sectionManager;

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Data

    [System.Serializable]
    public class SectionButtonEntry
    {
        [Tooltip("The 3D button GameObject. Must have a Collider.")]
        public GameObject buttonObject;

        [Tooltip("Optional TextMeshPro label on or inside the button.")]
        public TextMeshPro label;

        [Tooltip("Optional XRSimpleInteractable for VR controller / hand interaction.")]
        public XRSimpleInteractable xrInteractable;

        // Set at runtime by SetupTransition
        [HideInInspector] public int sectionIndex = -1;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Private State

    private List<SectionData> _sections;
    private int _currentIndex;

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void OnEnable()
    {
        // Re-subscribe XR listeners every time the model becomes visible
        foreach (SectionButtonEntry btn in sectionButtons)
        {
            if (btn.xrInteractable != null)
                btn.xrInteractable.selectEntered.AddListener(OnXRButtonSelected);
        }
    }

    private void OnDisable()
    {
        foreach (SectionButtonEntry btn in sectionButtons)
        {
            if (btn.xrInteractable != null)
                btn.xrInteractable.selectEntered.RemoveListener(OnXRButtonSelected);
        }
    }

    private void Update()
    {
        HandleMouseClick();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Called by SectionManager just before this GameObject is activated.
    /// Populates labels and configures all section buttons.
    /// </summary>
    public void SetupTransition(int currentIndex, List<SectionData> sections)
    {
        _currentIndex = currentIndex;
        _sections = sections;

        // ── Section labels ────────────────────────────────────────────────────
        if (currentSectionLabel != null)
            currentSectionLabel.text = sections[currentIndex].sectionName;

        int nextIndex = (currentIndex + 1) % sections.Count;
        if (nextSectionLabel != null)
            nextSectionLabel.text = sections[nextIndex].sectionName;

        // ── Section selection buttons ─────────────────────────────────────────
        for (int i = 0; i < sectionButtons.Count; i++)
        {
            SectionButtonEntry btn = sectionButtons[i];
            if (btn.buttonObject == null) continue;

            bool hasSection = i < sections.Count;
            btn.buttonObject.SetActive(hasSection);

            if (hasSection)
            {
                btn.sectionIndex = i;
                if (btn.label != null) btn.label.text = sections[i].sectionName;
            }
            else
            {
                btn.sectionIndex = -1;
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Input Handling

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            SectionButtonEntry matched = FindButtonForHit(hit.collider.gameObject);
            if (matched != null && matched.sectionIndex >= 0)
            {
                sectionManager.PlaySection(matched.sectionIndex);
                return;
            }
        }
    }

    // XR controller / hand ray selects fire this event
    private void OnXRButtonSelected(SelectEnterEventArgs args)
    {
        GameObject selected = args.interactableObject.transform.gameObject;
        SectionButtonEntry matched = FindButtonForObject(selected);
        if (matched != null && matched.sectionIndex >= 0)
            sectionManager.PlaySection(matched.sectionIndex);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers

    private SectionButtonEntry FindButtonForHit(GameObject hitObject)
    {
        foreach (SectionButtonEntry btn in sectionButtons)
        {
            if (btn.buttonObject == null) continue;
            if (hitObject == btn.buttonObject ||
                hitObject.transform.IsChildOf(btn.buttonObject.transform))
                return btn;
        }
        return null;
    }

    private SectionButtonEntry FindButtonForObject(GameObject obj)
    {
        foreach (SectionButtonEntry btn in sectionButtons)
        {
            if (btn.buttonObject == null) continue;
            if (obj == btn.buttonObject ||
                obj.transform.IsChildOf(btn.buttonObject.transform))
                return btn;
        }
        return null;
    }

    #endregion
}
