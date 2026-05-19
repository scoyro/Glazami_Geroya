using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ActTwoTransitionController : MonoBehaviour
{
    [System.Serializable]
    private class SubtitleLine
    {
        public string speaker;

        public Sprite avatar;

        [TextArea]
        public string text;

        public float duration = 2.5f;
        public float delayAfter = 0.3f;
        public AudioClip audioClip;
    }

    [Header("Zone Out")]
    [SerializeField] private ZoneOutVolumeEffect zoneOutEffect;
    [SerializeField] private AudioSource zoneOutAudioSource;
    [SerializeField] private AudioClip zoneOutClip;

    [Header("Dialogue UI")]
    [SerializeField] private DialogueSubtitleUI dialogueSubtitleUI;

    [Header("Timing")]
    [SerializeField] private float crisisStartDelay = 1.2f;
    [SerializeField] private float restoreAudioDelayAfterZoneOut = 0.2f;

    [Header("Audio Ducking")]
    [SerializeField] private AudioDucker audioDucker;

    [Header("Subtitles Before Crisis")]
    [SerializeField] private SubtitleLine[] beforeCrisisSubtitles;

    [Header("Subtitles After Crisis")]
    [SerializeField] private SubtitleLine[] afterCrisisSubtitles;

    [Header("Crisis Audio")]
    [SerializeField] private AudioClip lampExplosionClip;

    [Tooltip("Отдельный AudioSource сирены. Его потом можно остановить после тушения пожара.")]
    [SerializeField] private AudioSource alarmAudioSource;

    [SerializeField] private AudioClip crisisAmbienceClip;
    [SerializeField] private Transform lampSoundPoint;

    [Header("Control Room Door Close")]
    [SerializeField] private Transform controlRoomDoorTransform;
    [SerializeField] private Transform controlRoomDoorClosedPoint;
    [SerializeField] private float controlRoomDoorCloseDuration = 0.8f;
    [SerializeField] private AudioClip controlRoomDoorCloseClip;
    [SerializeField] private Transform controlRoomDoorSoundPoint;
    [SerializeField] private Collider controlRoomDoorBlockCollider;

    [Header("Lamp Explosion")]
    [SerializeField] private Light lampLight;
    [SerializeField] private GameObject normalLampObject;
    [SerializeField] private GameObject brokenLampObject;
    [SerializeField] private ParticleSystem lampExplosionVfx;

    [Header("Extra Events")]
    [SerializeField] private UnityEvent onZoneOutStarted;
    [SerializeField] private UnityEvent onCrisisMoment;
    [SerializeField] private UnityEvent onControlRoomDoorClosed;
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

        if (crisisStartDelay > 0f)
            yield return new WaitForSeconds(crisisStartDelay);

        yield return PlaySubtitleSequence(beforeCrisisSubtitles);

        TriggerCrisisMoment();

        yield return PlaySubtitleSequence(afterCrisisSubtitles);

        yield return CloseControlRoomDoorRoutine();

        if (restoreAudioDelayAfterZoneOut > 0f)
            yield return new WaitForSeconds(restoreAudioDelayAfterZoneOut);

        audioDucker?.Restore();

        dialogueSubtitleUI?.Hide();

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

    private IEnumerator PlaySubtitleSequence(SubtitleLine[] lines)
    {
        if (lines == null)
            yield break;

        foreach (SubtitleLine line in lines)
        {
            if (line == null)
                continue;

            if (line.audioClip != null && AudioManager.Instance != null)
                AudioManager.Instance.Play2D(line.audioClip);

            if (!string.IsNullOrWhiteSpace(line.text))
            {
                if (dialogueSubtitleUI != null)
                {
                    dialogueSubtitleUI.Show(line.speaker, line.text, line.avatar, line.duration);
                }
                else
                {
                    string fallbackText = string.IsNullOrWhiteSpace(line.speaker)
                        ? line.text
                        : $"{line.speaker}: {line.text}";

                    GameManager.Instance?.EventManager?.RequestThought(fallbackText, line.duration);
                }
            }

            if (line.duration > 0f)
                yield return new WaitForSeconds(line.duration);

            if (line.delayAfter > 0f)
                yield return new WaitForSeconds(line.delayAfter);
        }

        dialogueSubtitleUI?.Hide();
    }

    private IEnumerator CloseControlRoomDoorRoutine()
    {
        if (controlRoomDoorCloseClip != null && AudioManager.Instance != null)
        {
            Vector3 soundPosition = controlRoomDoorSoundPoint != null
                ? controlRoomDoorSoundPoint.position
                : transform.position;

            AudioManager.Instance.Play3D(controlRoomDoorCloseClip, soundPosition);
        }

        if (controlRoomDoorTransform == null || controlRoomDoorClosedPoint == null)
        {
            if (controlRoomDoorBlockCollider != null)
                controlRoomDoorBlockCollider.enabled = true;

            onControlRoomDoorClosed?.Invoke();
            yield break;
        }

        Quaternion startRotation = controlRoomDoorTransform.rotation;
        Quaternion targetRotation = controlRoomDoorClosedPoint.rotation;

        float time = 0f;

        while (time < controlRoomDoorCloseDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / controlRoomDoorCloseDuration);
            t = Smooth(t);

            controlRoomDoorTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        controlRoomDoorTransform.rotation = targetRotation;

        if (controlRoomDoorBlockCollider != null)
            controlRoomDoorBlockCollider.enabled = true;

        onControlRoomDoorClosed?.Invoke();
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}