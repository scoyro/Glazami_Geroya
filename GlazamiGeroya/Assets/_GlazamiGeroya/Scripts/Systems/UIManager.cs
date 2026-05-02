using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD: подсказка, журнал, мысли героя, обучающие заметки и таймеры.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Optional UI References")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private TMP_Text checklistText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text thoughtText;
    [Header("Typing")]
    [SerializeField] private float thoughtTypingSpeed = 0.035f;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider temperatureSlider;
    [SerializeField] private TMP_Text temperatureValueText;
    [SerializeField] private Gradient temperatureTextGradient;
    

    [Header("Settings")]
    [SerializeField] private float startTemperature = 40f;
    [SerializeField] private float maxTemperature = 120f;
    [SerializeField] private float thoughtDuration = 2.5f;
    [SerializeField] private float hintDuration = 4f;

    public float CurrentTemperature { get; private set; }

    private Coroutine messageRoutine;
    private GameManager gameManager;
    private Coroutine thoughtRoutine;
    private Coroutine hintRoutine;
    private float countdownRemaining;
    private string timerPrefix = string.Empty;
    private bool timerRunning;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        CurrentTemperature = startTemperature;
        UpdateTemperatureUi();
        SetPrompt(string.Empty);
        SetMessage(string.Empty);
        SetTimerText(string.Empty);

        if (checklistPanel != null)
            checklistPanel.SetActive(false);

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnUiMessageRequested -= SetMessage;
            gameManager.EventManager.OnThoughtRequested -= ShowThought;
            gameManager.EventManager.OnHintUnlocked -= ShowHint;
            gameManager.EventManager.OnTemperatureChanged -= SetTemperature;
            gameManager.EventManager.OnIncidentTimerStarted -= StartIncidentTimer;
            gameManager.EventManager.OnValveTimerStarted -= StartValveTimer;
            gameManager.EventManager.OnTimersStopped -= StopCountdown;

            gameManager.EventManager.OnUiMessageRequested += SetMessage;
            gameManager.EventManager.OnThoughtRequested += ShowThought;
            gameManager.EventManager.OnHintUnlocked += ShowHint;
            gameManager.EventManager.OnTemperatureChanged += SetTemperature;
            gameManager.EventManager.OnIncidentTimerStarted += StartIncidentTimer;
            gameManager.EventManager.OnValveTimerStarted += StartValveTimer;
            gameManager.EventManager.OnTimersStopped += StopCountdown;
        }
    }

    private void Update()
    {
        if (!timerRunning)
            return;

        countdownRemaining -= Time.deltaTime;
        if (countdownRemaining < 0f)
            countdownRemaining = 0f;

        SetTimerText($"{timerPrefix}{countdownRemaining:0.0} c");

        if (countdownRemaining <= 0f)
            timerRunning = false;
    }
    private string currentPrompt;
    public void SetPrompt(string text)
    {
        if (promptText == null)
            return;

        if (currentPrompt == text)
            return;

        currentPrompt = text;
        promptText.text = text;
    }

    public void SetMessage(string text, float duration = 4f)
    {
        if (messageText == null)
            return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        if (duration > 0f)
            messageRoutine = StartCoroutine(ShowTimedMessage(text, duration));
        else
            messageText.text = text;
    }

    public void SetTemperature(float value)
    {
        CurrentTemperature = Mathf.Clamp(value, 0f, maxTemperature);
        UpdateTemperatureUi();
        UpdateTemperatureText();
    }
    private void UpdateTemperatureText()
    {
        if (temperatureValueText == null)
            return;

        temperatureValueText.text = $"{CurrentTemperature:0}°C";

        float t = Mathf.InverseLerp(startTemperature, maxTemperature, CurrentTemperature);
        temperatureValueText.color = temperatureTextGradient.Evaluate(t);
    }

    public void RefreshChecklist(IEnumerable<ChecklistTask> tasks)
    {
                if (checklistText == null)
            return;

        if (tasks == null)
        {
            checklistText.text = string.Empty;

            if (checklistPanel != null)
                checklistPanel.SetActive(false);

            return;
        }

        var visibleTasks = tasks
            .Where(t => t != null && t.isVisible)
            .ToList();

        bool hasVisibleTasks = visibleTasks.Count > 0;

        if (checklistPanel != null)
            checklistPanel.SetActive(hasVisibleTasks);

        if (!hasVisibleTasks)
        {
            checklistText.text = string.Empty;
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<b>Задачи</b>");
        sb.AppendLine();

        foreach (var task in visibleTasks)
        {
            if (task.isCompleted)
                sb.AppendLine($"<color=#888888>[v] {task.title}</color>");
            else
                sb.AppendLine($"<color=#FFFFFF>[x] {task.title}</color>");
        }

        checklistText.text = sb.ToString();
    }

    public void ShowThought(string text, float duration = -1f)
    {
        if (thoughtText == null)
            return;

        if (thoughtRoutine != null)
            StopCoroutine(thoughtRoutine);

        float finalDuration = duration > 0f ? duration : thoughtDuration;

        thoughtRoutine = StartCoroutine(
            ShowTypedText(thoughtText, text, thoughtTypingSpeed, finalDuration)
        );
    }
    private IEnumerator ShowTimedMessage(string text, float duration)
    {
        messageText.text = text;
        yield return new WaitForSeconds(duration);
        messageText.text = string.Empty;
        messageRoutine = null;
    }
    private IEnumerator ShowTypedText(TMP_Text target, string text, float typingSpeed, float visibleDuration)
    {
        target.text = string.Empty;

        foreach (char c in text)
        {
            target.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(visibleDuration);

        target.text = string.Empty;
    }

    public void ShowHint(string text, float duration = -1f)
    {
        if (hintText == null)
            return;

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        float finalDuration = duration > 0f ? duration : hintDuration;

        hintRoutine = StartCoroutine(
            ShowTypedText(hintText, text, thoughtTypingSpeed, finalDuration)
        );
    }

    public void StartIncidentTimer(float seconds)
    {
        timerPrefix = "Времени осталось: ";
        StartCountdown(seconds);
    }

    public void StartValveTimer(float seconds)
    {
        timerPrefix = "В огне: ";
        StartCountdown(seconds);
    }

    public void StartCountdown(float seconds)
    {
        countdownRemaining = Mathf.Max(0f, seconds);
        timerRunning = true;
    }

    public void StopCountdown()
    {
        timerRunning = false;
        countdownRemaining = 0f;
        SetTimerText(string.Empty);
    }


    private void SetTimerText(string text)
    {
        if (timerText != null)
            timerText.text = text;
    }

    private void UpdateTemperatureUi()
    {
        if (temperatureSlider != null)
            temperatureSlider.value = CurrentTemperature / maxTemperature;
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;

        gameManager.EventManager.OnUiMessageRequested -= SetMessage;
        gameManager.EventManager.OnThoughtRequested -= ShowThought;
        gameManager.EventManager.OnHintUnlocked -= ShowHint;
        gameManager.EventManager.OnTemperatureChanged -= SetTemperature;
        gameManager.EventManager.OnIncidentTimerStarted -= StartIncidentTimer;
        gameManager.EventManager.OnValveTimerStarted -= StartValveTimer;
        gameManager.EventManager.OnTimersStopped -= StopCountdown;
    }
}
