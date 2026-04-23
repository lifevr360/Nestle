using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;

/// <summary>
/// Data container for one factory section.
/// Assign in the Inspector on SectionManager.
/// </summary>
[System.Serializable]
public class SectionData
{
    [Tooltip("Display name shown on the transition model and UI labels.")]
    public string sectionName = "Section";

    [Tooltip("360 video clip for this section.")]
    public VideoClip sectionVideoClip;

    [Tooltip("All HotspotController GameObjects that belong to this section. Drag from the scene hierarchy.")]
    public List<HotspotController> hotspots = new List<HotspotController>();
}

/// <summary>
/// Central orchestrator for the Nestle 360 walkthrough.
///
/// Flow:
///   1. Start → PlaySection(0)
///   2. VideoPlayer plays the section clip while HotspotManager drives hotspots.
///   3. Video ends → SectionTransition 3D model appears.
///   4. User selects any section from the model → PlaySection(index) called.
/// </summary>
public class SectionManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [Tooltip("The VideoPlayer rendering the 360 sphere material.")]
    public VideoPlayer mainVideoPlayer;

    [Tooltip("The HotspotManager scene component.")]
    public HotspotManager hotspotManager;

    [Tooltip("Root GameObject of the 3D section-transition model. Hidden during playback.")]
    public GameObject sectionTransitionObject;

    [Header("Sections (add 7–9 entries)")]
    public List<SectionData> sections = new List<SectionData>();

    [Header("Events")]
    [Tooltip("Fires when a section begins. Passes the section index.")]
    public UnityEvent<int> OnSectionStarted;

    [Tooltip("Fires when a section video ends (before transition model appears). Passes the section index.")]
    public UnityEvent<int> OnSectionEnded;

    // ── Private State ─────────────────────────────────────────────────────────
    private int _currentIndex = -1;
    private Coroutine _prepareCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Start()
    {
        if (sectionTransitionObject != null)
            sectionTransitionObject.SetActive(false);

        if (mainVideoPlayer != null)
            mainVideoPlayer.loopPointReached += OnVideoEnd;

        PlaySection(0);
    }

    private void OnDestroy()
    {
        if (mainVideoPlayer != null)
            mainVideoPlayer.loopPointReached -= OnVideoEnd;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Load and play a section by index.
    /// Called from Start, from SectionTransitionController (user pick),
    /// or from any UI button via UnityEvent.
    /// </summary>
    public void PlaySection(int index)
    {
        if (index < 0 || index >= sections.Count)
        {
            Debug.LogError($"[SectionManager] Section index {index} is out of range (count: {sections.Count}).");
            return;
        }

        // Hide the transition model whenever a section starts
        if (sectionTransitionObject != null)
            sectionTransitionObject.SetActive(false);

        _currentIndex = index;

        // Pass hotspot list for this section to HotspotManager
        hotspotManager.SetHotspots(sections[index].hotspots);

        // Stop any previous prepare coroutine and start a fresh one
        if (_prepareCoroutine != null)
            StopCoroutine(_prepareCoroutine);

        _prepareCoroutine = StartCoroutine(PrepareAndPlay(index));

        OnSectionStarted?.Invoke(index);
    }

    /// <summary>
    /// Toggle the 3D Floor Plan model on/off.
    /// Wire this to the Floor Plan button's OnClick in the Screen Space Canvas.
    /// When opened manually during playback the video is paused; closing it resumes.
    /// </summary>
    public void ToggleFloorPlan()
    {
        if (sectionTransitionObject == null) return;

        bool willShow = !sectionTransitionObject.activeSelf;

        if (willShow)
        {
            // Pause video while browsing the floor plan
            if (mainVideoPlayer != null && mainVideoPlayer.isPlaying)
                mainVideoPlayer.Pause();

            sectionTransitionObject.SetActive(true);
            SectionTransitionController ctrl = sectionTransitionObject.GetComponent<SectionTransitionController>();
            if (ctrl != null) ctrl.SetupTransition(_currentIndex, sections);
        }
        else
        {
            sectionTransitionObject.SetActive(false);

            // Resume video only if we were in the middle of a section (not at the end)
            if (mainVideoPlayer != null && !mainVideoPlayer.isPlaying)
                mainVideoPlayer.Play();
        }
    }

    /// <summary>Called by SectionTransitionController's Close button.</summary>
    public void CloseFloorPlan()
    {
        if (sectionTransitionObject == null) return;
        sectionTransitionObject.SetActive(false);

        // Resume video if it was paused by manual floor plan open
        if (mainVideoPlayer != null && !mainVideoPlayer.isPlaying)
            mainVideoPlayer.Play();
    }

    public int CurrentIndex => _currentIndex;
    public int SectionCount => sections.Count;
    public SectionData GetSection(int index) => (index >= 0 && index < sections.Count) ? sections[index] : null;

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Playback

    private IEnumerator PrepareAndPlay(int index)
    {
        mainVideoPlayer.Stop();
        mainVideoPlayer.clip = sections[index].sectionVideoClip;
        mainVideoPlayer.Prepare();

        // Wait until the VideoPlayer has buffered enough to start
        while (!mainVideoPlayer.isPrepared)
            yield return null;

        mainVideoPlayer.Play();
        Debug.Log($"[SectionManager] Playing section {index}: {sections[index].sectionName}");
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log($"[SectionManager] Section {_currentIndex} ended.");
        OnSectionEnded?.Invoke(_currentIndex);
        ShowTransitionModel();
    }

    private void ShowTransitionModel()
    {
        if (sectionTransitionObject == null) return;

        sectionTransitionObject.SetActive(true);

        SectionTransitionController ctrl = sectionTransitionObject.GetComponent<SectionTransitionController>();
        if (ctrl != null)
            ctrl.SetupTransition(_currentIndex, sections);
    }

    #endregion
}
