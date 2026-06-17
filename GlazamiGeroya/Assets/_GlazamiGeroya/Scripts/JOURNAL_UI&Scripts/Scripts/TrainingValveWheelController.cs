using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Учебная мини-игра вентиля для спокойной фазы.
/// Игрок берется за вентиль через InteractionTarget, затем крутит A/D.
/// 0% = полностью закрыт, 100% = полностью открыт.
///
/// Не заменяет финальную пожарную механику вентиля.
/// Этот скрипт нужен, чтобы игрок заранее руками понял связь:
/// закрытый вентиль — поток топлива стихает, открытый — поток возвращается.
/// </summary>
public class TrainingValveWheelController : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public enum CompletionMode
    {
        ReachClosed,
        ReachOpen,
        CloseThenOpen,
        OpenThenClose,
        VisitBothEnds
    }

    [Header("Wheel Visual")]
    [SerializeField] private Transform wheelVisual;
    [SerializeField] private RotationAxis localRotationAxis = RotationAxis.Z;
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 720f;

    [Tooltip("0 = закрыт, 1 = открыт")]
    [SerializeField, Range(0f, 1f)] private float startOpenAmount = 1f;

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.A;
    [SerializeField] private KeyCode openKey = KeyCode.D;
    [SerializeField] private KeyCode releaseKey = KeyCode.Escape;
    [SerializeField] private float rotateSpeed = 0.35f;

    [Header("Control Lock")]
    [SerializeField] private bool lockInteractionWhileControlling = true;
    [SerializeField] private bool lockPlayerWhileControlling = true;
    [SerializeField] private PlayerControlLock playerControlLock;
    [Header("Release Constraints")]
    [Tooltip("Если включено, игрок не сможет нажать Esc раньше времени")]
    [SerializeField] private bool preventEarlyRelease = true;
    
    [Tooltip("Минимальный процент (от 0 до 1) для выхода. 0.8 = 80%")]
    [SerializeField, Range(0f, 1f)] private float requiredReleasePercent = 0.8f;

    [Header("Task")]
    [SerializeField] private CompletionMode completionMode = CompletionMode.CloseThenOpen;

    [Tooltip("Если группа не нужна, можно сразу закрыть это задание.")]
    [SerializeField] private string completesTaskId;

    [Tooltip("Если задача общая для двух вентилей, укажи группу и stepId.")]
    [SerializeField] private ChecklistTaskGroup taskGroup;
    [SerializeField] private string taskGroupStepId;

    [SerializeField, Range(0.001f, 0.1f)] private float endTolerance = 0.02f;
    [SerializeField] private bool completeOnlyOnce = true;
    [SerializeField] private bool releaseOnComplete = false;

    [Header("Prompt")]
    [SerializeField] private string controlPrompt = "A — закрывать | D — открывать | Esc — отпустить";
    [SerializeField] private bool showPercentInPrompt = true;

    [Header("Thoughts")]
    [TextArea]
    [SerializeField] private string fullyClosedThought = "Шум стих. Значит, этот вентиль перекрывает подачу.";

    [TextArea]
    [SerializeField] private string fullyOpenThought = "Подача снова пошла ровно.";

    [TextArea]
    [SerializeField] private string taskCompletedThought = "Ход вентиля проверен.";

    [SerializeField] private float thoughtDuration = 2.5f;

    [Header("Fuel Flow Audio Optional")]
    [SerializeField] private AudioSource fuelFlowAudioSource;
    [SerializeField] private bool updateFuelFlowVolume = true;
    [SerializeField, Range(0f, 1f)] private float closedFlowVolume = 0f;
    [SerializeField, Range(0f, 1f)] private float openFlowVolume = 0.75f;
    [SerializeField] private bool startFlowAudioIfNeeded = true;

    [Header("Turning Audio (Continuous)")]
    [Tooltip("Массив звуков скрипа/трения в процессе вращения")]
    [SerializeField] private AudioClip[] turningClips;
    
    [Tooltip("Отдельный AudioSource на вентиле (поставь ему галочку Loop)")]
    [SerializeField] private AudioSource turningAudioSource;
    [Header("One Shot Audio Optional")]
    [SerializeField] private AudioClip[] fullyClosedClips;
    [SerializeField] private AudioClip[] fullyOpenClips;
    [SerializeField] private AudioClip[] completedClips;
    [SerializeField] private AudioSource oneShotAudioSource;

    [Header("Events")]
    [SerializeField] private UnityEvent onControlStarted;
    [SerializeField] private UnityEvent onControlEnded;
    [SerializeField] private UnityEvent onFullyClosed;
    [SerializeField] private UnityEvent onFullyOpened;
    [SerializeField] private UnityEvent onTaskCompleted;

    private Quaternion baseLocalRotation;
    private float openAmount;
    private bool isControlling;
    private bool reachedClosedDuringCheck;
    private bool reachedOpenDuringCheck;
    private bool taskCompleted;
    private bool closedFeedbackShown;
    private bool openFeedbackShown;

    public float OpenAmount => openAmount;
    public bool IsFullyClosed => openAmount <= endTolerance;
    public bool IsFullyOpen => openAmount >= 1f - endTolerance;
    public bool IsControlling => isControlling;

    private void Awake()
    {
        if (wheelVisual == null)
            wheelVisual = transform;

        baseLocalRotation = wheelVisual.localRotation;
        openAmount = Mathf.Clamp01(startOpenAmount);

        // ТИХАЯ ИНИЦИАЛИЗАЦИЯ: сразу помечаем состояние, чтобы не было ложных срабатываний
        if (IsFullyClosed)
        {
            reachedClosedDuringCheck = true;
            closedFeedbackShown = true;
        }
        else if (IsFullyOpen)
        {
            reachedOpenDuringCheck = true;
            openFeedbackShown = true;
        }

        ApplyRotation();
        UpdateFuelFlowAudio(true);
    }

    private void Update()
    {
        if (!isControlling)
            return;

        // --- ПРОВЕРКА ВЫХОДА (ESC) ---
        if (Input.GetKeyDown(releaseKey))
        {
            // Проверяем, включен ли запрет и достиг ли вентиль нужного процента
            if (preventEarlyRelease && openAmount < requiredReleasePercent)
            {
                int neededPercent = Mathf.RoundToInt(requiredReleasePercent * 100f);
                GameManager.Instance?.UIManager?.SetMessage($"Нельзя прекращать подачу! Докрутите вентиль!%", 4f);
                return; // Прерываем выход, игрок остается привязан к вентилю
            }

            EndControl();
            return;
        }

        // --- ЛОГИКА ВРАЩЕНИЯ ---
        float direction = 0f;

        if (Input.GetKey(closeKey))
            direction -= 1f;

        if (Input.GetKey(openKey))
            direction += 1f;

        if (Mathf.Abs(direction) > 0.01f)
        {
            float previousAmount = openAmount;
            openAmount = Mathf.Clamp01(openAmount + direction * rotateSpeed * Time.deltaTime);

            bool actuallyMoved = Mathf.Abs(openAmount - previousAmount) > 0.0001f;

            if (actuallyMoved)
            {
                ApplyRotation();
                UpdateFuelFlowAudio(false);
                CheckEndStates(true);
                
                PlayTurningAudio();
            }
            else
            {
                StopTurningAudio();
            }
        }
        else
        {
            StopTurningAudio();
        }

        UpdateControlPrompt();
    }
        private void PlayTurningAudio()
    {
        if (turningAudioSource == null || turningClips == null || turningClips.Length == 0)
            return;

        // Если звук сейчас не играет, выбираем новый случайный и запускаем
        if (!turningAudioSource.isPlaying)
        {
            int randomIndex = UnityEngine.Random.Range(0, turningClips.Length);
            turningAudioSource.clip = turningClips[randomIndex];
            turningAudioSource.Play();
        }
    }

    private void StopTurningAudio()
    {
        // Ставим на паузу (чтобы при возобновлении движения звук не дергался)
        if (turningAudioSource != null && turningAudioSource.isPlaying)
        {
            turningAudioSource.Pause();
        }
    }
    
    public void BeginControl()
    {
        if (isControlling)
            return;

        isControlling = true;

        if (lockPlayerWhileControlling)
            playerControlLock?.LockControls();

        if (lockInteractionWhileControlling)
            GameManager.Instance?.InteractionSystem?.LockInteraction(string.Empty); // Изменили тут

        lastMessageText = string.Empty; // Сброс кэша текста
        UpdateControlPrompt();
        onControlStarted?.Invoke();
    }

    public void EndControl()
    {
        if (!isControlling)
            return;

        isControlling = false;
        StopTurningAudio();
        if (lockInteractionWhileControlling)
            GameManager.Instance?.InteractionSystem?.UnlockInteraction();

        if (lockPlayerWhileControlling)
            playerControlLock?.UnlockControls();

        // Очищаем сообщение, передавая пустую строку и 0 секунд
        GameManager.Instance?.UIManager?.SetMessage(string.Empty, 0f); 
        onControlEnded?.Invoke();
    }

    public void SetFullyClosed()
    {
        openAmount = 0f;
        ApplyRotation();
        UpdateFuelFlowAudio(true);
        CheckEndStates(true);
    }

    public void SetFullyOpen()
    {
        openAmount = 1f;
        ApplyRotation();
        UpdateFuelFlowAudio(true);
        CheckEndStates(true);
    }

    private void ApplyRotation()
    {
        if (wheelVisual == null)
            return;

        float angle = Mathf.Lerp(closedAngle, openAngle, openAmount);
        wheelVisual.localRotation = baseLocalRotation * Quaternion.AngleAxis(angle, GetAxisVector());
    }

    private Vector3 GetAxisVector()
    {
        switch (localRotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    private void CheckEndStates(bool showFeedback)
    {
        if (IsFullyClosed)
        {
            reachedClosedDuringCheck = true;

            if (!closedFeedbackShown)
            {
                closedFeedbackShown = true;
                openFeedbackShown = false;

                PlayRandomOneShot(fullyClosedClips);
                onFullyClosed?.Invoke();

                if (showFeedback)
                    RequestThought(fullyClosedThought);
            }
        }

        if (IsFullyOpen)
        {
            reachedOpenDuringCheck = true;

            if (!openFeedbackShown)
            {
                openFeedbackShown = true;
                closedFeedbackShown = false;

                PlayRandomOneShot(fullyClosedClips);
                onFullyOpened?.Invoke();

                if (showFeedback)
                    RequestThought(fullyOpenThought);
            }
        }

        TryCompleteTask(showFeedback);
    }

    private void TryCompleteTask(bool showFeedback)
    {
        if (completeOnlyOnce && taskCompleted)
            return;

        bool shouldComplete = false;

        switch (completionMode)
        {
            case CompletionMode.ReachClosed:
                shouldComplete = IsFullyClosed;
                break;

            case CompletionMode.ReachOpen:
                shouldComplete = IsFullyOpen;
                break;

            case CompletionMode.CloseThenOpen:
                shouldComplete = reachedClosedDuringCheck && IsFullyOpen;
                break;

            case CompletionMode.OpenThenClose:
                shouldComplete = reachedOpenDuringCheck && IsFullyClosed;
                break;

            case CompletionMode.VisitBothEnds:
                shouldComplete = reachedClosedDuringCheck && reachedOpenDuringCheck;
                break;
        }

        if (!shouldComplete)
            return;

        taskCompleted = true;

        if (taskGroup != null && !string.IsNullOrWhiteSpace(taskGroupStepId))
            taskGroup.CompleteStep(taskGroupStepId);
        else if (!string.IsNullOrWhiteSpace(completesTaskId))
            GameManager.Instance?.ChecklistManager?.CompleteTask(completesTaskId);

        PlayRandomOneShot(fullyClosedClips);
        onTaskCompleted?.Invoke();

        if (showFeedback)
            RequestThought(taskCompletedThought);

        if (releaseOnComplete)
            EndControl();
    }

    private void UpdateFuelFlowAudio(bool instant)
    {
        if (!updateFuelFlowVolume || fuelFlowAudioSource == null)
            return;

        if (startFlowAudioIfNeeded && fuelFlowAudioSource.clip != null && !fuelFlowAudioSource.isPlaying)
        {
            fuelFlowAudioSource.loop = true;
            fuelFlowAudioSource.Play();
        }

        float targetVolume = Mathf.Lerp(closedFlowVolume, openFlowVolume, openAmount);

        if (instant)
            fuelFlowAudioSource.volume = targetVolume;
        else
            fuelFlowAudioSource.volume = Mathf.MoveTowards(
                fuelFlowAudioSource.volume,
                targetVolume,
                Time.deltaTime * 2.5f
            );
    }   
    private void PlayRandomOneShot(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        // Выбираем случайный индекс от 0 до длины массива
        int randomIndex = Random.Range(0, clips.Length);
        AudioClip randomClip = clips[randomIndex];

        PlayOneShot(randomClip);
    }

    private string lastMessageText = string.Empty;

    private void UpdateControlPrompt()
    {
        string prompt = BuildPromptText();

        // Обновляем UI только если текст (проценты) реально изменился
        if (lastMessageText != prompt)
        {
            lastMessageText = prompt;
            GameManager.Instance?.UIManager?.SetMessage(prompt, 0f);
        }
    }

    private string BuildPromptText()
    {
        if (!showPercentInPrompt)
            return controlPrompt;

        int percent = Mathf.RoundToInt(openAmount * 100f);
        return $"{controlPrompt}  [{percent}%]";
    }

    private void RequestThought(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameManager.Instance?.EventManager?.RequestThought(text, thoughtDuration);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        if (oneShotAudioSource != null)
        {
            oneShotAudioSource.PlayOneShot(clip);
            return;
        }

        AudioManager.Instance?.Play3D(clip, transform.position);
    }

    private void OnDisable()
    {
        if (isControlling)
            EndControl();
    }
}
