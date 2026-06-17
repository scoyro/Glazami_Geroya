using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Универсальный проигрыватель сценарного диалога.
/// Подходит для Сахарова: имя говорящего, текст, аватар/звук,
/// опциональные события и Animator Trigger на каждой реплике.
///
/// Запускать через InteractionTarget -> On Interact() -> StartDialogue().
/// Задание закрывается только после конца диалога.
/// </summary>
public class ScriptedDialogueSequence : MonoBehaviour
{
    [Serializable]
    private class DialogueLine
    {
        [Header("Text")]
        public string speaker = "Сахаров";

        [TextArea]
        public string text;

        [Header("Timing")]
        public float duration = 3f;
        public float delayAfter = 0.35f;

        [Header("Media")]
        public AudioClip audioClip;
        public Sprite avatar;

        [Header("Animation Optional")]
        public Animator animatorOverride;
        public string animatorTrigger;

        [Header("Events Optional")]
        public UnityEvent onLineStarted;
        public UnityEvent onLineFinished;
    }

    [Header("Dialogue UI")]
    [SerializeField] private DialogueSubtitleUI dialogueSubtitleUI;

    [Header("Fallback Animator")]
    [Tooltip("Используется, если в конкретной реплике не указан animatorOverride.")]
    [SerializeField] private Animator defaultAnimator;

    [Header("Player Lock")]
    [SerializeField] private bool lockPlayerDuringDialogue = true;
    [SerializeField] private bool lockInteractionDuringDialogue = true;
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] lines;

    [Header("Checklist")]
    [SerializeField] private string completesTaskId;
    [SerializeField] private bool completeOnlyOnce = true;

    [Header("Objects After Dialogue")]
    [SerializeField] private GameObject[] enableAfterDialogue;
    [SerializeField] private GameObject[] disableAfterDialogue;

    [Header("Thought After Dialogue")]
    [TextArea]
    [SerializeField] private string thoughtAfterDialogue;
    [SerializeField] private float thoughtDuration = 3f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool isPlaying;
    private bool completedOnce;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (interactionSystem == null && GameManager.Instance != null)
            interactionSystem = GameManager.Instance.InteractionSystem;
    }

    public void StartDialogue()
    {
        if (isPlaying)
            return;

        StartCoroutine(DialogueRoutine());
    }

    private IEnumerator DialogueRoutine()
    {
        isPlaying = true;

        LockPlayer();
        onDialogueStarted?.Invoke();

        if (lines != null)
        {
            foreach (DialogueLine line in lines)
            {
                if (line == null)
                    continue;

                line.onLineStarted?.Invoke();
                PlayLineAnimation(line);

                if (line.audioClip != null && AudioManager.Instance != null)
                    AudioManager.Instance.Play2D(line.audioClip);

                ShowLine(line);

                if (line.duration > 0f)
                    yield return new WaitForSeconds(line.duration);

                line.onLineFinished?.Invoke();

                if (line.delayAfter > 0f)
                    yield return new WaitForSeconds(line.delayAfter);
            }
        }

        dialogueSubtitleUI?.Hide();

        CompleteChecklistTask();
        SetObjects(enableAfterDialogue, true);
        SetObjects(disableAfterDialogue, false);

        if (!string.IsNullOrWhiteSpace(thoughtAfterDialogue))
            GameManager.Instance?.EventManager?.RequestThought(thoughtAfterDialogue, thoughtDuration);

        onDialogueFinished?.Invoke();
        UnlockPlayer();

        isPlaying = false;
    }

    private void ShowLine(DialogueLine line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.text))
            return;

        if (dialogueSubtitleUI != null)
        {
            dialogueSubtitleUI.Show(line.speaker, line.text, line.avatar, line.duration);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            GameManager.Instance.UIManager.ShowDialogueLine(line.speaker, line.text, line.duration);
    }

    private void PlayLineAnimation(DialogueLine line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.animatorTrigger))
            return;

        Animator animator = line.animatorOverride != null
            ? line.animatorOverride
            : defaultAnimator;

        if (animator == null)
            return;

        animator.SetTrigger(line.animatorTrigger);
    }

    private void CompleteChecklistTask()
    {
        if (completeOnlyOnce && completedOnce)
            return;

        if (string.IsNullOrWhiteSpace(completesTaskId))
            return;

        completedOnce = true;
        GameManager.Instance?.ChecklistManager?.CompleteTask(completesTaskId);
    }

    private void LockPlayer()
    {
        if (lockPlayerDuringDialogue)
            playerControlLock?.LockControls();

        if (lockInteractionDuringDialogue)
        {
            InteractionSystem system = interactionSystem != null
                ? interactionSystem
                : GameManager.Instance?.InteractionSystem;

            system?.LockInteraction();
        }
    }

    private void UnlockPlayer()
    {
        if (lockInteractionDuringDialogue)
        {
            InteractionSystem system = interactionSystem != null
                ? interactionSystem
                : GameManager.Instance?.InteractionSystem;

            system?.UnlockInteraction();
        }

        if (lockPlayerDuringDialogue)
            playerControlLock?.UnlockControls();
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

    private void OnDisable()
    {
        if (!isPlaying)
            return;

        dialogueSubtitleUI?.Hide();
        UnlockPlayer();
        isPlaying = false;
    }
}
