using UnityEngine;

/// <summary>
/// Лучевая система взаимодействий игрока.
/// Никаких всплывающих окон выбора — только осмотр и действие в мире.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactionMask = ~0;

    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (playerCamera == null || gameManager == null || gameManager.UIManager == null)
            return;

        var target = GetCurrentTarget();
        if (target == null)
        {
            gameManager.UIManager.SetPrompt(string.Empty);
            return;
        }

        if (!target.CanInteract(gameManager.GameStateManager))
        {
            gameManager.UIManager.SetPrompt(string.Empty);
            return;
        }

        gameManager.UIManager.SetPrompt(target.PromptText);

        if (Input.GetKeyDown(interactKey))
            PerformInteraction(target);
    }

    private InteractionTarget GetCurrentTarget()
    {
        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance, interactionMask))
            return null;

        return hit.collider.GetComponentInParent<InteractionTarget>();
    }

    private void PerformInteraction(InteractionTarget target)
    {
        target.Interact();

        var data = target.Data;
        var events = gameManager.EventManager;

        if (events == null)
            return;

        events.RaiseInteractionPerformed(data);

        if (!string.IsNullOrWhiteSpace(data.completesTaskId))
            gameManager.ChecklistManager?.CompleteTask(data.completesTaskId);

        if (!string.IsNullOrWhiteSpace(data.hintText))
            events.RaiseHintUnlocked(data.hintText);

        if (!string.IsNullOrWhiteSpace(data.uiMessage))
            events.RequestUiMessage(data.uiMessage);

        if (!string.IsNullOrWhiteSpace(data.sfxId))
            events.RequestSfx(data.sfxId);

        if (!string.IsNullOrWhiteSpace(data.voiceId))
            events.RequestVoice(data.voiceId);

        if (!string.IsNullOrWhiteSpace(data.vfxId))
            events.RequestVfx(data.vfxId);

        if (Mathf.Abs(data.temperatureDelta) > 0.001f)
            gameManager.TemperatureManager?.ApplyTemperatureDelta(data.temperatureDelta);
    }
}
