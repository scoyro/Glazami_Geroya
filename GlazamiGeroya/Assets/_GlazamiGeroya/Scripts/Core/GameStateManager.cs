using UnityEngine;

/// <summary>
/// Хранит текущую фазу и отслеживает провал по времени.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [SerializeField] private string incidentTimeoutEndingId = "too_late";
    [SerializeField] private string valveTimeoutEndingId = "burned_at_valve";

    public GamePhase Phase { get; private set; } = GamePhase.Calm;
    public bool IsCrisisMode { get; private set; }
    public bool IsGameOver { get; private set; }
    public GameResult Result { get; private set; } = GameResult.None;
    public string EndingId { get; private set; }

    private GameManager gameManager;
    private float activeTimer;
    private bool timerActive;
    private bool valveTimerActive;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        Phase = GamePhase.Calm;
        IsCrisisMode = false;
        IsGameOver = false;
        Result = GameResult.None;
        EndingId = null;
        activeTimer = 0f;
        timerActive = false;
        valveTimerActive = false;

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisModeChanged;
            gameManager.EventManager.OnPhaseChanged -= HandlePhaseChanged;
            gameManager.EventManager.OnIncidentTimerStarted -= HandleIncidentTimerStarted;
            gameManager.EventManager.OnValveTimerStarted -= HandleValveTimerStarted;
            gameManager.EventManager.OnTimersStopped -= StopTimers;
            gameManager.EventManager.OnGameFinished -= HandleGameFinished;

            gameManager.EventManager.OnCrisisModeChanged += HandleCrisisModeChanged;
            gameManager.EventManager.OnPhaseChanged += HandlePhaseChanged;
            gameManager.EventManager.OnIncidentTimerStarted += HandleIncidentTimerStarted;
            gameManager.EventManager.OnValveTimerStarted += HandleValveTimerStarted;
            gameManager.EventManager.OnTimersStopped += StopTimers;
            gameManager.EventManager.OnGameFinished += HandleGameFinished;
        }
    }

    private void Update()
    {
        if (!timerActive || IsGameOver)
            return;

        activeTimer -= Time.deltaTime;
        if (activeTimer > 0f)
            return;

        timerActive = false;

        if (valveTimerActive)
            MarkDefeat(valveTimeoutEndingId);
        else
            MarkDefeat(incidentTimeoutEndingId);
    }

    public void MarkVictory(string endingId)
    {
        if (IsGameOver) return;
        StopTimers();
        gameManager?.EventManager?.FinishGame(GameResult.Victory, endingId);
    }

    public void MarkDefeat(string endingId)
    {
        if (IsGameOver) return;
        StopTimers();
        gameManager?.EventManager?.FinishGame(GameResult.Defeat, endingId);
    }

    private void HandleCrisisModeChanged(bool enabled)
    {
        IsCrisisMode = enabled;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        Phase = phase;
    }

    private void HandleIncidentTimerStarted(float duration)
    {
        valveTimerActive = false;
        timerActive = true;
        activeTimer = duration;
    }

    private void HandleValveTimerStarted(float duration)
    {
        valveTimerActive = true;
        timerActive = true;
        activeTimer = duration;
    }

    private void StopTimers()
    {
        activeTimer = 0f;
        timerActive = false;
        valveTimerActive = false;
    }

    private void HandleGameFinished(GameResult result, string endingId)
    {
        IsGameOver = true;
        Result = result;
        EndingId = endingId;
        Phase = GamePhase.Ended;
        gameManager?.EventManager?.SetPhase(GamePhase.Ended);
        gameManager?.EndingController?.PlayEnding(result, endingId);
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;

        gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisModeChanged;
        gameManager.EventManager.OnPhaseChanged -= HandlePhaseChanged;
        gameManager.EventManager.OnIncidentTimerStarted -= HandleIncidentTimerStarted;
        gameManager.EventManager.OnValveTimerStarted -= HandleValveTimerStarted;
        gameManager.EventManager.OnTimersStopped -= StopTimers;
        gameManager.EventManager.OnGameFinished -= HandleGameFinished;
    }
}
