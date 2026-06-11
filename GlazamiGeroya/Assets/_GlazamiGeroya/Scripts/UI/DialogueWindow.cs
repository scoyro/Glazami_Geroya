using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSubtitleUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject avatarRoot;

    [Header("Typing")]
    [SerializeField] private bool useTypingEffect = true;
    [SerializeField] private float charactersPerSecond = 35f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Hide();
    }

    public void Show(string speaker, string text, float duration)
    {
        Show(speaker, text, null, duration);
    }

    public void Show(string speaker, string text, Sprite avatar, float duration)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(speaker, text, avatar, duration));
    }

    public void ShowInstant(string speaker, string text, Sprite avatar = null)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (speakerText != null)
            speakerText.text = speaker;

        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        SetAvatar(avatar);
    }

    public void Hide()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (speakerText != null)
            speakerText.text = string.Empty;

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        SetAvatar(null);
    }

    private IEnumerator ShowRoutine(string speaker, string text, Sprite avatar, float duration)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (speakerText != null)
            speakerText.text = speaker;

        SetAvatar(avatar);

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (useTypingEffect)
        {
            yield return TypeText(text);
        }
        else
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        Hide();
        currentRoutine = null;
    }

    private IEnumerator TypeText(string text)
    {
        if (dialogueText == null)
            yield break;

        if (string.IsNullOrEmpty(text))
            yield break;

        float delay = charactersPerSecond > 0f
            ? 1f / charactersPerSecond
            : 0f;

        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        int totalCharacters = text.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void SetAvatar(Sprite avatar)
    {
        if (avatarImage == null)
            return;

        bool hasAvatar = avatar != null;

        avatarImage.sprite = avatar;
        avatarImage.enabled = hasAvatar;

        if (avatarRoot != null)
            avatarRoot.SetActive(hasAvatar);
    }
}