using UnityEngine;

public class ColliderDebug : MonoBehaviour
{
    private Collider col;

    private void Awake() => col = GetComponent<Collider>();

    private void FixedUpdate()
    {
        Debug.Log($"[FixedUpdate] frame={Time.frameCount} active={gameObject.activeInHierarchy} col.enabled={col.enabled}");
    }

    private void Update()
    {
        Debug.Log($"[Update] frame={Time.frameCount} active={gameObject.activeInHierarchy} col.enabled={col.enabled}");

        // Do our own SphereCast from world origin toward the object
        Vector3 dir = (transform.position - Camera.main.transform.position).normalized;
        RaycastHit[] hits = Physics.SphereCastAll(
            Camera.main.transform.position, 0.5f, dir, 10f,
            ~0, QueryTriggerInteraction.Collide
        );

        bool found = false;
        foreach (var h in hits)
            if (h.collider.gameObject == gameObject)
                found = true;

        Debug.Log($"[Update] frame={Time.frameCount} SphereCast found this object: {found}");
    }
}