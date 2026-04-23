using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Place one HotspotController on each hotspot root GameObject.
/// Assign the hotspotIcon (world-space billboard with Collider),
/// uiHotspotPanel and videoHotspotPanel child prefab instances in the Inspector.
/// Set timing, type and content — HotspotManager drives Show/Hide/AutoTrigger.
/// </summary>
public class HotspotController : MonoBehaviour
{
    public enum HotspotType { UI, Video }

    // ── Type ──────────────────────────────────────────────────────────────────
    [Header("Hotspot Type")]
    public HotspotType hotspotType = HotspotType.UI;

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing (seconds in section video)")]
    [Tooltip("Video time at which the icon becomes visible.")]
    public float appearTime;

    [Tooltip("Video time at which the icon (and any open panel) auto-hides.")]
    public float disappearTime;

    [Tooltip("Video time at which the panel opens automatically. Set 0 to disable.")]
    public float autoTriggerTime = 0f;

    [Tooltip("How many seconds the panel stays open before auto-closing. Set 0 to disable.")]
    public float closeDuration = 0f;

    // ── UI Content ────────────────────────────────────────────────────────────
    [Header("UI Hotspot Content (only used when HotspotType = UI)")]
    public string headerText;
    [TextArea(2, 6)] public string bodyText;

    // ── Video Content ─────────────────────────────────────────────────────────
    [Header("Video Hotspot Content (only used when HotspotType = Video)")]
    public VideoClip hotspotVideoClip;
    [TextArea(2, 4)] public string captionText;

    // ── Scene References ──────────────────────────────────────────────────────
    [Header("Child Prefab Instances (assign in Inspector, start disabled)")]
    [Tooltip("The billboard icon / button with a Collider (and optionally XRSimpleInteractable).")]
    public GameObject hotspotIcon;

    [Tooltip("UI panel prefab instance — child of this GameObject.")]
    public GameObject uiHotspotPanel;

    [Tooltip("Video panel prefab instance — child of this GameObject.")]
    public GameObject videoHotspotPanel;

    // ── Events (HotspotManager subscribes to these) ───────────────────────────
    public System.Action<HotspotController> OnPanelOpened;
    public System.Action<HotspotController> OnPanelClosed;

    // ── Private State ─────────────────────────────────────────────────────────
    private bool _panelOpen = false;
    private bool _hasAutoTriggered = false;
    private Coroutine _autoCloseCoroutine;
    private Camera _mainCamera;
    private XRSimpleInteractable _xrInteractable;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        _mainCamera = Camera.main;

        // Wire up XR interaction if the icon has an XRSimpleInteractable
        if (hotspotIcon != null)
        {
            _xrInteractable = hotspotIcon.GetComponent<XRSimpleInteractable>();
            if (_xrInteractable != null)
                _xrInteractable.selectEntered.AddListener(OnXRSelect);
        }
    }

    private void Start()
    {
        InitializeContent();
        SetIconVisible(false);
        SetPanelVisible(false);
    }

    private void Update()
    {
        // Mouse / touch click on the icon (WebGL / standalone)
        if (hotspotIcon != null && hotspotIcon.activeSelf)
            HandlePointerClick();
    }

    private void LateUpdate()
    {
        // Billboard: icon always faces the camera
        if (hotspotIcon != null && hotspotIcon.activeSelf && _mainCamera != null)
        {
            hotspotIcon.transform.LookAt(_mainCamera.transform.position);
            hotspotIcon.transform.Rotate(0f, 180f, 0f);
        }
    }

    private void OnDestroy()
    {
        if (_xrInteractable != null)
            _xrInteractable.selectEntered.RemoveListener(OnXRSelect);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Input Handling

    private void HandlePointerClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (LookAround.isModelInteracting) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            if (hit.collider.gameObject == hotspotIcon ||
                hit.collider.transform.IsChildOf(hotspotIcon.transform))
            {
                TogglePanel();
                return;
            }
        }
    }

    // XR controller / hand ray select
    private void OnXRSelect(SelectEnterEventArgs args) => TogglePanel();

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API (called by HotspotManager)

    /// <summary>Make the icon visible (called when video reaches appearTime).</summary>
    public void Show()
    {
        if (hotspotIcon != null && !hotspotIcon.activeSelf)
            SetIconVisible(true);
    }

    /// <summary>Hide icon and close any open panel (called at disappearTime or section end).</summary>
    public void Hide()
    {
        SetIconVisible(false);
        ClosePanel();
    }

    /// <summary>Auto-open the panel (called once at autoTriggerTime).</summary>
    public void AutoTrigger()
    {
        if (_hasAutoTriggered) return;
        _hasAutoTriggered = true;
        OpenPanel();
    }

    /// <summary>Full reset — called by HotspotManager when a new section loads.</summary>
    public void ResetHotspot()
    {
        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }
        _hasAutoTriggered = false;
        _panelOpen = false;
        SetIconVisible(false);
        SetPanelVisible(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Panel Logic

    public void TogglePanel()
    {
        if (_panelOpen) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        if (_panelOpen) return;
        _panelOpen = true;
        SetPanelVisible(true);
        OnPanelOpened?.Invoke(this);

        if (closeDuration > 0f)
        {
            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }
    }

    public void ClosePanel()
    {
        if (!_panelOpen) return;
        _panelOpen = false;
        SetPanelVisible(false);

        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }

        OnPanelClosed?.Invoke(this);
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDuration);
        ClosePanel();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Internal Helpers

    private void SetIconVisible(bool visible)
    {
        if (hotspotIcon != null) hotspotIcon.SetActive(visible);
    }

    private void SetPanelVisible(bool visible)
    {
        if (hotspotType == HotspotType.UI)
        {
            if (uiHotspotPanel != null) uiHotspotPanel.SetActive(visible);
        }
        else // Video
        {
            if (videoHotspotPanel != null)
            {
                videoHotspotPanel.SetActive(visible);

                // Play / stop the video inside the panel
                VideoPlayer vp = videoHotspotPanel.GetComponentInChildren<VideoPlayer>();
                if (vp != null)
                {
                    if (visible)
                    {
                        vp.clip = hotspotVideoClip;
                        vp.Play();
                    }
                    else
                    {
                        vp.Stop();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Inject text into the panel's TextMeshProUGUI children.
    /// UI panel: [0] = header, [1] = body.
    /// Video panel: [0] = caption.
    /// </summary>
    private void InitializeContent()
    {
        if (hotspotType == HotspotType.UI && uiHotspotPanel != null)
        {
            TextMeshProUGUI[] texts = uiHotspotPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0) texts[0].text = headerText;
            if (texts.Length > 1) texts[1].text = bodyText;
        }
        else if (hotspotType == HotspotType.Video && videoHotspotPanel != null)
        {
            TextMeshProUGUI[] texts = videoHotspotPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0) texts[0].text = captionText;
        }
    }

    #endregion
}
