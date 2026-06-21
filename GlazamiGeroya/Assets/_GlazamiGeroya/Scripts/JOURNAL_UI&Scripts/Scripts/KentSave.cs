using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DelayedEventSequence : MonoBehaviour
{
    [Header("Первый этап")]
    [Tooltip("Задержка перед активацией первого события (в секундах)")]
    [SerializeField] private float _initialDelay = 1.0f;
    [Tooltip("Событие, которое сработает после первой задержки")]
    [SerializeField] private UnityEvent _onFirstEvent;

    [Header("Второй этап")]
    [Tooltip("Задержка перед активацией второго события (отсчет начинается после срабатывания первого)")]
    [SerializeField] private float _secondDelay = 1.0f;
    [Tooltip("Событие, которое сработает после второй задержки")]
    [SerializeField] private UnityEvent _onSecondEvent;

    private Coroutine _currentSequence;

    /// <summary>
    /// Этот метод нужно выбрать в поле UnityEvent вашего InteractionTarget
    /// </summary>
    public void StartSequence()
    {
        // Защита от двойного запуска: если последовательность уже идет, мы ее перезапускаем
        // (если вам нужно, чтобы они могли наслаиваться друг на друга, удалите следующие две строки)
        if (_currentSequence != null)
            StopCoroutine(_currentSequence);

        _currentSequence = StartCoroutine(SequenceRoutine());
    }

    /// <summary>
    /// Метод для принудительной остановки последовательности (если понадобится)
    /// </summary>
    public void StopSequence()
    {
        if (_currentSequence != null)
        {
            StopCoroutine(_currentSequence);
            _currentSequence = null;
        }
    }

    private IEnumerator SequenceRoutine()
    {
        // 1. Ожидание первой задержки
        if (_initialDelay > 0f)
        {
            yield return new WaitForSeconds(_initialDelay);
        }

        // 2. Вызов первого события
        _onFirstEvent?.Invoke();

        // 3. Ожидание второй задержки
        if (_secondDelay > 0f)
        {
            yield return new WaitForSeconds(_secondDelay);
        }

        // 4. Вызов второго события
        _onSecondEvent?.Invoke();

        // Очищаем ссылку по завершении
        _currentSequence = null;
    }
}