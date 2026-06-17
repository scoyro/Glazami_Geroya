using UnityEngine;
using UnityEngine.Events;

public class QuestValveController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Вращаемая визуальная часть вентиля")]
    [SerializeField] private Transform wheelVisual;
    
    [Tooltip("Ссылка на блокировщик ввода игрока")]
    [SerializeField] private PlayerControlLock playerControlLock;

    [Header("Audio Sources")]
    [Tooltip("Источник звука вращения (должен стоять Loop)")]
    [SerializeField] private AudioSource valveAudioSource;
    
    [Tooltip("Источник звука финального спуска давления (без Loop)")]
    [SerializeField] private AudioSource pressureAudioSource;

    [Header("Rotation Settings")]
    [Tooltip("Клавиша, которую нужно удерживать")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("Сколько секунд нужно непрерывно держать кнопку для полного поворота")]
    [SerializeField] private float rotationDuration = 1.5f;
    
    private enum Axis { X, Y, Z }
    [SerializeField] private Axis rotationAxis = Axis.Z;

    [Header("Checklist Task")]
    [SerializeField] private string completesTaskId;

    [Header("Unity Events")]
    public UnityEvent onRotationStarted;
    public UnityEvent onRotationCompleted;

    private Quaternion baseRotation;
    private bool isAlreadyUsed;
    private bool isControlling;
    private float progress;

    private void Awake()
    {
        if (wheelVisual != null)
            baseRotation = wheelVisual.localRotation;
    }

    public void TriggerValve()
    {
        if (isAlreadyUsed || isControlling) return;

        isControlling = true;
        onRotationStarted?.Invoke();

        if (playerControlLock != null)
            playerControlLock.LockControls();

        if (GameManager.Instance != null && GameManager.Instance.InteractionSystem != null)
            GameManager.Instance.InteractionSystem.LockInteraction("Удерживайте [E] чтобы затянуть");
    }

    private void Update()
    {
        if (!isControlling || isAlreadyUsed) return;

        if (Input.GetKey(interactKey))
        {
            progress += Time.deltaTime / rotationDuration;
            progress = Mathf.Clamp01(progress);

            float currentAngle = Mathf.Lerp(0f, 360f, progress);
            wheelVisual.localRotation = baseRotation * Quaternion.AngleAxis(currentAngle, GetAxisVector());

            if (valveAudioSource != null && !valveAudioSource.isPlaying)
                valveAudioSource.Play();

            if (progress >= 1f)
            {
                CompleteValve();
            }
        }
        else
        {
            if (valveAudioSource != null && valveAudioSource.isPlaying)
                valveAudioSource.Pause();
        }
    }

    private void CompleteValve()
    {
        isAlreadyUsed = true;
        isControlling = false;

        // Останавливаем звук скрипа
        if (valveAudioSource != null && valveAudioSource.isPlaying)
            valveAudioSource.Stop();

        // Проигрываем локальный звук пшика
        if (pressureAudioSource != null)
            pressureAudioSource.Play();

        if (!string.IsNullOrWhiteSpace(completesTaskId) && GameManager.Instance != null)
            GameManager.Instance.ChecklistManager?.CompleteTask(completesTaskId);

        onRotationCompleted?.Invoke();

        if (GameManager.Instance != null && GameManager.Instance.InteractionSystem != null)
            GameManager.Instance.InteractionSystem.UnlockInteraction();

        if (playerControlLock != null)
            playerControlLock.UnlockControls();
    }

    private Vector3 GetAxisVector()
    {
        switch (rotationAxis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }
}