using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Runtime.InteropServices;

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
    [SerializeField]
    private string fallbackDescription =
        "Добавьте текст концовки и историческую справку в инспекторе.";

    [Header("Behaviour")]
    [SerializeField] private bool forceUnpauseDuringEnding = true;
    [SerializeField] private bool pauseGameAfterText = true;
    [Header("Cursor")]
    [SerializeField] private bool unlockCursorDuringEnding = true;
    [SerializeField] private bool keepCursorUnlockedWhileEnding = true;

    [Header("Description Scroll")]
    [SerializeField] private ScrollRect descriptionScrollRect;
    [SerializeField] private bool autoScrollDescription = true;
    [SerializeField] private float autoScrollSpeed = 4f;
    [SerializeField] private bool snapScrollToTopOnStart = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GG_ReturnToSite(string url);
#endif
    private readonly Dictionary<string, EndingData> endingMap = new Dictionary<string, EndingData>();

    private GameManager gameManager;
    private Coroutine endingRoutine;

    private bool isPlaying;
    private bool skipRequested;
    private bool isTypingDescription;

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
        if (isTypingDescription)
            AutoScrollDescription();

        if (!isPlaying)
            return;

        if (!allowSkipTyping)
            return;

        if (Input.GetKeyDown(skipTypingKey) || Input.GetMouseButtonDown(0))
            skipRequested = true;
    }
    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        if (!keepCursorUnlockedWhileEnding)
            return;

        if (endingPanel != null && endingPanel.activeInHierarchy)
            UnlockCursorForEnding();
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
        isTypingDescription = false;

        if (forceUnpauseDuringEnding)
            Time.timeScale = 1f;

        LockPlayer();
        UnlockCursorForEnding();
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

        ResetDescriptionScroll();

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
        UnlockCursorForEnding();

        if (buttonsRoot != null)
            buttonsRoot.SetActive(true);

        if (pauseGameAfterText && ending.pauseGameAfterShow)
            Time.timeScale = 0f;

        endingRoutine = null;
    }
    private void UnlockCursorForEnding()
    {
        if (!unlockCursorDuringEnding)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void ShowPanelBase(EndingData ending)
    {
        if (endingPanel != null)
            endingPanel.SetActive(true);

        UnlockCursorForEnding();
        if (resultText != null)
            resultText.text = GetResultLabel(ending.result);

        if (titleText != null)
            titleText.text = ending.title;

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.maxVisibleCharacters = int.MaxValue;
        }

        ResetDescriptionScroll();
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

        // ОПТИМИЗАЦИЯ: Передаем текст целиком один раз
        descriptionText.text = text;
        descriptionText.maxVisibleCharacters = 0;

        ResetDescriptionScroll();
        isTypingDescription = true;

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        for (int i = 0; i <= text.Length; i++)
        {
            if (skipRequested)
            {
                // Если скипнули, показываем все символы и выходим из цикла
                descriptionText.maxVisibleCharacters = text.Length;
                break;
            }

            // Просто открываем следующую букву (без аллокаций памяти)
            descriptionText.maxVisibleCharacters = i;

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return null;
        }

        isTypingDescription = false;
        descriptionText.maxVisibleCharacters = int.MaxValue;
        ForceScrollToBottom();
    }

    private void ShowDescriptionInstant(string text)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = text;
        descriptionText.maxVisibleCharacters = int.MaxValue;

        ResetDescriptionScroll();
    }

    private void ResetDescriptionScroll()
    {
        if (descriptionScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (snapScrollToTopOnStart)
            descriptionScrollRect.verticalNormalizedPosition = 1f;
    }

    private void AutoScrollDescription()
    {
        if (!autoScrollDescription)
            return;

        if (descriptionScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        descriptionScrollRect.verticalNormalizedPosition = Mathf.Lerp(
            descriptionScrollRect.verticalNormalizedPosition,
            0f,
            Time.unscaledDeltaTime * autoScrollSpeed
        );
    }

    private void ForceScrollToBottom()
    {
        if (!autoScrollDescription)
            return;

        if (descriptionScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        descriptionScrollRect.verticalNormalizedPosition = 0f;
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

        ResetDescriptionScroll();
    }

    public void RestartGame()
    {
        // Восстанавливаем время, если игра была поставлена на паузу (Time.timeScale = 0)
        // Это самая частая причина "неработающего" перезапуска
        Time.timeScale = 1f;

        // Получаем индекс текущей активной сцены и загружаем её заново
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

#if UNITY_WEBGL && !UNITY_EDITOR
    GG_ReturnToSite("../index.html?completed=aldar&study=video#episodes");
    return;
#endif

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