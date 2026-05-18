using UnityEngine;

public class PanelClickable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelProcedureController procedureController;
    [SerializeField] private PanelHoverText hoverText;

    [Header("Action")]
    [SerializeField] private PanelActionType actionType;

    private bool isHovering;

    private void OnMouseEnter()
    {
        isHovering = true;
        ShowHoverText();
    }

    private void OnMouseOver()
    {
        if (!isHovering)
            return;

        ShowHoverText();
    }

    private void OnMouseExit()
    {
        isHovering = false;

        if (hoverText != null)
            hoverText.Hide();
    }

    private void OnMouseDown()
    {
        if (procedureController == null)
            return;

        switch (actionType)
        {
            case PanelActionType.VentilationLever:
                procedureController.TryPullVentilationLever();
                break;

            case PanelActionType.SealButton:
                procedureController.TryPressSealButton();
                break;
        }

        ShowHoverText();
    }

    private void OnDisable()
    {
        isHovering = false;

        if (hoverText != null)
            hoverText.Hide();
    }

    private void ShowHoverText()
    {
        if (hoverText == null || procedureController == null)
            return;

        string text = procedureController.GetHoverText(actionType);
        hoverText.Show(text);
    }
}