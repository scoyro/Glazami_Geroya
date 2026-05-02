using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Больше не показывает выбор через UI.
/// Теперь это режиссёр свободного сценария:
/// хранит знания игрока, запускает ЧП и оценивает действия в кризисе.
/// </summary>
public class ChoiceSystem : MonoBehaviour
{
    [Header("Incident Timing")]
    [SerializeField] private float incidentDuration = 60f;
    [SerializeField] private float valveDuration = 9f;
    [SerializeField] private float crisisThoughtDelay = 3f;

    [Header("Key Interaction IDs")]
    [SerializeField] private string startIncidentInteractionId = "incident_alarm";
    [SerializeField] private string callForHelpActionId = "call_for_help";
    [SerializeField] private string extinguisherActionId = "use_fire_suppression";
    [SerializeField] private string runAwayActionId = "evacuate_without_valve";
    [SerializeField] private string enterValveActionId = "reach_valve";
    [SerializeField] private string valveCorrectActionId = "turn_valve_right";
    [SerializeField] private string valveWrongActionId = "turn_valve_left";
    

    [Header("Thoughts")]
    [TextArea] [SerializeField] private string crisisThought = "Пожар... где перекрыть топливо?";
    [TextArea] [SerializeField] private string valveThought = "Нужно перекрыть подачу. У меня мало времени.";
    [TextArea] [SerializeField] private string UIMessage = "";

    private readonly HashSet<string> knownFacts = new HashSet<string>();
    private readonly HashSet<string> usedOneShotThoughts = new HashSet<string>();
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnInteractionPerformed -= HandleInteraction;
            gameManager.EventManager.OnInteractionPerformed += HandleInteraction;
        }
    }

    private void HandleInteraction(InteractionData data)
    {
        RememberObservation(data);

        if (data.startCrisis || data.interactionId == startIncidentInteractionId)
        {
            StartIncident();
            return;
        }

        var state = gameManager?.GameStateManager;
        if (state == null || state.IsGameOver)
            return;

        if (state.Phase == GamePhase.Calm)
            return;

        if (state.Phase == GamePhase.Crisis)
        {
            ResolveCrisisPhase(data.interactionId);
            return;
        }

        if (state.Phase == GamePhase.ValveSequence)
        {
            ResolveValvePhase(data.interactionId);
        }
    }

    private void RememberObservation(InteractionData data)
    {
        if (!string.IsNullOrWhiteSpace(data.interactionId))
            knownFacts.Add(data.interactionId);

        if (!string.IsNullOrWhiteSpace(data.thoughtText) && !data.revealOnlyOnce)
            gameManager?.EventManager?.RequestThought(data.thoughtText, data.textDuration);

        if (!string.IsNullOrWhiteSpace(data.thoughtText) && data.revealOnlyOnce && usedOneShotThoughts.Add(data.interactionId))
            gameManager?.EventManager?.RequestThought(data.thoughtText, data.textDuration);
    }

    public void StartIncident()
    {
        var events = gameManager?.EventManager;
        if (events == null || gameManager.GameStateManager == null || gameManager.GameStateManager.Phase != GamePhase.Calm)
            return;

        events.SetCrisisMode(true);
        events.SetPhase(GamePhase.Crisis);
        events.StartIncidentTimer(incidentDuration);
        events.RequestVfx("crisis_fire");
        events.RequestSfx("alarm");
        StartCoroutine(ShowCrisisThoughtDelayed());
        events.RequestVoice("incident_announcement");
        
        
    }
    private IEnumerator ShowCrisisThoughtDelayed()
{
    yield return new WaitForSeconds(crisisThoughtDelay);

    if (gameManager == null || gameManager.GameStateManager == null)
        yield break;

    if (gameManager.GameStateManager.Phase != GamePhase.Crisis)
        yield break;
    gameManager.EventManager?.RequestUiMessage(UIMessage);
    gameManager.EventManager?.RequestThought(crisisThought);
}

    private void ResolveCrisisPhase(string actionId)
    {
        var state = gameManager?.GameStateManager;
        var events = gameManager?.EventManager;
        if (state == null || events == null) return;

        switch (actionId)
        {
            case var _ when actionId == callForHelpActionId:
                events.RequestThought("Никто не успеет. Нужно действовать самому.");
                state.MarkDefeat("panic_call_help");
                break;

            case var _ when actionId == extinguisherActionId:
                events.RequestThought("Тушение поможет лишь на время. Источник нужно перекрыть.");
                events.RequestSfx("suppressor");
                events.RequestVfx("foam_burst");
                break;

            case var _ when actionId == runAwayActionId:
                events.RequestThought("Если уйти сейчас, топливо продолжит литься.");
                state.MarkDefeat("escape_without_valve");
                break;

            case var _ when actionId == enterValveActionId:
                BeginValveSequence();
                break;
        }
    }

    private void BeginValveSequence()
    {
        var events = gameManager?.EventManager;
        if (events == null) return;

        events.SetPhase(GamePhase.ValveSequence);
        events.StartValveTimer(valveDuration);
        events.RequestThought(valveThought);
        events.RequestVfx("valve_fire");
        events.RaiseTemperatureChanged(gameManager.UIManager != null ? gameManager.UIManager.CurrentTemperature + 15f : 80f);
    }

    private void ResolveValvePhase(string actionId)
    {
        var state = gameManager?.GameStateManager;
        var events = gameManager?.EventManager;
        if (state == null || events == null) return;

        if (actionId == valveWrongActionId)
        {
            events.RequestThought("Не туда! Поток топлива усилился.");
            state.MarkDefeat("wrong_valve_direction");
            return;
        }

        if (actionId == valveCorrectActionId)
        {
            events.RequestThought("Перекрыл. Теперь уходить.");
            events.RequestSfx("metal_valve");
            events.StopTimers();
            state.MarkVictory("aldar_canonical");
        }
    }

    public bool Knows(string interactionId)
    {
        return !string.IsNullOrWhiteSpace(interactionId) && knownFacts.Contains(interactionId);
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;
        gameManager.EventManager.OnInteractionPerformed -= HandleInteraction;
    }
}
