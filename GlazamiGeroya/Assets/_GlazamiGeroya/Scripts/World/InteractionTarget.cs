using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Интерактивный объект сцены.
/// Может быть как обучающей заметкой, так и критическим действием в кризисе.
/// </summary>
public class InteractionTarget : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private string promptText = "Нажмите E";

    [Header("Data")]
    [SerializeField] private InteractionData interactionData;
    [SerializeField] private bool disableAfterUse;

    [Header("Audio")]
    [SerializeField] private AudioClip interactionClip;
    [SerializeField] private bool use3DSound = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;

    [SerializeField] private string requiredCompletedTaskId;

    private bool wasUsed;

    public string PromptText => promptText;
    public InteractionData Data => interactionData;
    public void SetPromptText(string text)
    {
        promptText = text;
    }

    public bool CanInteract(GameStateManager stateManager)
    {
        if (disableAfterUse && wasUsed)
            return false;

        if (stateManager == null)
            return true;

        if (!string.IsNullOrWhiteSpace(requiredCompletedTaskId))
        {
            if (GameManager.Instance == null ||
                GameManager.Instance.ChecklistManager == null ||
                !GameManager.Instance.ChecklistManager.IsTaskCompleted(requiredCompletedTaskId))
            {
                return false;
            }
        }

        if (interactionData.interactionKind == InteractionKind.CriticalAction)
            return stateManager.Phase == GamePhase.Crisis || stateManager.Phase == GamePhase.ValveSequence;

        return true;
    }

    public void Interact()
    {
        if (disableAfterUse && wasUsed)
            return;

        wasUsed = true;

        if (interactionClip != null && AudioManager.Instance != null)
        {
            if (use3DSound)
                AudioManager.Instance.Play3D(interactionClip, transform.position);
            else
                AudioManager.Instance.Play2D(interactionClip);
        }

        onInteract?.Invoke();
    }
}