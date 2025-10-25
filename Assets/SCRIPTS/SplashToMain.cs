using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashToMain : MonoBehaviour
{
    [Header("Scene Settings")]
    public string mainSceneName = "MainScene";

    [Header("Fade Settings")]
    public Image fadeImage;        // 👈 assign your black UI Image here
    public float fadeDuration = 1f;

    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("No VideoPlayer found on this GameObject!");
            return;
        }

        if (fadeImage == null)
            Debug.LogError("Fade Image not assigned in Inspector!");

        // Start by fading in from black
        StartCoroutine(FadeIn());

        // Subscribe to video end event
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = 1 - (t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0;
        fadeImage.color = c;
    }

    IEnumerator FadeOutAndLoad()
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = t / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1;
        fadeImage.color = c;

        SceneManager.LoadScene(mainSceneName);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(FadeOutAndLoad());
    }

    void Update()
    {
        // Optional skip
        if (Input.anyKeyDown)
            StartCoroutine(FadeOutAndLoad());
    }
}
