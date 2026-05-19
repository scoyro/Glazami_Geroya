using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ActTwoTransitionController : MonoBehaviour
{
    [Header("Zone Out")]
    [SerializeField] private ZoneOutVolumeEffect zoneOutEffect;
    [SerializeField] private AudioSource zoneOutAudioSource;
    [SerializeField] private AudioClip zoneOutClip;

    [Header("Timing")]
    [SerializeField] private float crisisStartDelay = 1.2f;
    [SerializeField] private float restoreAudioDelayAfterZoneOut = 0.2f;

    [Header("Audio Ducking")]
    [SerializeField] private AudioDucker audioDucker;

    [Header("Crisis Audio")]
    [SerializeField] private AudioClip lampExplosionClip;

    [Tooltip("Отдельный AudioSource сирены. Его потом можно остановить после тушения пожара.")]
    [SerializeField] private AudioSource alarmAudioSource;

    [SerializeField] private AudioClip crisisAmbienceClip;
    [SerializeField] private Transform lampSoundPoint;

    [Header("Lamp Explosion")]
    [SerializeField] private Light lampLight;
    [SerializeField] private GameObject normalLampObject;
    [SerializeField] private GameObject brokenLampObject;
    [SerializeField] private ParticleSystem lampExplosionVfx;

    [Header("Extra Events")]
    [SerializeField] private UnityEvent onZoneOutStarted;
    [SerializeField] private UnityEvent onCrisisMoment;
    [SerializeField] private UnityEvent onTransitionFinished;

    private bool started;

    public void StartTransition()
    {
        if (started)
            return;

        started = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        StartZoneOut();

        yield return new WaitForSeconds(crisisStartDelay);

        TriggerCrisisMoment();

        float remainingZoneOutTime = GetRemainingZoneOutTime();

        if (remainingZoneOutTime > 0f)
            yield return new WaitForSeconds(remainingZoneOutTime);

        if (restoreAudioDelayAfterZoneOut > 0f)
            yield return new WaitForSeconds(restoreAudioDelayAfterZoneOut);

        audioDucker?.Restore();

        onTransitionFinished?.Invoke();
    }

    private void StartZoneOut()
    {
        audioDucker?.Duck();

        if (zoneOutEffect != null)
            zoneOutEffect.Play();

        if (zoneOutAudioSource != null && zoneOutClip != null)
        {
            zoneOutAudioSource.clip = zoneOutClip;
            zoneOutAudioSource.loop = false;
            zoneOutAudioSource.Play();
        }

        onZoneOutStarted?.Invoke();
    }

    private void TriggerCrisisMoment()
    {
        ExplodeLamp();

        GameManager.Instance?.ChoiceSystem?.StartIncident();

        PlayCrisisAudio();

        onCrisisMoment?.Invoke();
    }

    private void ExplodeLamp()
    {
        if (lampLight != null)
            lampLight.enabled = false;

        if (normalLampObject != null)
            normalLampObject.SetActive(false);

        if (brokenLampObject != null)
            brokenLampObject.SetActive(true);

        if (lampExplosionVfx != null)
            lampExplosionVfx.Play();

        if (lampExplosionClip != null && AudioManager.Instance != null)
        {
            Vector3 position = lampSoundPoint != null
                ? lampSoundPoint.position
                : transform.position;

            AudioManager.Instance.Play3D(lampExplosionClip, position);
        }
    }

    private void PlayCrisisAudio()
    {
        if (alarmAudioSource != null)
        {
            alarmAudioSource.loop = true;

            if (!alarmAudioSource.isPlaying)
                alarmAudioSource.Play();
        }

        if (crisisAmbienceClip != null && AudioManager.Instance != null)
            AudioManager.Instance.SetAmbience(crisisAmbienceClip, true);
    }

    private float GetRemainingZoneOutTime()
    {
        if (zoneOutEffect == null)
            return 0f;

        return Mathf.Max(0f, zoneOutEffect.TotalDuration - crisisStartDelay);
    }
}