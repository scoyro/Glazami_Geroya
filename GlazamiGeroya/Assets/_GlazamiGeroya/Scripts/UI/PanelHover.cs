using TMPro;
using UnityEngine;

public class PanelHoverText : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform hoverRoot;
    [SerializeField] private TMP_Text hoverText;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(18f, -18f);

    private bool isVisible;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!isVisible || hoverRoot == null)
            return;

        hoverRoot.position = (Vector2)Input.mousePosition + screenOffset;
    }

    public void Show(string text)
    {
        if (hoverRoot == null || hoverText == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            Hide();
            return;
        }

        hoverText.text = text;
        hoverRoot.gameObject.SetActive(true);
        isVisible = true;

        hoverRoot.position = (Vector2)Input.mousePosition + screenOffset;
    }

    public void Hide()
    {
        isVisible = false;

        if (hoverRoot != null)
            hoverRoot.gameObject.SetActive(false);
    }
}