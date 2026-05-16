using UnityEngine;

[CreateAssetMenu(
    fileName = "RandomAudioConfig",
    menuName = "Audio/Random Audio Config"
)]
public class RandomAudioConfig : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Timing")]
    public float minDelay = 3f;
    public float maxDelay = 10f;

    [Header("Playback")]
    public bool use3DSound = true;
    public bool playOnStart = true;
    public bool loopRandomly = true;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Fade")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.5f;
    
    [Header("3D Distance")]
    public float minDistance = 1f;
    public float maxDistance = 15f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomDelay()
    {
        return Random.Range(minDelay, maxDelay);
    }
}