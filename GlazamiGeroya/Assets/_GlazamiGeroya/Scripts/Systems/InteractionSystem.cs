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
    [SerializeField] private float interactRadius = 0.15f;
    [SerializeField] private float promptClearDelay = 0.08f;

    private InteractionTarget lastTarget;
    private float lastTargetSeenTime;

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

        if (target != null && target.CanInteract(gameManager.GameStateManager))
        {
            lastTarget = target;
            lastTargetSeenTime = Time.time;

            gameManager.UIManager.SetPrompt(target.PromptText);

            if (Input.GetKeyDown(interactKey))
                PerformInteraction(target);

            return;
        }

        if (lastTarget != null && Time.time - lastTargetSeenTime < promptClearDelay)
        {
            gameManager.UIManager.SetPrompt(lastTarget.PromptText);
            return;
        }

        lastTarget = null;
        gameManager.UIManager.SetPrompt(string.Empty);
    }

    private InteractionTarget GetCurrentTarget()
    {
        if (!Physics.SphereCast(
                playerCamera.transform.position,
                interactRadius,
                playerCamera.transform.forward,
                out RaycastHit hit,
                interactDistance,
                interactionMask,
                QueryTriggerInteraction.Collide))
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
            events.RaiseHintUnlocked(data.hintText, data.textDuration);

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
