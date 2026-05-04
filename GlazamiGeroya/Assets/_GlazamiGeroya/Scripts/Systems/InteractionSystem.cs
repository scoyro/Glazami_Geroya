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
    [SerializeField] private float allowedAimRadius = 0.35f;
    [SerializeField] private float keepAimRadius = 0.55f;

    [Header("Stability")]
    [SerializeField] private float targetSwitchDelay = 0.25f;
    [SerializeField] private int missedFramesToLose = 6;

    private InteractionTarget lastTarget;
    private float lastTargetSwitchTime;
    private int missedFrameCount;
    private LayerMask interactionMask;
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        if (gameManager != null && gameManager == manager)
            return;

        gameManager = manager;

        if (playerCamera == null)
            playerCamera = Camera.main;

        interactionMask = LayerMask.GetMask("Interactable");
    }

    private void LateUpdate()
    {
        if (playerCamera == null || gameManager == null || gameManager.UIManager == null)
            return;

        InteractionTarget detectedTarget = GetCurrentTarget();

        if (detectedTarget != null && detectedTarget.CanInteract(gameManager.GameStateManager))
        {
            bool canSwitchTarget =
                lastTarget == null ||
                detectedTarget == lastTarget ||
                Time.time - lastTargetSwitchTime >= targetSwitchDelay;

            if (canSwitchTarget)
            {
                if (lastTarget != detectedTarget)
                    lastTargetSwitchTime = Time.time;

                lastTarget = detectedTarget;
            }

            missedFrameCount = 0;
        }
        else
        {
            missedFrameCount++;
        }

        bool targetLost = lastTarget == null
            || !lastTarget.gameObject.activeInHierarchy
            || missedFrameCount >= missedFramesToLose
            || !lastTarget.CanInteract(gameManager.GameStateManager);

        if (!targetLost)
        {
            gameManager.UIManager.SetPrompt(lastTarget.PromptText);

            if (Input.GetKeyDown(interactKey))
                PerformInteraction(lastTarget);

            return;
        }

        if (missedFrameCount >= missedFramesToLose)
        {
            missedFrameCount = 0;
            lastTarget = null;
        }

        gameManager.UIManager.SetPrompt(string.Empty);
    }

    private InteractionTarget GetCurrentTarget()
    {
        // Прямой raycast по центру экрана
        if (Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out RaycastHit hit,
            interactDistance,
            interactionMask,
            QueryTriggerInteraction.Collide))
        {
            InteractionTarget target = hit.collider.GetComponent<InteractionTarget>();
            if (target != null)
                return target;
        }

        // Fallback — SphereCast для объектов чуть в стороне от прицела
        float radius = (lastTarget != null) ? keepAimRadius : allowedAimRadius;

        RaycastHit[] hits = Physics.SphereCastAll(
            playerCamera.transform.position,
            radius,
            playerCamera.transform.forward,
            interactDistance,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        InteractionTarget bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (RaycastHit sphereHit in hits)
        {
            InteractionTarget target = sphereHit.collider.GetComponent<InteractionTarget>();
            if (target == null)
                continue;

            if (sphereHit.distance < bestDistance)
            {
                bestDistance = sphereHit.distance;
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