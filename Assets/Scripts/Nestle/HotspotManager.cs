using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Lives on a persistent scene GameObject.
/// Every frame it reads the main VideoPlayer time and calls Show / Hide / AutoTrigger
/// on each HotspotController in the active section's hotspot list.
///
/// For Video hotspots it also pauses the main video when a panel opens
/// and resumes it when the panel closes.
///
/// SectionManager calls SetHotspots() each time a new section starts.
/// </summary>
public class HotspotManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The main 360 VideoPlayer that drives all hotspot timing.")]
    public VideoPlayer mainVideoPlayer;

    [Header("Active Hotspots (populated at runtime by SectionManager)")]
    public List<HotspotController> hotspots = new List<HotspotController>();

    // ── Private State ─────────────────────────────────────────────────────────
    private bool _pausedForVideoHotspot = false;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        SubscribeAll(hotspots);
    }

    private void OnDestroy()
    {
        UnsubscribeAll(hotspots);
    }

    private void Update()
    {
        if (mainVideoPlayer == null || hotspots == null) return;

        double currentTime = mainVideoPlayer.time;
        foreach (HotspotController hotspot in hotspots)
        {
            if (hotspot != null)
                EvaluateHotspot(hotspot, currentTime);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Called by SectionManager when a new section loads.
    /// Resets the old list, subscribes to the new one.
    /// </summary>
    public void SetHotspots(List<HotspotController> newHotspots)
    {
        // Clean up previous section
        UnsubscribeAll(hotspots);
        ResetAll();

        hotspots = newHotspots ?? new List<HotspotController>();
        SubscribeAll(hotspots);

        _pausedForVideoHotspot = false;
    }

    /// <summary>Resets all hotspots to their initial hidden state.</summary>
    public void ResetAll()
    {
        foreach (HotspotController h in hotspots)
            if (h != null) h.ResetHotspot();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Hotspot Evaluation

    private void EvaluateHotspot(HotspotController hotspot, double videoTime)
    {
        bool inWindow = videoTime >= hotspot.appearTime && videoTime < hotspot.disappearTime;

        if (inWindow)
        {
            hotspot.Show();

            // Auto-trigger once when the video reaches autoTriggerTime
            if (hotspot.autoTriggerTime > 0f && videoTime >= hotspot.autoTriggerTime)
                hotspot.AutoTrigger();
        }
        else
        {
            hotspot.Hide();
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Hotspot Event Callbacks

    private void OnPanelOpened(HotspotController hotspot)
    {
        // Pause main video only for Video hotspots
        if (hotspot.hotspotType == HotspotController.HotspotType.Video)
        {
            if (mainVideoPlayer != null && mainVideoPlayer.isPlaying)
            {
                mainVideoPlayer.Pause();
                _pausedForVideoHotspot = true;
            }
        }
    }

    private void OnPanelClosed(HotspotController hotspot)
    {
        // Resume main video if we paused it for this Video hotspot
        if (hotspot.hotspotType == HotspotController.HotspotType.Video && _pausedForVideoHotspot)
        {
            if (mainVideoPlayer != null)
                mainVideoPlayer.Play();
            _pausedForVideoHotspot = false;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Subscription Helpers

    private void SubscribeAll(List<HotspotController> list)
    {
        foreach (HotspotController h in list)
        {
            if (h == null) continue;
            h.OnPanelOpened += OnPanelOpened;
            h.OnPanelClosed += OnPanelClosed;
        }
    }

    private void UnsubscribeAll(List<HotspotController> list)
    {
        foreach (HotspotController h in list)
        {
            if (h == null) continue;
            h.OnPanelOpened -= OnPanelOpened;
            h.OnPanelClosed -= OnPanelClosed;
        }
    }

    #endregion
}
