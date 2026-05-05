using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    #region Public Variables

    // ── One step = one node in the seekbar ──────────────────────────────────
    [System.Serializable]
    public class SectionStep
    {
        [Tooltip("Which video index (filename) to stream for this step")]
        public int videoIndex;

        [Tooltip("The circular node Button for this step")]
        public Button nodeButton;

        [Tooltip("The Slider AFTER this node (null for the last node)")]
        public Slider progressSlider;

        [Tooltip("Objects to enable while this step's video is playing")]
        public List<GameObject> activeObjects;

        [Tooltip("Hindi voice-over for this step")]
        public AudioClip hindiAudioClip;

        [Tooltip("English voice-over for this step")]
        public AudioClip englishAudioClip;
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

    [Header("Sections")]
    public List<SectionData> sectionList;

    [Header("Skybox Materials")]
    public Material skyboxPlayMaterial;   // your render texture material
    public Material skyboxPauseMaterial;  // your semi-transparent / frosted material

    private Coroutine overlayCoroutine;

    #endregion

    #region Private Variables

    private Language selectedLanguage = Language.English;
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
        DisableAllStepObjects();
        videoPlayer.loopPointReached += OnVideoFinished;
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f); // wait one frame for all components to initialize
        PlaySection(0);
    }

    private void Update()
    {
        if (isSectionPlaying && videoPlayer.isPlaying && currentSectionIndex >= 0 && currentStepIndex >= 0)
        {
            DriveActiveSlider();
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

        HideAllSeekbars();
        DisableAllStepObjects();

        var section = sectionList[sectionIndex];
        if (section.seekbarRoot != null)
            section.seekbarRoot.SetActive(true);

        currentSectionIndex = sectionIndex;
        currentStepIndex = -1;
        isSectionPlaying = true;

        ToggleEndObject(false);

        RegisterStepButtonListeners(sectionIndex);
        ResetSectionSliders(sectionIndex);

        JumpToStep(0, false);
    }

    /// <summary>
    /// Called when user clicks a node button.
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
            // Jumped backward — empty sliders from target onward
            for (int i = targetStep; i <= currentStepIndex; i++)
            {
                if (section.steps[i].progressSlider != null)
                    section.steps[i].progressSlider.value = 0f;
            }
        }
        else if (userInitiated)
        {
            // Same step clicked — restart slider
            if (section.steps[targetStep].progressSlider != null)
                section.steps[targetStep].progressSlider.value = 0f;
        }

        // Disable previous step's objects before switching
        if (currentStepIndex >= 0 && currentStepIndex < section.steps.Count)
            SetStepObjects(section.steps[currentStepIndex], false);

        currentStepIndex = targetStep;

        if (sectionCoroutine != null)
            StopCoroutine(sectionCoroutine);

        sectionCoroutine = StartCoroutine(PlayStepVideo(section.steps[targetStep]));
    }

    private IEnumerator PlayStepVideo(SectionStep step)
    {
        StopVideoAndAudio();
        DisableAllStepObjects();

        string url = baseVideoURL + step.videoIndex + ".mp4";
        Debug.Log("Section playing video: " + url);

        videoPlayer.url = url;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioMute(0, !isAudioEnabled);
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        // ── Force audio re-init (mirrors the disable/enable fix you discovered) ──
        videoPlayer.gameObject.SetActive(false);
        yield return null;                  // one frame off
        videoPlayer.gameObject.SetActive(true);
        yield return null;                  // one frame back on
                                            // ────────────────────────────────────────────────────────────────────────

        // Re-apply audio settings after re-enable
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioMute(0, !isAudioEnabled);

        SetStepObjects(step, true);

        AudioClip clip = (selectedLanguage == Language.Hindi)
            ? step.hindiAudioClip
            : step.englishAudioClip;

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

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isSectionPlaying)
            OnSectionStepFinished();
    }

    private void OnSectionStepFinished()
    {
        var section = sectionList[currentSectionIndex];
        var currentStep = section.steps[currentStepIndex];

        // Fill current slider fully
        if (currentStep.progressSlider != null)
            currentStep.progressSlider.value = 1f;

        int nextStep = currentStepIndex + 1;

        if (nextStep >= section.steps.Count)
        {
            // All steps done
            SetStepObjects(currentStep, false);
            isSectionPlaying = false;
            if (section.seekbarRoot != null)
                section.seekbarRoot.SetActive(false);
            ToggleEndObject(true);
            Debug.Log("Section " + currentSectionIndex + " complete.");
            return;
        }

        // Auto-advance to next step
        JumpToStep(nextStep, false);
    }

    /// <summary>
    /// Enables or disables all objects in a step's activeObjects list.
    /// </summary>
    private void SetStepObjects(SectionStep step, bool state)
    {
        if (step.activeObjects == null) return;
        foreach (var obj in step.activeObjects)
        {
            if (obj != null) obj.SetActive(state);
        }
    }

    /// <summary>
    /// Disables every activeObject across all sections and steps.
    /// </summary>
    private void DisableAllStepObjects()
    {
        foreach (var section in sectionList)
            foreach (var step in section.steps)
                SetStepObjects(step, false);
    }

    private void RegisterStepButtonListeners(int sectionIndex)
    {
        var section = sectionList[sectionIndex];
        for (int i = 0; i < section.steps.Count; i++)
        {
            int capturedIndex = i;
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

    private void SwapSkybox(bool paused)
    {
        RenderSettings.skybox = paused ? skyboxPauseMaterial : skyboxPlayMaterial;
        DynamicGI.UpdateEnvironment(); // ensures lighting updates with the new skybox
    }

    #endregion

    #region Public Video Controls

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            voiceOverAudioSource.Pause();
            isPaused = true;
            SwapSkybox(true);   // ← swap to pause material
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
            SwapSkybox(false);  // ← revert to render texture material
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

    public void SetSeekbarVisible(bool visible)
    {
        if (currentSectionIndex < 0 || currentSectionIndex >= sectionList.Count) return;

        var seekbar = sectionList[currentSectionIndex].seekbarRoot;
        if (seekbar != null)
            seekbar.SetActive(visible);
    }
    #endregion

    #region Private Helpers

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
