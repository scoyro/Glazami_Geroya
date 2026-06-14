using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskJournalOverlay : MonoBehaviour
{
    [Serializable]
    private class TaskSketchEntry
    {
        public string taskId;
        public Sprite sketch;
    }

    [Header("Input")]
    [SerializeField] private KeyCode journalKey = KeyCode.Tab;
    [SerializeField] private bool holdToShow = true;

    [Header("UI")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private CanvasGroup dimCanvasGroup;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TMP_Text taskListText;
    [SerializeField] private Image sketchImage;

    [Header("Sketches")]
    [SerializeField] private Sprite defaultSketch;
    [SerializeField] private List<TaskSketchEntry> taskSketches = new List<TaskSketchEntry>();

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float dimTargetAlpha = 0.55f;

    [Header("Text")]
    [SerializeField] private string title = "Список задач";
    [SerializeField] private Color activeTaskColor = Color.white;
    [SerializeField] private Color completedTaskColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color lockedTaskColor = new Color(0.35f, 0.35f, 0.35f);

    private bool isOpen;
    private string currentActiveTaskId;

    private readonly Dictionary<string, Sprite> sketchMap = new Dictionary<string, Sprite>();

    private void Awake()
    {
        BuildSketchMap();
        SetupCanvasGroups();
        RefreshContent();
        HideInstant();
    }

    private void Update()
    {
        HandleInput();

        bool hasTasks = HasAnyVisibleTask();
        bool shouldShow = isOpen && hasTasks;

        UpdateVisibility(shouldShow);

        if (shouldShow)
            RefreshContent();
    }

    private void HandleInput()
    {
        if (holdToShow)
        {
            isOpen = Input.GetKey(journalKey);
            return;
        }

        if (Input.GetKeyDown(journalKey))
            isOpen = !isOpen;
    }

    private void UpdateVisibility(bool shouldShow)
    {
        if (overlayRoot != null && shouldShow && !overlayRoot.activeSelf)
            overlayRoot.SetActive(true);

        float targetPanelAlpha = shouldShow ? 1f : 0f;
        float targetDimAlpha = shouldShow ? dimTargetAlpha : 0f;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = Mathf.MoveTowards(
                panelCanvasGroup.alpha,
                targetPanelAlpha,
                Time.deltaTime * fadeSpeed
            );

            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup.alpha = Mathf.MoveTowards(
                dimCanvasGroup.alpha,
                targetDimAlpha,
                Time.deltaTime * fadeSpeed
            );

            dimCanvasGroup.interactable = false;
            dimCanvasGroup.blocksRaycasts = false;
        }

        bool fullyHidden =
            panelCanvasGroup != null &&
            dimCanvasGroup != null &&
            panelCanvasGroup.alpha <= 0.001f &&
            dimCanvasGroup.alpha <= 0.001f;

        if (!shouldShow && fullyHidden && overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void RefreshContent()
    {
        ChecklistManager checklist = GameManager.Instance != null
            ? GameManager.Instance.ChecklistManager
            : null;

        if (checklist == null || taskListText == null)
            return;

        var visibleTasks = checklist.Tasks.Values
            .Where(task => task != null && task.isVisible)
            .ToList();

        ChecklistTask activeTask = visibleTasks.FirstOrDefault(task => !task.isCompleted);
        currentActiveTaskId = activeTask != null ? activeTask.id : string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"<b>{title}</b>");
        sb.AppendLine();

        foreach (ChecklistTask task in visibleTasks)
        {
            if (task.isCompleted)
            {
                string color = ColorUtility.ToHtmlStringRGB(completedTaskColor);
                sb.AppendLine($"<color=#{color}><s>{task.title}</s></color>");
            }
            else if (task == activeTask)
            {
                string color = ColorUtility.ToHtmlStringRGB(activeTaskColor);
                sb.AppendLine($"<color=#{color}>{task.title}</color>");
            }
            else
            {
                string color = ColorUtility.ToHtmlStringRGB(lockedTaskColor);
                sb.AppendLine($"<color=#{color}>{task.title}</color>");
            }
        }

        taskListText.text = sb.ToString();

        UpdateSketch(activeTask);
    }

    private void UpdateSketch(ChecklistTask activeTask)
    {
        if (sketchImage == null)
            return;

        Sprite sprite = defaultSketch;

        if (activeTask != null &&
            !string.IsNullOrWhiteSpace(activeTask.id) &&
            sketchMap.TryGetValue(activeTask.id, out Sprite taskSketch) &&
            taskSketch != null)
        {
            sprite = taskSketch;
        }

        sketchImage.sprite = sprite;
        sketchImage.enabled = sprite != null;
        sketchImage.preserveAspect = true;
    }

    private bool HasAnyVisibleTask()
    {
        ChecklistManager checklist = GameManager.Instance != null
            ? GameManager.Instance.ChecklistManager
            : null;

        if (checklist == null)
            return false;

        return checklist.Tasks.Values.Any(task => task != null && task.isVisible);
    }

    private void BuildSketchMap()
    {
        sketchMap.Clear();

        foreach (TaskSketchEntry entry in taskSketches)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.taskId))
                continue;

            if (entry.sketch == null)
                continue;

            sketchMap[entry.taskId] = entry.sketch;
        }
    }

    private void SetupCanvasGroups()
    {
        if (overlayRoot == null)
            overlayRoot = gameObject;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (dimCanvasGroup != null)
        {
            dimCanvasGroup.alpha = 0f;
            dimCanvasGroup.interactable = false;
            dimCanvasGroup.blocksRaycasts = false;
        }
    }

    private void HideInstant()
    {
        isOpen = false;

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        if (dimCanvasGroup != null)
            dimCanvasGroup.alpha = 0f;

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }
}