using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class IofDoorController : MonoBehaviour
{
    [Header("Room Check")]
    [SerializeField] private IofRoomPresence roomPresence;
    [SerializeField] private bool requirePlayerInsideRoomToClose = true;

    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Transform openRotationPoint;
    [SerializeField] private Transform closedRotationPoint;
    [SerializeField] private float rotateDuration = 1.2f;
    [SerializeField] private Collider doorBlockCollider;

    [Header("Interaction")]
    [SerializeField] private InteractionTarget interactionTarget;
    [SerializeField] private string closePrompt = "Закрыть дверь";
    [SerializeField] private string openPrompt = "Открыть дверь";
    [SerializeField] private string lockedPrompt = "Дверь заблокирована";

    [Header("Seal Lock")]
    [SerializeField] private bool lockAfterClosing = true;

    [TextArea]
    [SerializeField] private string lockedThought = "Дверь герметизирована. Сейчас её нельзя открыть.";

    [TextArea]
    [SerializeField] private string outsideRoomThought = "Закрывать дверь нужно изнутри помещения ИОФ.";

    [TextArea]
    [SerializeField] private string closedThought = "Дверь закрыта. Помещение герметизировано.";

    [TextArea]
    [SerializeField] private string openedThought = "Дверь открыта.";

    [Header("Audio")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip lockClip;
    [SerializeField] private AudioClip lockedAttemptClip;

    [Header("Events")]
    [SerializeField] private UnityEvent onDoorOpeningStarted;
    [SerializeField] private UnityEvent onDoorOpened;
    [SerializeField] private UnityEvent onDoorClosingStarted;
    [SerializeField] private UnityEvent onDoorClosed;
    [SerializeField] private UnityEvent onDoorUnlocked;

    private bool isClosed;
    private bool isMoving;
    private bool isSealedLocked;
    public bool IsClosed => isClosed;
    public bool IsSealedLocked => isSealedLocked;
    public void SealLockDoor()
    {
        if (!isClosed)
            return;

        isSealedLocked = true;

        if (doorBlockCollider != null)
            doorBlockCollider.enabled = true;

        RefreshPrompt();
    }
    

    private void Start()
    {
        RefreshPrompt();
    }

    public void ToggleDoor()
    {
        if (isMoving)
            return;

        if (isClosed)
        {
            if (isSealedLocked)
            {
                HandleLockedAttempt();
                return;
            }

            OpenDoor();
        }
        else
        {
            TryCloseDoor();
        }
    }

    public void TryCloseDoor()
    {
        if (isClosed || isMoving)
            return;

        if (requirePlayerInsideRoomToClose && (roomPresence == null || !roomPresence.PlayerInside))
        {
            GameManager.Instance?.EventManager?.RequestThought(outsideRoomThought, 4f);
            return;
        }

        StartCoroutine(RotateDoorRoutine(
            closedRotationPoint.rotation,
            closeClip,
            onDoorClosingStarted,
            onDoorClosed,
            true
        ));
    }

    public void OpenDoor()
    {
        if (!isClosed || isMoving)
            return;

        if (isSealedLocked)
        {
            HandleLockedAttempt();
            return;
        }

        StartCoroutine(RotateDoorRoutine(
            openRotationPoint.rotation,
            openClip,
            onDoorOpeningStarted,
            onDoorOpened,
            false
        ));
    }

    public void UnlockDoor()
    {
        isSealedLocked = false;

        RefreshPrompt();

        onDoorUnlocked?.Invoke();
    }

    private void HandleLockedAttempt()
    {
        if (lockedAttemptClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(lockedAttemptClip, doorTransform.position);

        if (!string.IsNullOrWhiteSpace(lockedThought))
            GameManager.Instance?.EventManager?.RequestThought(lockedThought, 4f);

        RefreshPrompt();
    }

    private IEnumerator RotateDoorRoutine(
        Quaternion targetRotation,
        AudioClip moveClip,
        UnityEvent onStarted,
        UnityEvent onFinished,
        bool closing)
    {
        if (doorTransform == null)
            yield break;

        isMoving = true;
        RefreshPrompt();

        onStarted?.Invoke();

        if (moveClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(moveClip, doorTransform.position);

        Quaternion startRotation = doorTransform.rotation;

        float time = 0f;

        while (time < rotateDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / rotateDuration);
            t = t * t * (3f - 2f * t);

            doorTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        doorTransform.rotation = targetRotation;

        isClosed = closing;

        if (closing && lockAfterClosing)
            isSealedLocked = true;

        if (doorBlockCollider != null)
            doorBlockCollider.enabled = isClosed;

        if (closing && lockClip != null && AudioManager.Instance != null)
            AudioManager.Instance.Play3D(lockClip, doorTransform.position);

        if (closing && !string.IsNullOrWhiteSpace(closedThought))
            GameManager.Instance?.EventManager?.RequestThought(closedThought, 4f);

        if (!closing && !string.IsNullOrWhiteSpace(openedThought))
            GameManager.Instance?.EventManager?.RequestThought(openedThought, 4f);

        isMoving = false;

        RefreshPrompt();

        onFinished?.Invoke();
    }

    private void RefreshPrompt()
    {
        if (interactionTarget == null)
            return;

        if (isMoving)
        {
            interactionTarget.SetPromptText("");
            return;
        }

        if (isClosed && isSealedLocked)
        {
            interactionTarget.SetPromptText(lockedPrompt);
            return;
        }

        if (isClosed)
        {
            interactionTarget.SetPromptText(openPrompt);
            return;
        }

        interactionTarget.SetPromptText(closePrompt);
    }
}