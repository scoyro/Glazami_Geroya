using System.Collections;
using UnityEngine;

public class RandomAudioEmitter : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private RandomAudioConfig config;

    [Header("Playback Control")]
    [SerializeField] private bool waitUntilClipEnds = true;

    private Coroutine routine;

    private void Start()
    {
        if (config != null && config.playOnStart)
            StartRandomLoop();
    }

    public void StartRandomLoop()
    {
        if (config == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(RandomLoop());
    }

    public void StopRandomLoop()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator RandomLoop()
    {
        while (config != null && config.loopRandomly)
        {
            float delay = config.GetRandomDelay();
            yield return new WaitForSeconds(delay);

            AudioClip clip = config.GetRandomClip();

            if (clip == null || AudioManager.Instance == null)
                continue;

            PlayClip(clip);

            if (waitUntilClipEnds)
                yield return new WaitForSeconds(clip.length);
        }

        routine = null;
    }

    private void PlayClip(AudioClip clip)
    {
        if (config.use3DSound)
        {
            AudioManager.Instance.Play3D(
                clip,
                transform.position,
                config.volume,
                config.minDistance,
                config.maxDistance,
                config.rolloffMode,
                config.fadeInDuration,
                config.fadeOutDuration
            );
        }
        else
        {
            AudioManager.Instance.Play2D(
                clip,
                config.volume,
                config.fadeInDuration,
                config.fadeOutDuration
            );
        }
    }
}