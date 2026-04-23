using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    #region Public Variables

    [System.Serializable]
    public class TimedObject
    {
        public GameObject targetObject;   // Object to control
        public float triggerTime;         // Start time (seconds)
        public float endTime;             // End time (seconds)

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

    public enum Language { Hindi, English }

    [Header("Video Settings")]
    [Tooltip("Base URL ending with '/'")]
    public string baseVideoURL = "https://moxyblr1.s3.ap-south-1.amazonaws.com/Nestle/";

    public VideoPlayer videoPlayer;
    public GameObject onVideoEndObject; 
    public AudioSource voiceOverAudioSource;
    public List<VideoData> videoList;

    #endregion

    #region Private Variables

    private Language selectedLanguage = Language.English;
    private int currentVideoIndex = -1;

    #endregion

    #region Unity Methods
    void Start()
    {
         DisableAllTimedObjects();
        videoPlayer.loopPointReached += OnVideoFinished;
    }
    private void Update()
    {
        if (currentVideoIndex >= 0 && videoPlayer.isPlaying)
        {
            CheckTimedObjects();
        }
    }

    #endregion

    #region Custom Methods

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

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Video finished: " + currentVideoIndex);

        ToggleEndObject(true); 
    }

    public void PlayVideo(int videoIndex)
    {
        if (videoIndex == currentVideoIndex && videoPlayer.isPlaying)
        {
            return;
        }

        if (videoIndex < 0 || videoIndex >= videoList.Count)
        {
            Debug.LogError("Invalid video index: " + videoIndex);
            return;
        }

        StopAllCoroutines();
        StopVideoAndAudio();

        // disable everything from ALL videos
        DisableAllTimedObjects();

        ResetTimedObjects(videoIndex);
        ToggleEndObject(false);

        StartCoroutine(StartPlayingVideo(videoIndex));
    }

    private IEnumerator StartPlayingVideo(int index)
    {
        string videoURL = baseVideoURL + index + ".mp4";

        videoPlayer.url = videoURL;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        AudioClip selectedClip = (selectedLanguage == Language.Hindi)
            ? videoList[index].hindiAudioClip
            : videoList[index].englishAudioClip;

        if (selectedClip == null)
        {
            voiceOverAudioSource.Stop();
        }
        else
        {
            PlayVoiceOver(selectedClip);
        }

        videoPlayer.Play();
        currentVideoIndex = index;

        Debug.Log("Playing video from: " + videoURL);
    }

    private void CheckTimedObjects()
    {
        var videoData = videoList[currentVideoIndex];
        double currentTime = videoPlayer.time;

        foreach (var item in videoData.timedObjects)
        {
            // START
            if (!item.hasStarted && currentTime >= item.triggerTime)
            {
                if (item.targetObject != null)
                {
                    item.targetObject.SetActive(true);
                    Debug.Log("Enabled at: " + item.triggerTime);
                }
                item.hasStarted = true;
            }

            // END
            if (!item.hasEnded && currentTime >= item.endTime)
            {
                if (item.targetObject != null)
                {
                    item.targetObject.SetActive(false);
                    Debug.Log("Disabled at: " + item.endTime);
                }
                item.hasEnded = true;
            }
        }
    }

    private void ResetTimedObjects(int index)
    {
        foreach (var item in videoList[index].timedObjects)
        {
            item.hasStarted = false;
            item.hasEnded = false;

            if (item.targetObject != null)
            {
                item.targetObject.SetActive(false);
            }
        }
    }

    //ensures everything is OFF when switching videos
    private void DisableAllTimedObjects()
    {
        foreach (var video in videoList)
        {
            foreach (var item in video.timedObjects)
            {
                if (item.targetObject != null)
                {
                    item.targetObject.SetActive(false);
                }
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
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (voiceOverAudioSource.isPlaying)
        {
            voiceOverAudioSource.Stop();
        }
    }

    #endregion
}