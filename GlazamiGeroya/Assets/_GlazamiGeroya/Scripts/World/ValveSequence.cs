using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ValveFinalSequenceController : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private ValveCutsceneController valveCutsceneController;
    [SerializeField] private ValveCameraEffectsController cameraEffects;

    [Header("Canonical Ending")]
    [SerializeField] private CanonicalValveEndingController canonicalEndingController;

    [Header("Valve")]
    [SerializeField] private Transform valveWheel;

    [Header("Valve Rotation")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
    [SerializeField] private float totalRotationAngle = 720f;
    [SerializeField] private float valveSmoothTime = 0.15f;

    [Header("Gameplay")]
    [SerializeField] private KeyCode pressKey = KeyCode.E;
    [SerializeField] private float sequenceDuration = 9f;
    [SerializeField] private float progressPerPress = 0.045f;
    [SerializeField] private float progressDecayPerSecond = 0.025f;
    [SerializeField] private float requiredProgress = 1f;

    [Header("Fire Pressure")]
    [SerializeField] private ValveFirePressureController firePressureController;
    [SerializeField] private float successFireFadeDuration = 1.5f;

    [Header("Heat")]
    [SerializeField] private HeatSource valveHeatSource;
    [SerializeField] private float maxHeatBonus = 120f;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private string startPrompt = "Быстро нажимайте E";
    [SerializeField] private string successMessage = "Топливо перекрыто";
    [SerializeField] private string failMessage = "Вы не успели";

    [Header("Audio")]
    [SerializeField] private AudioSource valveAudioSource;
    [SerializeField] private AudioClip pressClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;

    [Header("Valve Breathing")]
    [SerializeField] private AudioSource valveBreathingAudioSource;
    [SerializeField] private AudioClip valveBreathingClip;
    [SerializeField] private bool stopBreathingOnSuccess = false;
    [SerializeField] private bool stopBreathingOnFail = true;

    [Header("Success Moment")]
    [SerializeField] private float delayBeforeSuccessEvent = 1.5f;
    [SerializeField] private AudioSource fuelAudioSource;
    [SerializeField] private AudioSource fireAudioSource;
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private float successAudioFadeDuration = 1.2f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFail;

    [Header("Fail Cinematic")]
    [SerializeField] private Transform failNodTarget;
    [SerializeField] private Transform failFallTarget;
    [SerializeField] private ZoneOutVolumeEffect deathZoneOut;
    [SerializeField] private float failNodDownDuration = 0.45f;
    [SerializeField] private float failNodRecoverDuration = 0.35f;
    [SerializeField] private float failFallDuration = 0.9f;
    [SerializeField] private float failFadeOutDuration = 0.7f;
    [SerializeField] private float delayAfterFailFade = 0.3f;

    [Header("Ending")]
    [SerializeField] private EndingController endingController;
    [SerializeField] private string failEndingId = "escape_without_valve";

    private bool isRunning;
    private bool valveClosed;

    private float progress;
    private float visualProgress;
    private float visualVelocity;

    private Quaternion valveStartRotation;

    private void Awake()
    {
        if (valveWheel != null)
            valveStartRotation = valveWheel.localRotation;
    }

    public void StartSequence()
    {
        if (isRunning)
            return;

        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        isRunning = true;
        valveClosed = false;

        progress = 0f;
        visualProgress = 0f;
        visualVelocity = 0f;

        SetValveHeat(0f);

        if (firePressureController != null)
            firePressureController.ResetFire();

        if (valveWheel != null)
        {
            valveStartRotation = valveWheel.localRotation;
            UpdateValveVisual(true);
        }

        if (valveCutsceneController != null)
        {
            valveCutsceneController.StartCutscene();

            while (valveCutsceneController.IsMoving)
                yield return null;
        }

        if (cameraEffects != null)
            cameraEffects.BeginEffects();

        if (uiManager != null)
            uiManager.SetMessage(startPrompt);

        PlayValveBreathing();

        yield return ValveInputRoutine();

        if (cameraEffects != null)
            cameraEffects.EndEffects();

        if (uiManager != null)
            uiManager.SetPrompt(string.Empty);

        if (valveClosed)
            FinishSuccess();
        else
            FinishFail();
    }

    private IEnumerator ValveInputRoutine()
    {
        float timer = 0f;

        while (timer < sequenceDuration)
        {
            timer += Time.deltaTime;

            float pressure = sequenceDuration > 0f ? timer / sequenceDuration : 1f;
            pressure = Mathf.Clamp01(pressure);

            if (firePressureController != null)
                firePressureController.SetPressure(pressure);

            if (cameraEffects != null)
                cameraEffects.SetStress(pressure);

            SetValveHeat(pressure);

            if (Input.GetKeyDown(pressKey))
            {
                AddProgress();

                if (cameraEffects != null)
                    cameraEffects.Punch();

                if (pressClip != null && valveAudioSource != null)
                    valveAudioSource.PlayOneShot(pressClip);
            }

            if (!valveClosed)
            {
                progress -= progressDecayPerSecond * Time.deltaTime;
                progress = Mathf.Clamp01(progress);
            }

            UpdateValveVisual(false);

            yield return null;
        }
    }

    private void AddProgress()
    {
        if (valveClosed)
            return;

        float resistanceMultiplier;

        if (progress < 0.2f)
            resistanceMultiplier = 0.4f;
        else if (progress < 0.8f)
            resistanceMultiplier = 1f;
        else
            resistanceMultiplier = 0.3f;

        progress += progressPerPress * resistanceMultiplier;
        progress = Mathf.Clamp01(progress);

        if (progress >= requiredProgress)
        {
            progress = requiredProgress;
            valveClosed = true;
        }
    }

    private void UpdateValveVisual(bool instant)
    {
        if (valveWheel == null)
            return;

        if (instant)
        {
            visualProgress = progress;
            visualVelocity = 0f;
        }
        else
        {
            visualProgress = Mathf.SmoothDamp(
                visualProgress,
                progress,
                ref visualVelocity,
                valveSmoothTime
            );
        }

        float angle = totalRotationAngle * visualProgress;
        Quaternion rotationOffset = Quaternion.AngleAxis(angle, localRotationAxis.normalized);

        valveWheel.localRotation = valveStartRotation * rotationOffset;
    }

    private void PlayValveBreathing()
    {
        if (valveBreathingAudioSource == null)
            return;

        if (valveBreathingClip != null)
            valveBreathingAudioSource.clip = valveBreathingClip;

        valveBreathingAudioSource.loop = false;
        valveBreathingAudioSource.pitch = 1f;

        valveBreathingAudioSource.Stop();
        valveBreathingAudioSource.Play();
    }

    private void StopValveBreathing()
    {
        if (valveBreathingAudioSource == null)
            return;

        valveBreathingAudioSource.Stop();
    }

    private void SetValveHeat(float pressure)
    {
        if (valveHeatSource == null)
            return;

        float heatBonus = Mathf.Lerp(0f, maxHeatBonus, Mathf.Clamp01(pressure));
        valveHeatSource.SetTemperatureBonus(heatBonus);
    }

    private void ResetValveHeat()
    {
        if (valveHeatSource != null)
            valveHeatSource.SetTemperatureBonus(0f);
    }

    private void FinishSuccess()
    {
        StartCoroutine(SuccessRoutine());
    }

    private IEnumerator SuccessRoutine()
    {
        isRunning = false;

        ResetValveHeat();

        if (stopBreathingOnSuccess)
            StopValveBreathing();

        if (successClip != null && valveAudioSource != null)
            valveAudioSource.PlayOneShot(successClip);

        if (uiManager != null)
            uiManager.SetMessage(successMessage, 2f);

        if (firePressureController != null)
            firePressureController.FadeDownAfterSuccess(successFireFadeDuration);

        yield return FadeOutAudioSources();

        if (cameraEffects != null)
            cameraEffects.SetStress(0.25f);

        onSuccess?.Invoke();

        if (canonicalEndingController != null)
            canonicalEndingController.StartEnding();
    }

    private void FinishFail()
    {
        if (!isRunning)
            return;

        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        isRunning = false;

        if (failClip != null && valveAudioSource != null)
            valveAudioSource.PlayOneShot(failClip);

        if (uiManager != null)
            uiManager.SetMessage(failMessage, 2f);

        if (cameraEffects != null)
            cameraEffects.SetStress(1f);

        if (valveCutsceneController != null)
        {
            yield return valveCutsceneController.DeathFallRoutine(
                failNodTarget,
                failFallTarget,
                failNodDownDuration,
                failNodRecoverDuration,
                failFallDuration,
                deathZoneOut,
                failFadeOutDuration
            );
        }

        if (stopBreathingOnFail)
            StopValveBreathing();

        yield return new WaitForSeconds(delayAfterFailFade);

        ResetValveHeat();

        onFail?.Invoke();

        if (endingController != null)
            endingController.PlayEnding(failEndingId);
    }

    private IEnumerator FadeOutAudioSources()
    {
        float timer = 0f;

        float fuelStartVolume = fuelAudioSource != null ? fuelAudioSource.volume : 0f;
        float fireStartVolume = fireAudioSource != null ? fireAudioSource.volume : 0f;
        float alarmStartVolume = alarmAudioSource != null ? alarmAudioSource.volume : 0f;

        while (timer < successAudioFadeDuration)
        {
            timer += Time.deltaTime;

            float t = successAudioFadeDuration > 0f ? timer / successAudioFadeDuration : 1f;

            if (fuelAudioSource != null)
                fuelAudioSource.volume = Mathf.Lerp(fuelStartVolume, 0f, t);

            if (fireAudioSource != null)
                fireAudioSource.volume = Mathf.Lerp(fireStartVolume, 0f, t);

            if (alarmAudioSource != null)
                alarmAudioSource.volume = Mathf.Lerp(alarmStartVolume, 0f, t);

            yield return null;
        }

        StopAudioSource(fuelAudioSource);
        StopAudioSource(fireAudioSource);
        StopAudioSource(alarmAudioSource);
    }

    private void StopAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        source.volume = 0f;
        source.Stop();
    }
}