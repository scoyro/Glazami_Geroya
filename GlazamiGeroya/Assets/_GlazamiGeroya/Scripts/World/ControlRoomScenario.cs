using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ControlRoomDoorScenarioController : MonoBehaviour
{
    [System.Serializable]
    private class DialogueLine
    {
        [Header("Text")]
        public string speaker = "Рация";

        [TextArea]
        public string text;

        [Header("Timing")]
        public float duration = 3f;
        public float delayAfter = 0.4f;

        [Header("Audio")]
        public AudioClip audioClip;
    }

    [Header("Player")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform controlRoomSpawnPoint;
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Optional Camera")]
    [Tooltip("Если нужно после телепорта повернуть камеру/взгляд в сторону NPC.")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform lookAtAfterTeleportPoint;

    [Header("Door Interaction")]
    [SerializeField] private InteractionTarget doorInteractionTarget;
    [SerializeField] private Collider doorInteractionCollider;
    [SerializeField] private bool disableDoorInteractionAfterStart = true;

    [Header("External Scene Before Entering Control Room")]
    [SerializeField] private DialogueLine[] outsideDialogue;

    [Header("Fade / Door Sounds")]
    [SerializeField] private float fadeOutDuration = 0.45f;
    [SerializeField] private float fadeInDuration = 0.45f;
    [SerializeField] private float blackScreenDelay = 0.35f;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;
    [SerializeField] private Transform doorSoundPoint;

    [Header("Control Room Timing")]
    [SerializeField] private float delayBeforeControlRoomDialogue = 1.2f;

    [Header("Control Room Dialogue")]
    [SerializeField] private DialogueLine[] controlRoomDialogue;

    [Header("Siren / Ambience Optional")]
    [Tooltip("Если нужно приглушить сирену на время диалога.")]
    [SerializeField] private AudioSource sirenAudioSource;
    [SerializeField] private float sirenDialogueVolume = 0.35f;
    [SerializeField] private bool restoreSirenVolumeAfterDialogue = true;

    [Header("Scenario Objects")]
    [Tooltip("Например, огонь/обломки, которые блокируют путь обратно после выбора ветки.")]
    [SerializeField] private GameObject[] enableOnScenarioStart;

    [Tooltip("Например, старые подсказки/объекты, которые больше не должны быть доступны.")]
    [SerializeField] private GameObject[] disableOnScenarioStart;

    [Header("After Dialogue")]
    [Tooltip("Например, InteractionTarget двери выхода из КУ обратно в МО.")]
    [SerializeField] private GameObject[] enableAfterDialogue;

    [SerializeField] private GameObject[] disableAfterDialogue;

    [Header("Messages")]
    [TextArea]
    [SerializeField] private string afterDialogueThought = "Значит, насос ещё работает... Нужно вернуться.";

    [Header("Events")]
    [SerializeField] private UnityEvent onScenarioStarted;
    [SerializeField] private UnityEvent onPlayerTeleportedToControlRoom;
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool started;
    private bool tooLate;
    private float originalSirenVolume;

    public bool Started => started;
    public bool TooLate => tooLate;

    public void StartHelpScenario()
    {
        if (started)
            return;

        started = true;
        StartCoroutine(ScenarioRoutine());
    }

    private IEnumerator ScenarioRoutine()
    {
        onScenarioStarted?.Invoke();

        SetObjects(enableOnScenarioStart, true);
        SetObjects(disableOnScenarioStart, false);

        if (disableDoorInteractionAfterStart)
        {
            if (doorInteractionCollider != null)
                doorInteractionCollider.enabled = false;

            if (doorInteractionTarget != null)
                doorInteractionTarget.SetPromptText("");
        }

        LockPlayer();

        yield return PlayDialogueSequence(outsideDialogue);

        yield return FadeTeleportRoutine();

        if (delayBeforeControlRoomDialogue > 0f)
            yield return new WaitForSeconds(delayBeforeControlRoomDialogue);

        yield return PlayControlRoomDialogueRoutine();

        tooLate = true;

        if (!string.IsNullOrWhiteSpace(afterDialogueThought))
            RequestThought(afterDialogueThought, 4f);

        SetObjects(enableAfterDialogue, true);
        SetObjects(disableAfterDialogue, false);

        UnlockPlayer();

        onDialogueFinished?.Invoke();
    }

    private IEnumerator FadeTeleportRoutine()
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOut(null, fadeOutDuration);
            yield return WaitForFader();
        }

        if (doorOpenClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(doorOpenClip, GetDoorSoundPosition());

        if (blackScreenDelay > 0f)
            yield return new WaitForSeconds(blackScreenDelay);

        TeleportPlayerToControlRoom();

        if (doorCloseClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(doorCloseClip, GetDoorSoundPosition());

        onPlayerTeleportedToControlRoom?.Invoke();

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeIn(null, fadeInDuration);
            yield return WaitForFader();
        }
    }

    private IEnumerator PlayControlRoomDialogueRoutine()
    {
        if (sirenAudioSource != null)
        {
            originalSirenVolume = sirenAudioSource.volume;
            sirenAudioSource.volume = sirenDialogueVolume;
        }

        yield return PlayDialogueSequence(controlRoomDialogue);

        if (sirenAudioSource != null && restoreSirenVolumeAfterDialogue)
            sirenAudioSource.volume = originalSirenVolume;
    }

    private IEnumerator PlayDialogueSequence(DialogueLine[] lines)
    {
        if (lines == null)
            yield break;

        foreach (DialogueLine line in lines)
        {
            if (line == null)
                continue;

            if (line.audioClip != null && AudioManager.Instance != null)
                AudioManager.Instance.Play2D(line.audioClip);

            if (!string.IsNullOrWhiteSpace(line.text))
                RequestDialogue(line.speaker, line.text, line.duration);

            if (line.duration > 0f)
                yield return new WaitForSeconds(line.duration);

            if (line.delayAfter > 0f)
                yield return new WaitForSeconds(line.delayAfter);
        }
    }

    private void TeleportPlayerToControlRoom()
    {
        if (playerRoot == null || controlRoomSpawnPoint == null)
            return;

        CharacterController characterController = playerRoot.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        playerRoot.SetPositionAndRotation(
            controlRoomSpawnPoint.position,
            controlRoomSpawnPoint.rotation
        );

        if (characterController != null)
            characterController.enabled = true;

        RotateCameraAfterTeleport();
    }

    private void RotateCameraAfterTeleport()
    {
        if (playerCamera == null || lookAtAfterTeleportPoint == null)
            return;

        Vector3 direction = lookAtAfterTeleportPoint.position - playerCamera.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        playerCamera.rotation = targetRotation;
    }

    private void LockPlayer()
    {
        playerControlLock?.LockControls();
        interactionSystem?.LockInteraction();
    }

    private void UnlockPlayer()
    {
        interactionSystem?.UnlockInteraction();
        playerControlLock?.UnlockControls();
    }

    private IEnumerator WaitForFader()
    {
        while (ScreenFader.Instance != null && ScreenFader.Instance.IsBusy)
            yield return null;
    }

    private Vector3 GetDoorSoundPosition()
    {
        return doorSoundPoint != null ? doorSoundPoint.position : transform.position;
    }

    private void RequestDialogue(string speaker, string text, float duration)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (GameManager.Instance == null || GameManager.Instance.UIManager == null)
        {
            Debug.LogWarning("ControlRoomDoorScenarioController: UIManager не найден.");
            return;
        }

        GameManager.Instance.UIManager.ShowDialogueLine(speaker, text, duration);
    }

    private void RequestThought(string text, float duration)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameManager.Instance?.EventManager?.RequestThought(text, duration);
    }

    private void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}