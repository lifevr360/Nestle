using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    #region Public Variables
    [System.Serializable]
    public class VideoData
    {
        public string videoPath;       // Local video file path
        public AudioClip hindiAudioClip;  // Hindi audio clip
        public AudioClip englishAudioClip; // English audio clip
    }
    public enum Language { Hindi, English }
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public AudioSource voiceOverAudioSource;
    public List<VideoData> videoList;
    #endregion

    #region Private Variables
    private Language selectedLanguage = Language.English; // Default language
    private int currentVideoIndex = -1; // Track the currently playing video
    #endregion

    #region Unity Methods
    private void Start()
    {
        //PlayVideo(0); // Play the first video
    }
    #endregion

    #region Custom Methods
    public void SetLanguage(int value)
    {
        selectedLanguage = (Language)value;
        Debug.Log("Language set to: " + selectedLanguage);
    }

    public void PlayVideo(int videoIndex)
    {
        // Check if the requested video is already playing
        if (videoIndex == currentVideoIndex && videoPlayer.isPlaying)
        {
            Debug.Log("Video is already playing. Ignoring request.");
            return;
        }

        if (videoIndex < 0 || videoIndex >= videoList.Count)
        {
            Debug.LogError("Invalid video index: " + videoIndex);
            return;
        }

        Debug.Log("Stopped coroutines.");
        StopAllCoroutines();
        StopVideoAndAudio();
        StartCoroutine(StartPlayingVideo(videoIndex));
    }

    private IEnumerator StartPlayingVideo(int index)
    {

        videoPlayer.url = videoList[index].videoPath;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        AudioClip selectedClip = (selectedLanguage == Language.Hindi) ? videoList[index].hindiAudioClip : videoList[index].englishAudioClip;


        if (selectedClip == null)
        {
            Debug.Log("Playing video with embedded audio.");
            voiceOverAudioSource.Stop(); // Ensure external audio doesn't play
        }
        else
        {
            Debug.Log("Playing video without embedded audio. Using internal audio clip.");
            PlayVoiceOver(selectedClip);
        }

        videoPlayer.Play();
        currentVideoIndex = index;
        Debug.Log("Playing video from: " + videoList[index].videoPath);
    }

    private void PlayVoiceOver(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Audio clip is missing for the selected language.");
            return;
        }

        voiceOverAudioSource.Stop();
        voiceOverAudioSource.clip = clip;
        voiceOverAudioSource.Play();
        Debug.Log("Playing internal audio.");
    }

    private void StopVideoAndAudio()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            Debug.Log("Stopped current video.");
        }


        if (voiceOverAudioSource.isPlaying)
        {
            voiceOverAudioSource.Stop();
        }
    }
    #endregion
}
