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
    [SerializeField] private float interactRadius = 0.1f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactionMask = ~0;

    [Header("Stability")]
    [SerializeField] private float promptClearDelay = 0.12f;

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

        InteractionTarget detectedTarget = GetCurrentTarget();

        // Если нашли цель
        if (detectedTarget != null && detectedTarget.CanInteract(gameManager.GameStateManager))
        {
            lastTarget = detectedTarget;
            lastTargetSeenTime = Time.time;

            gameManager.UIManager.SetPrompt(detectedTarget.PromptText);

            if (Input.GetKeyDown(interactKey))
                PerformInteraction(detectedTarget);

            return;
        }

        // Если цель только что потерялась — удерживаем её немного
        if (lastTarget != null && Time.time - lastTargetSeenTime < promptClearDelay)
        {
            gameManager.UIManager.SetPrompt(lastTarget.PromptText);

            if (Input.GetKeyDown(interactKey) && lastTarget.CanInteract(gameManager.GameStateManager))
                PerformInteraction(lastTarget);

            return;
        }

        // Полная потеря цели
        lastTarget = null;
        gameManager.UIManager.SetPrompt(string.Empty);
    }

    private InteractionTarget GetCurrentTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            interactRadius,
            interactDistance,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return null;

        InteractionTarget bestTarget = null;
        float bestScore = float.MinValue;

        foreach (RaycastHit hit in hits)
        {
            InteractionTarget target = hit.collider.GetComponent<InteractionTarget>();

            if (target == null)
                continue;

            Vector3 directionToTarget = (hit.collider.bounds.center - playerCamera.transform.position).normalized;

            float dot = Vector3.Dot(playerCamera.transform.forward, directionToTarget);

            // Чем ближе к центру экрана, тем лучше.
            // Чем ближе физически, тем немного лучше.
            float score = dot * 10f - hit.distance * 0.1f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
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
            events.RequestUiMessage(data.uiMessage, data.textDuration);

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