using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PanelProcedureController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private IofDoorController doorController;

    [Header("Cutscene")]
    [SerializeField] private PanelCutsceneController cutsceneController;
    [SerializeField] private bool exitCutsceneOnComplete = true;

    [Header("Failure Behaviour")]
    [SerializeField] private bool exitCutsceneIfDoorIsOpen = true;
    [SerializeField] private float exitAfterFailureDelay = 1.2f;

    [Header("Lever Visual")]
    [SerializeField] private Transform leverTransform;
    [SerializeField] private Transform leverOffRotationPoint;
    [SerializeField] private float leverRotateDuration = 0.5f;

    [Header("Button Visual")]
    [SerializeField] private Transform buttonTransform;
    [SerializeField] private Transform buttonPressedPoint;
    [SerializeField] private float buttonPressDuration = 0.2f;
    [SerializeField] private float buttonReturnDuration = 0.15f;

    [Header("Ventilation")]
    [SerializeField] private AudioSource ventilationAudioSource;
    [SerializeField] private float ventilationFadeOutDuration = 1.5f;
    [SerializeField] private ParticleSystem[] airflowParticles;

    [Header("Panel Lights")]
    [SerializeField] private GameObject ventilationDisabledLamp;
    [SerializeField] private GameObject sealedLamp;
    [SerializeField] private Light ventilationDisabledLight;
    [SerializeField] private Light sealedLight;

    [Header("Audio")]
    [SerializeField] private AudioClip leverClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip sealLockClip;
    [SerializeField] private AudioClip deniedClip;

    [Header("Fire Suppression")]
    [SerializeField] private FireSuppressionLeverController fireSuppressionLever;

    [Header("Unseal")]
    [SerializeField] private bool allowUnsealAfterSuppression = true;

    [Header("Hover Text")]
    [SerializeField] private string pullLeverHover = "Потянуть рычаг вентиляции.";
    [SerializeField] private string ventilationAlreadyDisabledHover = "Вентиляция уже отключена.";
    [SerializeField] private string closeDoorFirstHover = "Сначала закройте дверь.";
    [SerializeField] private string disableVentilationFirstHover = "Сначала отключите вентиляцию.";
    [SerializeField] private string sealRoomHover = "Нажать кнопку герметизации.";
    [SerializeField] private string roomAlreadySealedHover = "Помещение уже герметизировано.";
    [SerializeField] private string suppressionWorkingHover = "Система пожаротушения ещё работает.";
    [SerializeField] private string unsealRoomHover = "Снять герметизацию.";
    [SerializeField] private string roomUnsealedHover = "Герметизация снята.";

    [Header("Messages")]
    [TextArea]
    [SerializeField] private string doorOpenThought = "Сначала нужно закрыть дверь.";

    [TextArea]
    [SerializeField] private string ventilationAlreadyDisabledThought = "Вентиляция уже отключена.";

    [TextArea]
    [SerializeField] private string ventilationRequiredThought = "Сначала нужно отключить вентиляцию.";

    [TextArea]
    [SerializeField] private string ventilationDisabledThought = "Вентиляция отключена.";

    [TextArea]
    [SerializeField] private string sealingMessage = "Герметизация помещения";

    [TextArea]
    [SerializeField] private string systemReadyThought = "Система готова.";

    [TextArea]
    [SerializeField] private string suppressionStillWorkingThought =
        "Систему нельзя отключать, пока пожаротушение не завершено.";

    [TextArea]
    [SerializeField] private string unsealedThought =
        "Герметизация снята. Дверь можно открыть.";

    [SerializeField] private string unsealedUiMessage = "Герметизация снята";

    [Header("Events")]
    [SerializeField] private UnityEvent onVentilationDisabled;
    [SerializeField] private UnityEvent onRoomSealed;
    [SerializeField] private UnityEvent onProcedureCompleted;
    [SerializeField] private UnityEvent onRoomUnsealed;

    private bool ventilationDisabled;
    private bool roomSealed;
    private bool roomUnsealed;
    private bool isBusy;

    public bool VentilationDisabled => ventilationDisabled;
    public bool RoomSealed => roomSealed;
    public bool RoomUnsealed => roomUnsealed;

    // Важно: после снятия герметизации ProcedureCompleted останется true,
    // потому что подготовка щитка уже была выполнена.
    public bool ProcedureCompleted => ventilationDisabled && (roomSealed || roomUnsealed);

    private void Start()
    {
        SetLamp(ventilationDisabledLamp, ventilationDisabledLight, false);
        SetLamp(sealedLamp, sealedLight, false);
    }

    public void TryPullVentilationLever()
    {
        if (isBusy)
            return;

        if (ventilationDisabled)
        {
            RequestThought(ventilationAlreadyDisabledThought);
            PlayDenied();
            return;
        }

        if (roomSealed || roomUnsealed)
            return;

        StartCoroutine(DisableVentilationRoutine());
    }

    public void TryPressSealButton()
    {
        if (isBusy)
            return;

        if (roomUnsealed)
        {
            PlayDenied();
            return;
        }

        if (roomSealed)
        {
            TryUnsealRoom();
            return;
        }

        if (doorController == null || !doorController.IsClosed)
        {
            RequestThought(doorOpenThought);
            PlayDenied();

            if (exitCutsceneIfDoorIsOpen && cutsceneController != null)
                StartCoroutine(ExitCutsceneAfterDelay());

            return;
        }

        if (!ventilationDisabled)
        {
            RequestThought(ventilationRequiredThought);
            PlayDenied();
            return;
        }

        StartCoroutine(SealRoomRoutine());
    }

    private IEnumerator ExitCutsceneAfterDelay()
    {
        yield return new WaitForSeconds(exitAfterFailureDelay);

        if (cutsceneController != null && cutsceneController.IsActive && !cutsceneController.IsMoving)
            cutsceneController.FinishCutscene();
    }

    private void TryUnsealRoom()
    {
        if (!allowUnsealAfterSuppression)
        {
            PlayDenied();
            return;
        }

        if (fireSuppressionLever == null)
        {
            RequestThought("Не найден контроллер системы пожаротушения.");
            PlayDenied();
            return;
        }

        if (fireSuppressionLever.IsWorking)
        {
            RequestThought(suppressionStillWorkingThought);
            PlayDenied();
            return;
        }

        if (!fireSuppressionLever.IsFinished)
        {
            RequestThought("Сначала нужно запустить систему пожаротушения.");
            PlayDenied();
            return;
        }

        if (doorController != null)
            doorController.UnlockDoor();

        roomSealed = false;
        roomUnsealed = true;

        SetLamp(sealedLamp, sealedLight, false);

        if (!string.IsNullOrWhiteSpace(unsealedUiMessage))
            GameManager.Instance?.EventManager?.RequestUiMessage(unsealedUiMessage, 4f);

        RequestThought(unsealedThought);

        onRoomUnsealed?.Invoke();
    }

    private IEnumerator DisableVentilationRoutine()
    {
        isBusy = true;

        if (leverClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(
                leverClip,
                leverTransform != null ? leverTransform.position : transform.position
            );

        yield return RotateLeverDown();

        yield return FadeOutVentilation();

        StopAirflowParticles();

        ventilationDisabled = true;

        SetLamp(ventilationDisabledLamp, ventilationDisabledLight, true);

        RequestThought(ventilationDisabledThought);

        onVentilationDisabled?.Invoke();

        isBusy = false;
    }

    private IEnumerator SealRoomRoutine()
    {
        isBusy = true;

        if (buttonClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(
                buttonClip,
                buttonTransform != null ? buttonTransform.position : transform.position
            );

        yield return PressButtonVisual();

        if (sealLockClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(sealLockClip, transform.position);

        if (doorController != null)
            doorController.SealLockDoor();

        roomSealed = true;
        roomUnsealed = false;

        SetLamp(sealedLamp, sealedLight, true);

        GameManager.Instance?.EventManager?.RequestUiMessage(sealingMessage, 4f);
        RequestThought(systemReadyThought);

        onRoomSealed?.Invoke();
        onProcedureCompleted?.Invoke();

        isBusy = false;

        if (exitCutsceneOnComplete && cutsceneController != null)
            cutsceneController.FinishCutscene();
    }

    private IEnumerator RotateLeverDown()
    {
        if (leverTransform == null || leverOffRotationPoint == null)
            yield break;

        Quaternion startRotation = leverTransform.rotation;
        Quaternion targetRotation = leverOffRotationPoint.rotation;

        float time = 0f;

        while (time < leverRotateDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / leverRotateDuration);
            t = Smooth(t);

            leverTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        leverTransform.rotation = targetRotation;
    }

    private IEnumerator PressButtonVisual()
    {
        if (buttonTransform == null || buttonPressedPoint == null)
            yield break;

        Vector3 startPosition = buttonTransform.position;
        Vector3 pressedPosition = buttonPressedPoint.position;

        float time = 0f;

        while (time < buttonPressDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / buttonPressDuration);
            t = Smooth(t);

            buttonTransform.position = Vector3.Lerp(startPosition, pressedPosition, t);

            yield return null;
        }

        buttonTransform.position = pressedPosition;

        time = 0f;

        while (time < buttonReturnDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / buttonReturnDuration);
            t = Smooth(t);

            buttonTransform.position = Vector3.Lerp(pressedPosition, startPosition, t);

            yield return null;
        }

        buttonTransform.position = startPosition;
    }

    private IEnumerator FadeOutVentilation()
    {
        if (ventilationAudioSource == null)
            yield break;

        float startVolume = ventilationAudioSource.volume;
        float time = 0f;

        while (time < ventilationFadeOutDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / ventilationFadeOutDuration);

            ventilationAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        ventilationAudioSource.volume = 0f;
        ventilationAudioSource.Stop();
    }

    public string GetHoverText(PanelActionType actionType)
    {
        switch (actionType)
        {
            case PanelActionType.VentilationLever:
                if (ventilationDisabled)
                    return ventilationAlreadyDisabledHover;

                return pullLeverHover;

            case PanelActionType.SealButton:
                if (roomUnsealed)
                    return roomUnsealedHover;

                if (roomSealed)
                {
                    if (fireSuppressionLever == null)
                        return "Не назначен контроллер пожаротушения.";

                    if (fireSuppressionLever.IsWorking)
                        return suppressionWorkingHover;

                    if (!fireSuppressionLever.IsFinished)
                        return "Нажать кнопку герметизации.";

                    return unsealRoomHover;
                }

                if (doorController == null || !doorController.IsClosed)
                    return closeDoorFirstHover;

                if (!ventilationDisabled)
                    return disableVentilationFirstHover;

                return sealRoomHover;

            default:
                return string.Empty;
        }
    }

    private void StopAirflowParticles()
    {
        if (airflowParticles == null)
            return;

        foreach (ParticleSystem ps in airflowParticles)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void SetLamp(GameObject lampObject, Light lampLight, bool enabled)
    {
        if (lampObject != null)
            lampObject.SetActive(enabled);

        if (lampLight != null)
            lampLight.enabled = enabled;
    }

    private void RequestThought(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameManager.Instance?.EventManager?.RequestThought(text, 4f);
    }

    private void PlayDenied()
    {
        if (deniedClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play2D(deniedClip);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}