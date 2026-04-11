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

    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;

    private bool wasUsed;

    public string PromptText => promptText;
    public InteractionData Data => interactionData;

    public bool CanInteract(GameStateManager stateManager)
    {
        if (disableAfterUse && wasUsed)
            return false;

        if (stateManager == null)
            return true;

        if (interactionData.interactionKind == InteractionKind.CriticalAction)
            return stateManager.Phase == GamePhase.Crisis || stateManager.Phase == GamePhase.ValveSequence;

        return true;
    }

    public void Interact()
    {
        if (disableAfterUse && wasUsed)
            return;

        wasUsed = true;
        onInteract?.Invoke();
    }
}
