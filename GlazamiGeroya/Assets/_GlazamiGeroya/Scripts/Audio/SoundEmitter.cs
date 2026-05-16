using System.Collections;
using UnityEngine;

public class RandomAudioEmitter : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private RandomAudioConfig config;

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

            PlayRandomClip();
        }

        routine = null;
    }

    private void PlayRandomClip()
    {
        if (config == null || AudioManager.Instance == null)
            return;

        AudioClip clip = config.GetRandomClip();

        if (clip == null)
            return;

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