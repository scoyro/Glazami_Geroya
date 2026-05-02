using UnityEngine;

public class LadderInteractionState : MonoBehaviour
{
    [SerializeField] private GameObject topInteractPoint;
    [SerializeField] private GameObject bottomInteractPoint;

    private void Start()
    {
        SetNoneActive();
    }

    public void EnableTopInteraction()
    {
        if (topInteractPoint != null)
            topInteractPoint.SetActive(true);

        if (bottomInteractPoint != null)
            bottomInteractPoint.SetActive(false);
    }

    public void EnableBottomInteraction()
    {
        if (topInteractPoint != null)
            topInteractPoint.SetActive(false);

        if (bottomInteractPoint != null)
            bottomInteractPoint.SetActive(true);
    }

    public void SetNoneActive()
    {
        if (topInteractPoint != null)
            topInteractPoint.SetActive(false);

        if (bottomInteractPoint != null)
            bottomInteractPoint.SetActive(false);
    }
}