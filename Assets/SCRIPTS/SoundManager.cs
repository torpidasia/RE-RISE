using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    private const string SOUND_STATE_KEY = "SoundState"; // 1 = On, 0 = Off
    private bool isSoundOn = true;

    private void Awake()
    {
        // Singleton pattern (persistent across scenes)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSoundSettings();
    }

    // ===== Toggle Entire Sound System =====
    public void ToggleSound(bool isOn)
    {
        isSoundOn = isOn;
        PlayerPrefs.SetInt(SOUND_STATE_KEY, isSoundOn ? 1 : 0);
        ApplySoundState();
    }

    private void ApplySoundState()
    {
        float volumeValue = isSoundOn ? 0f : -80f; // 0 = full volume, -80 = mute

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVol", volumeValue);
            audioMixer.SetFloat("SFXVol", volumeValue);
            audioMixer.SetFloat("UIVol", volumeValue);
        }

        if (musicSource != null) musicSource.mute = !isSoundOn;
        if (sfxSource != null) sfxSource.mute = !isSoundOn;
        if (uiSource != null) uiSource.mute = !isSoundOn;
    }

    private void LoadSoundSettings()
    {
        isSoundOn = PlayerPrefs.GetInt(SOUND_STATE_KEY, 1) == 1;
        ApplySoundState();
    }

    public bool IsSoundOn()
    {
        return isSoundOn;
    }
}
