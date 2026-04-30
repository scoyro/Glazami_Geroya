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
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private TMP_Text checklistText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text thoughtText;
    [SerializeField] private Text hintText;
    [SerializeField] private Text timerText;
    [SerializeField] private Slider temperatureSlider;
    [SerializeField] private TMP_Text temperatureValueText;
    [SerializeField] private Gradient temperatureTextGradient;
    

    [Header("Settings")]
    [SerializeField] private float startTemperature = 40f;
    [SerializeField] private float maxTemperature = 120f;
    [SerializeField] private float thoughtDuration = 2.5f;
    [SerializeField] private float hintDuration = 4f;

    public float CurrentTemperature { get; private set; }

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

    public void SetPrompt(string text)
    {
        if (promptText != null)
            promptText.text = text;
    }

    public void SetMessage(string text)
    {
        if (messageText != null)
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
                sb.AppendLine($"<color=#888888><s>✓ {task.title}</s></color>");
            else
                sb.AppendLine($"<color=#FFFFFF>• {task.title}</color>");
        }

        checklistText.text = sb.ToString();
    }

    public void ShowThought(string text)
    {
        if (thoughtText == null)
            return;

        if (thoughtRoutine != null)
            StopCoroutine(thoughtRoutine);

        thoughtRoutine = StartCoroutine(ShowTimedText(thoughtText, text, thoughtDuration));
    }

    public void ShowHint(string text)
    {
        if (hintText == null)
            return;

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        hintRoutine = StartCoroutine(ShowTimedText(hintText, text, hintDuration));
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

    private IEnumerator ShowTimedText(Text target, string text, float duration)
    {
        target.text = text;
        yield return new WaitForSeconds(duration);
        target.text = string.Empty;
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
