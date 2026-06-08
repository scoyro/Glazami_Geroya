using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CommanderIntroTrigger : MonoBehaviour
{
    [Serializable]
    private class DialogueLine
    {
        public string speaker = "Командир";

        [TextArea]
        public string text = "Алдар, твоя смена. Журнал на столе. Обойдёшь отсек, всё проверишь — вернёшься ко мне.";

        public float duration = 4.5f;
        public float delayAfter = 0.5f;
        public AudioClip audioClip;
        public Sprite avatar;
    }

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableTriggerAfterStart = true;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Look Targets")]
    [SerializeField] private Transform commanderLookTarget;
    [SerializeField] private Transform journalDirectionTarget;

    [Header("Camera Pitch")]
    [Tooltip("Финальный угол камеры после реплики. 0 — прямо. Положительное значение обычно чуть вниз.")]
    [SerializeField] private float finalCameraPitch = 0f;

    [Tooltip("Добавочный угол при взгляде на командира. Если смотрит высоко — увеличь значение.")]
    [SerializeField] private float commanderPitchOffset = 0f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float delayBeforeCommanderLook = 0.5f;
    [SerializeField] private float lookToCommanderDuration = 1.4f;
    [SerializeField] private float delayBeforeJournalLook = 0.25f;
    [SerializeField] private float lookToJournalDuration = 1.0f;

    [Header("Dialogue")]
    [SerializeField] private DialogueSubtitleUI dialogueSubtitleUI;
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("Checklist")]
    [SerializeField] private string firstTaskId = "take_journal";
    [SerializeField] private bool showFirstTaskAfterIntro = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onIntroStarted;
    [SerializeField] private UnityEvent onIntroFinished;

    private bool started;
    private bool introPlaying;

    private Transform playerRoot;
    private Transform playerCamera;
    private Vector3 lockedPlayerPosition;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (playerController != null)
        {
            playerRoot = playerController.transform;
            playerCamera = playerController.cameraTransform;
        }

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            dialogueLines = new DialogueLine[]
            {
                new DialogueLine()
            };
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (started)
            return;

        if (!other.CompareTag(playerTag))
            return;

        StartIntro();
    }

    private void LateUpdate()
    {
        if (!introPlaying)
            return;

        if (playerRoot == null)
            return;

        playerRoot.position = lockedPlayerPosition;
    }

    public void StartIntro()
    {
        if (started)
            return;

        started = true;

        if (disableTriggerAfterStart)
        {
            Collider triggerCollider = GetComponent<Collider>();

            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }

        StartCoroutine(StartIntroDelayedRoutine());
    }
    private IEnumerator StartIntroDelayedRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return IntroRoutine();
    }

    private IEnumerator IntroRoutine()
    {
        if (playerController == null || playerRoot == null || playerCamera == null)
            yield break;

        introPlaying = true;
        lockedPlayerPosition = playerRoot.position;

        LockPlayer();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onIntroStarted?.Invoke();

        if (delayBeforeCommanderLook > 0f)
            yield return new WaitForSeconds(delayBeforeCommanderLook);

        if (commanderLookTarget != null)
            yield return LookAtTargetRoutine(commanderLookTarget, lookToCommanderDuration, true, commanderPitchOffset);

        yield return PlayDialogueSequence();

        if (delayBeforeJournalLook > 0f)
            yield return new WaitForSeconds(delayBeforeJournalLook);

        if (journalDirectionTarget != null)
            yield return FaceDirectionTargetRoutine(journalDirectionTarget, lookToJournalDuration);

        if (showFirstTaskAfterIntro && !string.IsNullOrWhiteSpace(firstTaskId))
            GameManager.Instance?.ChecklistManager?.SetTaskVisible(firstTaskId, true);

        playerController.SyncLookRotationFromCamera();

        UnlockPlayer();

        introPlaying = false;

        onIntroFinished?.Invoke();
    }

    private IEnumerator LookAtTargetRoutine(Transform target, float duration, bool useVerticalAim, float pitchOffset)
    {
        if (target == null || playerRoot == null || playerCamera == null)
            yield break;

        Vector3 flatDirection = target.position - playerRoot.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.001f)
            yield break;

        Quaternion startPlayerRotation = playerRoot.rotation;
        Quaternion targetPlayerRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

        Quaternion startCameraLocalRotation = playerCamera.localRotation;

        float targetPitch = finalCameraPitch;

        if (useVerticalAim)
            targetPitch = CalculatePitchToTarget(target.position, targetPlayerRotation) + pitchOffset;

        Quaternion targetCameraLocalRotation = Quaternion.Euler(targetPitch, 0f, 0f);

        yield return RotatePlayerAndCameraRoutine(
            startPlayerRotation,
            targetPlayerRotation,
            startCameraLocalRotation,
            targetCameraLocalRotation,
            duration
        );
    }

    private IEnumerator FaceDirectionTargetRoutine(Transform target, float duration)
    {
        if (target == null || playerRoot == null || playerCamera == null)
            yield break;

        Vector3 flatDirection = target.position - playerRoot.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.001f)
            yield break;

        Quaternion startPlayerRotation = playerRoot.rotation;
        Quaternion targetPlayerRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

        Quaternion startCameraLocalRotation = playerCamera.localRotation;

        float targetPitch = CalculatePitchToTarget(target.position, targetPlayerRotation);
        Quaternion targetCameraLocalRotation = Quaternion.Euler(targetPitch, 0f, 0f);

        yield return RotatePlayerAndCameraRoutine(
            startPlayerRotation,
            targetPlayerRotation,
            startCameraLocalRotation,
            targetCameraLocalRotation,
            duration
        );
    }

    private IEnumerator RotatePlayerAndCameraRoutine(
        Quaternion startPlayerRotation,
        Quaternion targetPlayerRotation,
        Quaternion startCameraLocalRotation,
        Quaternion targetCameraLocalRotation,
        float duration)
    {
        float time = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            playerRoot.rotation = Quaternion.Slerp(startPlayerRotation, targetPlayerRotation, t);
            playerCamera.localRotation = Quaternion.Slerp(startCameraLocalRotation, targetCameraLocalRotation, t);

            yield return null;
        }

        playerRoot.rotation = targetPlayerRotation;
        playerCamera.localRotation = targetCameraLocalRotation;

        playerController.SyncLookRotationFromCamera();
    }

    private float CalculatePitchToTarget(Vector3 targetPosition, Quaternion targetPlayerRotation)
    {
        Vector3 direction = targetPosition - playerCamera.position;

        if (direction.sqrMagnitude < 0.001f)
            return finalCameraPitch;

        Vector3 localDirection = Quaternion.Inverse(targetPlayerRotation) * direction.normalized;

        float pitch = -Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(
            pitch,
            playerController.minY,
            playerController.maxY
        );

        return pitch;
    }

    private IEnumerator PlayDialogueSequence()
    {
        if (dialogueLines == null)
            yield break;

        foreach (DialogueLine line in dialogueLines)
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
                else if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                {
                    GameManager.Instance.UIManager.ShowDialogueLine(line.speaker, line.text, line.duration);
                }
            }

            if (line.duration > 0f)
                yield return new WaitForSeconds(line.duration);

            if (line.delayAfter > 0f)
                yield return new WaitForSeconds(line.delayAfter);
        }

        dialogueSubtitleUI?.Hide();
    }

    private void LockPlayer()
    {
        playerController.SetCameraExternallyControlled(true);
        playerControlLock?.LockControls();
        interactionSystem?.LockInteraction();
    }

    private void UnlockPlayer()
    {
        playerController.SyncLookRotationFromCamera();

        playerControlLock?.UnlockControls();
        interactionSystem?.UnlockInteraction();

        playerController.SetCameraExternallyControlled(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}