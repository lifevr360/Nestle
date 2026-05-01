using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    #region Public Variables

    [System.Serializable]
    public class TimedObject
    {
        public GameObject targetObject;
        public float triggerTime;
        public float endTime;

        [HideInInspector] public bool hasStarted;
        [HideInInspector] public bool hasEnded;
    }

    [System.Serializable]
    public class VideoData
    {
        public AudioClip hindiAudioClip;
        public AudioClip englishAudioClip;
        public List<TimedObject> timedObjects;
    }

    // ── One step = one node in the seekbar ──────────────────────────────────
    [System.Serializable]
    public class SectionStep
    {
        [Tooltip("Which index in videoList to play for this step")]
        public int videoIndex;

        [Tooltip("The circular node Button for this step")]
        public Button nodeButton;

        [Tooltip("The Slider AFTER this node (null for the last node)")]
        public Slider progressSlider;
    }

    // ── One section = a full seekbar row ────────────────────────────────────
    [System.Serializable]
    public class SectionData
    {
        [Tooltip("Root GameObject that holds the entire seekbar UI for this section")]
        public GameObject seekbarRoot;

        public List<SectionStep> steps;
    }

    public enum Language { Hindi, English }

    [Header("Video Settings")]
    [Tooltip("Base URL ending with '/'")]
    public string baseVideoURL = "https://moxyblr1.s3.ap-south-1.amazonaws.com/Nestle/";

    [Header("References")]
    public VideoPlayer videoPlayer;
    public GameObject onVideoEndObject;
    public AudioSource voiceOverAudioSource;

    [Header("Video Data")]
    public List<VideoData> videoList;

    [Header("Sections")]
    public List<SectionData> sectionList;

    #endregion

    #region Private Variables

    private Language selectedLanguage = Language.English;
    private int currentVideoIndex = -1;
    private bool isPaused = false;
    private bool isAudioEnabled = true;

    // Section state
    private int currentSectionIndex = -1;
    private int currentStepIndex = -1;
    private bool isSectionPlaying = false;
    private Coroutine sectionCoroutine = null;

    #endregion

    #region Unity Methods

    void Start()
    {
        DisableAllTimedObjects();
        videoPlayer.loopPointReached += OnVideoFinished;
        PlayVideo(9);
    }

    private void Update()
    {
        // Drive timed objects for regular (non-section) videos
        if (!isSectionPlaying && currentVideoIndex >= 0 && videoPlayer.isPlaying)
        {
            CheckTimedObjects(currentVideoIndex);
        }

        // Drive active slider fill from real video time
        if (isSectionPlaying && videoPlayer.isPlaying && currentSectionIndex >= 0 && currentStepIndex >= 0)
        {
            DriveActiveSlider();
            CheckTimedObjects(currentVideoIndex);
        }
    }

    #endregion

    #region Section API

    /// <summary>
    /// Call this to start a section (shows seekbar, auto-plays first video).
    /// </summary>
    public void PlaySection(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= sectionList.Count)
        {
            Debug.LogError("Invalid section index: " + sectionIndex);
            return;
        }

        // Hide all seekbars, show the right one
        HideAllSeekbars();
        var section = sectionList[sectionIndex];
        if (section.seekbarRoot != null)
            section.seekbarRoot.SetActive(true);

        currentSectionIndex = sectionIndex;
        currentStepIndex = -1;
        isSectionPlaying = true;

        ToggleEndObject(false);

        // Wire up button listeners fresh each time
        RegisterStepButtonListeners(sectionIndex);

        // Reset all sliders to 0
        ResetSectionSliders(sectionIndex);

        // Start from step 0
        JumpToStep(0, false);
    }

    /// <summary>
    /// Called when user clicks a node button. Public so buttons can call it via inspector too.
    /// </summary>
    public void OnStepButtonClicked(int stepIndex)
    {
        if (!isSectionPlaying) return;
       
        JumpToStep(stepIndex, true);
    }

    #endregion

    #region Section Internals

    private void JumpToStep(int targetStep, bool userInitiated)
    {
         Debug.Log("index" + targetStep);
        var section = sectionList[currentSectionIndex];

        if (targetStep < 0 || targetStep >= section.steps.Count) return;

        // Fill or empty sliders for skipped steps
        if (targetStep > currentStepIndex)
        {
            // Jumped forward — fill all sliders between old and new
            for (int i = currentStepIndex; i < targetStep; i++)
            {
                if (i >= 0 && section.steps[i].progressSlider != null)
                    section.steps[i].progressSlider.value = 1f;
            }
        }
        else if (targetStep < currentStepIndex)
        {
            // Jumped backward — empty all sliders from target onward
            for (int i = targetStep; i <= currentStepIndex; i++)
            {
                if (section.steps[i].progressSlider != null)
                    section.steps[i].progressSlider.value = 0f;
            }
        }
        // Same step clicked — slider resets to 0 and video restarts
        else if (userInitiated)
        {
            if (section.steps[targetStep].progressSlider != null)
                section.steps[targetStep].progressSlider.value = 0f;
        }

        currentStepIndex = targetStep;

        // Stop any in-flight coroutine
        if (sectionCoroutine != null)
            StopCoroutine(sectionCoroutine);

        int vidIndex = section.steps[targetStep].videoIndex;
        sectionCoroutine = StartCoroutine(PlayStepVideo(vidIndex));
    }

    private IEnumerator PlayStepVideo(int videoIndex)
    {
        StopVideoAndAudio();
        DisableAllTimedObjects();
        ResetTimedObjects(videoIndex);

        string url = baseVideoURL + videoIndex + ".mp4";
        Debug.Log("Section playing video: " + url);

        videoPlayer.url = url;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioMute(0, !isAudioEnabled);
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        currentVideoIndex = videoIndex;

        AudioClip clip = (selectedLanguage == Language.Hindi)
            ? videoList[videoIndex].hindiAudioClip
            : videoList[videoIndex].englishAudioClip;

        if (clip == null)
            voiceOverAudioSource.Stop();
        else
        {
            PlayVoiceOver(clip);
            if (!isAudioEnabled) voiceOverAudioSource.Pause();
        }

        videoPlayer.Play();
        isPaused = false;
    }

    /// <summary>
    /// Smoothly drives the current step's slider based on actual video time.
    /// </summary>
    private void DriveActiveSlider()
    {
        var section = sectionList[currentSectionIndex];
        var step = section.steps[currentStepIndex];

        if (step.progressSlider == null) return;
        if (videoPlayer.frameCount == 0) return;

        float t = (float)(videoPlayer.time / (videoPlayer.frameCount / videoPlayer.frameRate));
        step.progressSlider.value = Mathf.Clamp01(t);
    }

    /// <summary>
    /// Called by loopPointReached. Handles both section auto-advance and regular video end.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isSectionPlaying)
        {
            OnSectionStepFinished();
        }
        else
        {
            Debug.Log("Video finished: " + currentVideoIndex);
            ToggleEndObject(true);
        }
    }

    private void OnSectionStepFinished()
    {
        var section = sectionList[currentSectionIndex];

        // Fill current slider fully
        if (section.steps[currentStepIndex].progressSlider != null)
            section.steps[currentStepIndex].progressSlider.value = 1f;

        int nextStep = currentStepIndex + 1;

        if (nextStep >= section.steps.Count)
        {
            // All steps done
            Debug.Log("Section " + currentSectionIndex + " complete.");
            isSectionPlaying = false;
            // Hide this section's seekbar before showing the end object
            if (sectionList[currentSectionIndex].seekbarRoot != null)
                sectionList[currentSectionIndex].seekbarRoot.SetActive(false);
            ToggleEndObject(true);
            return;
        }

        // Auto-advance
        JumpToStep(nextStep, false);
    }

    private void RegisterStepButtonListeners(int sectionIndex)
    {
        var section = sectionList[sectionIndex];
        for (int i = 0; i < section.steps.Count; i++)
        {
            int capturedIndex = i; // capture for closure
            var btn = section.steps[i].nodeButton;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnStepButtonClicked(capturedIndex));
            }
        }
    }

    private void ResetSectionSliders(int sectionIndex)
    {
        foreach (var step in sectionList[sectionIndex].steps)
        {
            if (step.progressSlider != null)
                step.progressSlider.value = 0f;
        }
    }

    private void HideAllSeekbars()
    {
        foreach (var section in sectionList)
        {
            if (section.seekbarRoot != null)
                section.seekbarRoot.SetActive(false);
        }
    }

    #endregion

    #region Regular Video API (unchanged)

    public void PlayVideo(int videoIndex)
    {
        if (videoIndex == currentVideoIndex && videoPlayer.isPlaying) return;

        if (videoIndex < 0 || videoIndex >= videoList.Count)
        {
            Debug.LogError("Invalid video index: " + videoIndex);
            return;
        }

        // Exit section mode if active
        isSectionPlaying = false;
        HideAllSeekbars();

        StopAllCoroutines();
        StopVideoAndAudio();
        DisableAllTimedObjects();
        ResetTimedObjects(videoIndex);
        ToggleEndObject(false);

        StartCoroutine(StartPlayingVideo(videoIndex));
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            voiceOverAudioSource.Pause();
            isPaused = true;
            Debug.Log("Video Paused");
        }
    }

    public void ResumeVideo()
    {
        if (isPaused)
        {
            videoPlayer.Play();
            if (voiceOverAudioSource.clip != null)
                voiceOverAudioSource.UnPause();
            isPaused = false;
            Debug.Log("Video Resumed");
        }
    }

    public void ToggleAudio(bool enable)
    {
        isAudioEnabled = enable;
        videoPlayer.SetDirectAudioMute(0, !enable);

        if (enable)
        {
            if (voiceOverAudioSource.clip != null && !voiceOverAudioSource.isPlaying)
                voiceOverAudioSource.UnPause();
        }
        else
        {
            voiceOverAudioSource.Pause();
        }

        Debug.Log("Audio Enabled: " + enable);
    }

    public void SetLanguage(int value)
    {
        selectedLanguage = (Language)value;
    }

    public void ToggleEndObject(bool state)
    {
        if (onVideoEndObject != null)
        {
            onVideoEndObject.SetActive(state);
            Debug.Log("End object set to: " + state);
        }
    }

    #endregion

    #region Private Helpers

    private IEnumerator StartPlayingVideo(int index)
    {
        string videoURL = baseVideoURL + index + ".mp4";
        Debug.Log("Playing video from: " + videoURL);

        videoPlayer.url = videoURL;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioMute(0, !isAudioEnabled);
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        AudioClip selectedClip = (selectedLanguage == Language.Hindi)
            ? videoList[index].hindiAudioClip
            : videoList[index].englishAudioClip;

        if (selectedClip == null)
            voiceOverAudioSource.Stop();
        else
        {
            PlayVoiceOver(selectedClip);
            if (!isAudioEnabled) voiceOverAudioSource.Pause();
        }

        videoPlayer.Play();
        currentVideoIndex = index;
        isPaused = false;
    }

    private void CheckTimedObjects(int videoIndex)
    {
        if (videoIndex < 0 || videoIndex >= videoList.Count) return;

        var videoData = videoList[videoIndex];
        double currentTime = videoPlayer.time;

        foreach (var item in videoData.timedObjects)
        {
            if (!item.hasStarted && currentTime >= item.triggerTime)
            {
                if (item.targetObject != null) item.targetObject.SetActive(true);
                item.hasStarted = true;
            }

            if (!item.hasEnded && currentTime >= item.endTime)
            {
                if (item.targetObject != null) item.targetObject.SetActive(false);
                item.hasEnded = true;
            }
        }
    }

    private void ResetTimedObjects(int index)
    {
        if (index < 0 || index >= videoList.Count) return;
        foreach (var item in videoList[index].timedObjects)
        {
            item.hasStarted = false;
            item.hasEnded = false;
            if (item.targetObject != null) item.targetObject.SetActive(false);
        }
    }

    private void DisableAllTimedObjects()
    {
        foreach (var video in videoList)
        {
            foreach (var item in video.timedObjects)
            {
                if (item.targetObject != null) item.targetObject.SetActive(false);
            }
        }
    }

    private void PlayVoiceOver(AudioClip clip)
    {
        if (clip == null) return;
        voiceOverAudioSource.Stop();
        voiceOverAudioSource.clip = clip;
        voiceOverAudioSource.Play();
    }

    private void StopVideoAndAudio()
    {
        if (videoPlayer.isPlaying) videoPlayer.Stop();
        if (voiceOverAudioSource.isPlaying) voiceOverAudioSource.Stop();
    }

    #endregion
}