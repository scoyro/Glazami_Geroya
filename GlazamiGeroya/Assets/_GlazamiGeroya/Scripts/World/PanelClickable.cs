using UnityEngine;

public class PanelClickable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelProcedureController procedureController;

    [Header("Action")]
    [SerializeField] private PanelActionType actionType;

    public string GetHoverText()
    {
        if (procedureController == null)
            return string.Empty;

        return procedureController.GetHoverText(actionType);
    }

    public void Click()
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
    }
}