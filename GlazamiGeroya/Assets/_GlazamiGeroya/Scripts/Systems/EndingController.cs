using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private GameObject buttonsRoot;

    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Data")]
    [SerializeField] private List<EndingData> endings = new List<EndingData>();

    [Header("Typing")]
    [SerializeField] private bool typeDescription = true;
    [SerializeField] private float charactersPerSecond = 35f;
    [SerializeField] private bool allowSkipTyping = true;
    [SerializeField] private KeyCode skipTypingKey = KeyCode.Space;

    [Header("Player Lock")]
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Audio")]
    [SerializeField] private AudioSource endingAudioSource;
    [SerializeField] private bool stopAllAudioSourcesOnEnding = false;
    [SerializeField] private AudioSource[] audioSourcesToStop;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Fallback Ending")]
    [SerializeField] private string fallbackTitle = "Трагический исход";

    [TextArea(4, 10)]
    [SerializeField] private string fallbackDescription =
        "Добавьте текст концовки и историческую справку в инспекторе.";

    [Header("Behaviour")]
    [SerializeField] private bool forceUnpauseDuringEnding = true;
    [SerializeField] private bool pauseGameAfterText = true;

    private readonly Dictionary<string, EndingData> endingMap = new Dictionary<string, EndingData>();

    private GameManager gameManager;
    private Coroutine endingRoutine;

    private bool isPlaying;
    private bool skipRequested;

    public bool IsPlaying => isPlaying;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        BuildEndingMap();
        HideEndingPanel();
    }

    private void Awake()
    {
        BuildEndingMap();
        HideEndingPanel();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (!allowSkipTyping)
            return;

        if (Input.GetKeyDown(skipTypingKey) || Input.GetMouseButtonDown(0))
            skipRequested = true;
    }

    public void PlayEnding(string endingId)
    {
        if (isPlaying)
            return;

        EndingData ending = GetEnding(endingId);

        if (endingRoutine != null)
            StopCoroutine(endingRoutine);

        endingRoutine = StartCoroutine(PlayEndingRoutine(ending));
    }

    // Оставлено для совместимости со старым кодом.
    // Если где-то уже вызывается PlayEnding(GameResult, string), проект не сломается.
    public void PlayEnding(GameResult result, string endingId)
    {
        PlayEnding(endingId);
    }

    private IEnumerator PlayEndingRoutine(EndingData ending)
    {
        isPlaying = true;
        skipRequested = false;

        if (forceUnpauseDuringEnding)
            Time.timeScale = 1f;

        LockPlayer();
        StopRequestedAudio();

        if (buttonsRoot != null)
            buttonsRoot.SetActive(false);

        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.maxVisibleCharacters = int.MaxValue;
        }

        if (ending.fadeOutBeforeShow && ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOut(null, ending.fadeOutDuration);

            while (ScreenFader.Instance.IsBusy)
                yield return null;
        }

        if (ending.delayBeforePanel > 0f)
            yield return new WaitForSecondsRealtime(ending.delayBeforePanel);

        ShowPanelBase(ending);

        PlayEndingAudio(ending);

        if (descriptionText != null)
        {
            if (typeDescription)
                yield return TypeDescription(ending.description);
            else
                ShowDescriptionInstant(ending.description);
        }

        if (buttonsRoot != null)
            buttonsRoot.SetActive(true);

        if (pauseGameAfterText && ending.pauseGameAfterShow)
            Time.timeScale = 0f;

        endingRoutine = null;
    }

    private void ShowPanelBase(EndingData ending)
    {
        if (endingPanel != null)
            endingPanel.SetActive(true);

        if (resultText != null)
            resultText.text = GetResultLabel(ending.result);

        if (titleText != null)
            titleText.text = ending.title;

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.maxVisibleCharacters = int.MaxValue;
        }
    }

    private IEnumerator TypeDescription(string text)
    {
        if (descriptionText == null)
            yield break;

        if (string.IsNullOrEmpty(text))
        {
            descriptionText.text = string.Empty;
            yield break;
        }

        descriptionText.text = text;
        descriptionText.maxVisibleCharacters = 0;

        float delay = charactersPerSecond > 0f
            ? 1f / charactersPerSecond
            : 0f;

        int totalCharacters = text.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            if (skipRequested)
            {
                descriptionText.maxVisibleCharacters = int.MaxValue;
                yield break;
            }

            descriptionText.maxVisibleCharacters = i;

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return null;
        }

        descriptionText.maxVisibleCharacters = int.MaxValue;
    }

    private void ShowDescriptionInstant(string text)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = text;
        descriptionText.maxVisibleCharacters = int.MaxValue;
    }

    private EndingData GetEnding(string endingId)
    {
        if (!string.IsNullOrWhiteSpace(endingId) &&
            endingMap.TryGetValue(endingId, out EndingData ending))
        {
            return ending;
        }

        return new EndingData
        {
            id = "fallback",
            result = EndingResult.Defeat,
            title = fallbackTitle,
            description = fallbackDescription,
            fadeOutBeforeShow = true,
            fadeOutDuration = 0.6f,
            delayBeforePanel = 0.3f,
            pauseGameAfterShow = true
        };
    }

    private void BuildEndingMap()
    {
        endingMap.Clear();

        foreach (EndingData ending in endings)
        {
            if (ending == null)
                continue;

            if (string.IsNullOrWhiteSpace(ending.id))
                continue;

            endingMap[ending.id] = ending;
        }
    }

    private string GetResultLabel(EndingResult result)
    {
        switch (result)
        {
            case EndingResult.Victory:
                return "ИСТОРИЧЕСКИЙ ИСХОД";

            case EndingResult.Defeat:
                return "ПЛОХАЯ КОНЦОВКА";

            case EndingResult.Neutral:
                return "КОНЦОВКА";

            default:
                return string.Empty;
        }
    }

    private void PlayEndingAudio(EndingData ending)
    {
        if (ending.endingClip == null)
            return;

        if (endingAudioSource != null)
        {
            endingAudioSource.Stop();
            endingAudioSource.clip = ending.endingClip;
            endingAudioSource.loop = false;
            endingAudioSource.Play();
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play2D(ending.endingClip);
    }

    private void StopRequestedAudio()
    {
        if (stopAllAudioSourcesOnEnding)
        {
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

            foreach (AudioSource source in allSources)
            {
                if (source != null)
                    source.Stop();
            }

            return;
        }

        if (audioSourcesToStop == null)
            return;

        foreach (AudioSource source in audioSourcesToStop)
        {
            if (source != null)
                source.Stop();
        }
    }

    private void LockPlayer()
    {
        playerControlLock?.LockControls();
        interactionSystem?.LockInteraction();
    }

    private void HideEndingPanel()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (buttonsRoot != null)
            buttonsRoot.SetActive(false);

        if (resultText != null)
            resultText.text = string.Empty;

        if (titleText != null)
            titleText.text = string.Empty;

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.maxVisibleCharacters = int.MaxValue;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (gameManager != null && gameManager.SceneController != null)
        {
            gameManager.SceneController.ReloadCurrentScene();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (gameManager != null && gameManager.SceneController != null)
        {
            gameManager.SceneController.LoadScene(menuSceneName);
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}

public enum EndingResult
{
    Victory,
    Defeat,
    Neutral
}

[System.Serializable]
public class EndingData
{
    public string id;
    public EndingResult result = EndingResult.Defeat;

    public string title;

    [TextArea(5, 14)]
    public string description;

    [Header("Audio")]
    public AudioClip endingClip;

    [Header("Timing")]
    public bool fadeOutBeforeShow = true;
    public float fadeOutDuration = 0.6f;
    public float delayBeforePanel = 0.4f;

    [Header("Pause")]
    public bool pauseGameAfterShow = true;
}