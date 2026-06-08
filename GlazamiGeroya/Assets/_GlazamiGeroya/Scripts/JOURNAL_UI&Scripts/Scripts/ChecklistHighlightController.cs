using System;
using System.Collections.Generic;
using UnityEngine;

public class ChecklistHighlightController : MonoBehaviour
{
    [Serializable]
    private class HighlightEntry
    {
        public string taskId;
        public EmissionPulseHighlight highlight;
    }

    [Header("Task Highlights")]
    [SerializeField] private List<HighlightEntry> highlights = new List<HighlightEntry>();

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 0.15f;

    private readonly Dictionary<EmissionPulseHighlight, bool> highlightStates =
        new Dictionary<EmissionPulseHighlight, bool>();

    private float refreshTimer;

    private void Start()
    {
        RefreshHighlights();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        RefreshHighlights();
    }

    public void RefreshHighlights()
    {
        ChecklistManager checklistManager = GameManager.Instance != null
            ? GameManager.Instance.ChecklistManager
            : null;

        if (checklistManager == null)
            return;

        HashSet<EmissionPulseHighlight> wantedHighlights =
            new HashSet<EmissionPulseHighlight>();

        foreach (HighlightEntry entry in highlights)
        {
            if (entry == null)
                continue;

            if (entry.highlight == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.taskId))
                continue;

            if (!checklistManager.Tasks.TryGetValue(entry.taskId, out ChecklistTask task))
                continue;

            if (task == null)
                continue;

            if (task.isVisible && !task.isCompleted)
                wantedHighlights.Add(entry.highlight);
        }

        foreach (HighlightEntry entry in highlights)
        {
            if (entry == null || entry.highlight == null)
                continue;

            bool shouldBeEnabled = wantedHighlights.Contains(entry.highlight);

            if (highlightStates.TryGetValue(entry.highlight, out bool currentState))
            {
                if (currentState == shouldBeEnabled)
                    continue;
            }

            if (shouldBeEnabled)
                entry.highlight.EnableHighlight();
            else
                entry.highlight.DisableHighlight();

            highlightStates[entry.highlight] = shouldBeEnabled;
        }
    }

    public void DisableAllHighlights()
    {
        foreach (HighlightEntry entry in highlights)
        {
            if (entry == null || entry.highlight == null)
                continue;

            entry.highlight.DisableHighlight();
            highlightStates[entry.highlight] = false;
        }
    }
}