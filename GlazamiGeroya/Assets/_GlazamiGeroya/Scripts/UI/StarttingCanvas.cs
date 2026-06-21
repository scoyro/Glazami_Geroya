using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))] 
public class StartScreenController : MonoBehaviour
{
    [Header("UI References (Synchronized Fade)")]
    [Tooltip("Ссылка на РОДИТЕЛЬСКИЙ CanvasGroup, содержащий и текст, и управление")]
    [SerializeField] private CanvasGroup _rootCanvasGroup; 
    [SerializeField] private TextMeshProUGUI _promptText;

    [Header("Text Fade Settings (Fade In Only)")]
    [Tooltip("CanvasGroup, висящий непосредственно НА ОБЪЕКТЕ с текстом")]
    [SerializeField] private CanvasGroup _textCanvasGroup; 
    [Tooltip("Длительность плавного ПОЯВЛЕНИЯ текста (в секундах)")]
    [SerializeField] private float _textFadeInDuration = 1.0f;

    [Header("General Timing Settings")]
    [Tooltip("Сколько секунд ждать перед НАЧАЛОМ появления текста")]
    [SerializeField] private float _delayBeforePrompt = 3f;
    [Tooltip("Длительность плавного ИСЧЕЗНОВЕНИЯ всего экрана (UI + текст) и появления звука")]
    [SerializeField] private float _fadeDuration = 1.5f;
    
    [Header("Audio Settings")]
    [Tooltip("Целевая громкость после завершения фэйда (от 0 до 1)")]
    [SerializeField, Range(0f, 1f)] private float _targetMasterVolume = 1f;

    private bool _isWaitingForInput = false;
    private bool _isFadingOut = false;

    private void Awake()
    {
        // Убедимся, что ссылка на родительский холст есть
        if (_rootCanvasGroup == null)
            _rootCanvasGroup = GetComponent<CanvasGroup>();
        
        if (_textCanvasGroup == null && _promptText != null)
        {
            _textCanvasGroup = _promptText.GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // 1. Инициализация стартового состояния ГЛАВНОГО родителя (все видно)
        _rootCanvasGroup.alpha = 1f; 
        _rootCanvasGroup.interactable = true;
        _rootCanvasGroup.blocksRaycasts = true;
        
        // 2. Инициализация ТЕКСТА
        // Текст активен, но полностью прозрачен (Alpha 0)
        _promptText.gameObject.SetActive(true); 
        if (_textCanvasGroup != null)
        {
            _textCanvasGroup.alpha = 0f; 
        }
        
        // 3. Полное подавление звука в сцене
        AudioListener.volume = 0f;

        // 4. Запуск логики ожидания
        StartCoroutine(SequenceRoutine());
    }

    private void Update()
    {
        // Если текст полностью появился, ждем любое нажатие и запускаем синхронный фэйд
        if (_isWaitingForInput && !_isFadingOut && Input.anyKeyDown)
        {
            // Мы больше не отключаем текст мгновенно. Мы анимируем родителя.
            StartCoroutine(FadeOutSynchronousRoutine());
        }
    }

    private IEnumerator SequenceRoutine()
    {
        // А) Ждем начальную задержку
        yield return new WaitForSecondsRealtime(_delayBeforePrompt);

        // Б) Плавное появление ТОЛЬКО текста (Fade In)
        if (_textCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < _textFadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                _textCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / _textFadeInDuration);
                yield return null;
            }
            _textCanvasGroup.alpha = 1f; 
        }

        // В) Разрешаем ввод
        _isWaitingForInput = true;
    }

    private IEnumerator FadeOutSynchronousRoutine()
    {
        _isFadingOut = true;
        float elapsedTime = 0f;

        // Анимируем родительский CanvasGroup, чтобы он исчезал.
        // Поскольку текст и иконки являются его детьми, они исчезают вместе с ним.
        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / _fadeDuration);

            // Анимируем прозрачность ГЛАВНОГО CanvasGroup от 1 до 0
            _rootCanvasGroup.alpha = Mathf.Lerp(1f, 0f, normalizedTime);

            // Анимируем возвращение звука от 0 до целевого значения
            AudioListener.volume = Mathf.Lerp(0f, _targetMasterVolume, normalizedTime);

            yield return null;
        }

        // Гарантируем конечные значения
        _rootCanvasGroup.alpha = 0f;
        AudioListener.volume = _targetMasterVolume;

        // Отключаем родительский объект, чтобы он не потреблял ресурсы
        gameObject.SetActive(false);
    }
}