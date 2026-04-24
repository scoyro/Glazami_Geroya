using UnityEngine;

public class TakeJournal : MonoBehaviour
{
    [SerializeField] private GameObject text;
    [SerializeField] private GameObject me;
    void Start()
    {
        text.SetActive(false);
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            text.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                Destroy(me);
            }
        }
    }
}
