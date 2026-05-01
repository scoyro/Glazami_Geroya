using System;
using UnityEngine;

/// <summary>
/// Центральная шина игровых событий.
/// Никаких прямых ссылок между системами логики — только через события.
/// </summary>
public class EventManager : MonoBehaviour
{
    private GameManager gameManager;

    public event Action<string> OnSceneLoaded;
    public event Action<InteractionData> OnInteractionPerformed;
    public event Action<string> OnTaskCompleted;
    public event Action<string, float> OnHintUnlocked;
    public event Action<string, float> OnThoughtRequested;
    public event Action<string> OnUiMessageRequested;
    public event Action<float> OnTemperatureChanged;
    public event Action<float> OnIncidentTimerStarted;
    public event Action<float> OnValveTimerStarted;
    public event Action OnTimersStopped;
    public event Action<bool> OnCrisisModeChanged;
    public event Action<string> OnSfxRequested;
    public event Action<string> OnVoiceRequested;
    public event Action<string> OnVfxRequested;
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<GameResult, string> OnGameFinished;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
    }

    public void RaiseSceneLoaded(string sceneName) => OnSceneLoaded?.Invoke(sceneName);
    public void RaiseInteractionPerformed(InteractionData data) => OnInteractionPerformed?.Invoke(data);
    public void RaiseTaskCompleted(string taskId) => OnTaskCompleted?.Invoke(taskId);
    public void RaiseHintUnlocked(string hintText, float duration = -1f)
    => OnHintUnlocked?.Invoke(hintText, duration);

    public void RequestThought(string thoughtText, float duration = -1f)
        => OnThoughtRequested?.Invoke(thoughtText, duration);
    public void RequestUiMessage(string message) => OnUiMessageRequested?.Invoke(message);
    public void RaiseTemperatureChanged(float temperature) => OnTemperatureChanged?.Invoke(temperature);
    public void StartIncidentTimer(float duration) => OnIncidentTimerStarted?.Invoke(duration);
    public void StartValveTimer(float duration) => OnValveTimerStarted?.Invoke(duration);
    public void StopTimers() => OnTimersStopped?.Invoke();
    public void SetCrisisMode(bool enabled) => OnCrisisModeChanged?.Invoke(enabled);
    public void RequestSfx(string soundId) => OnSfxRequested?.Invoke(soundId);
    public void RequestVoice(string voiceId) => OnVoiceRequested?.Invoke(voiceId);
    public void RequestVfx(string vfxId) => OnVfxRequested?.Invoke(vfxId);
    public void SetPhase(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
    public void FinishGame(GameResult result, string endingId) => OnGameFinished?.Invoke(result, endingId);
}

public enum GamePhase
{
    Calm = 0,
    Crisis = 1,
    ValveSequence = 2,
    Ended = 3
}

public enum GameResult
{
    None = 0,
    Victory = 1,
    Defeat = 2
}

public enum InteractionKind
{
    Observe = 0,
    Action = 1,
    CriticalAction = 2
}

[Serializable]
public struct InteractionData
{
    public string interactionId;
    public InteractionKind interactionKind;
    public string completesTaskId;
    public string hintText;
    public string thoughtText;
    public float textDuration;
    public string uiMessage;
    public string sfxId;
    public string voiceId;
    public string vfxId;
    public float temperatureDelta;
    public bool revealOnlyOnce;
    public bool startCrisis;
}
