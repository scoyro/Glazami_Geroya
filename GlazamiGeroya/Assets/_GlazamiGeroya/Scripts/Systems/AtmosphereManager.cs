using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Атмосфера сцены: фон, сирены, системные звуки и мысли героя.
/// </summary>
public class AtmosphereManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Libraries")]
    [SerializeField] private List<AudioEntry> sfxEntries = new List<AudioEntry>();
    [SerializeField] private List<AudioEntry> voiceEntries = new List<AudioEntry>();

    private readonly Dictionary<string, AudioClip> sfxMap = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> voiceMap = new Dictionary<string, AudioClip>();
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        BuildMap(sfxEntries, sfxMap);
        BuildMap(voiceEntries, voiceMap);

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnSfxRequested -= PlaySfx;
            gameManager.EventManager.OnVoiceRequested -= PlayVoice;
            gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisMode;

            gameManager.EventManager.OnSfxRequested += PlaySfx;
            gameManager.EventManager.OnVoiceRequested += PlayVoice;
            gameManager.EventManager.OnCrisisModeChanged += HandleCrisisMode;
        }
    }

    public void SetAmbience(AudioClip clip, bool loop = true)
    {
        if (ambienceSource == null || clip == null) return;

        ambienceSource.clip = clip;
        ambienceSource.loop = loop;
        ambienceSource.Play();
    }

    public void PlaySfx(string id)
    {
        if (sfxSource == null || !sfxMap.TryGetValue(id, out var clip) || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayVoice(string id)
    {
        if (voiceSource == null || !voiceMap.TryGetValue(id, out var clip) || clip == null) return;
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private void HandleCrisisMode(bool enabled)
    {
        if (!enabled) return;
        PlaySfx("alarm");
    }

    private void BuildMap(List<AudioEntry> source, Dictionary<string, AudioClip> map)
    {
        map.Clear();

        foreach (var entry in source)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.clip == null)
                continue;

            map[entry.id] = entry.clip;
        }
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;
        gameManager.EventManager.OnSfxRequested -= PlaySfx;
        gameManager.EventManager.OnVoiceRequested -= PlayVoice;
        gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisMode;
    }
}

[System.Serializable]
public class AudioEntry
{
    public string id;
    public AudioClip clip;
}
