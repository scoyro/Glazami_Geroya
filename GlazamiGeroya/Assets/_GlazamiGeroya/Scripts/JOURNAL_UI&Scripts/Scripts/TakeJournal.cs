using UnityEngine;

public class TakeJournal : MonoBehaviour
{
    [SerializeField] private GameObject text;
    [SerializeField] private GameObject me;

    [Header("Task")]
    [SerializeField] private ChecklistManager checklistManager;
    [SerializeField] private string taskId = "take_journal";

    private bool playerInRange;

    private void Start()
    {
        if (text != null)
            text.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            Take();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (text != null)
            text.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (text != null)
            text.SetActive(false);
    }

    private void Take()
    {
        if (checklistManager != null)
            checklistManager.CompleteTask(taskId);

        if (text != null)
            text.SetActive(false);

        if (me != null)
            Destroy(me);
        else
            Destroy(gameObject);
    }
}