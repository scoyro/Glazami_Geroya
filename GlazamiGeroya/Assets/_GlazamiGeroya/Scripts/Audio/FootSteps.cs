using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource footstepSource;

    [Header("Clips In Order")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Movement Detection")]
    [SerializeField] private float minInputAmount = 0.1f;
    [SerializeField] private float minRealSpeed = 0.05f;

    [Header("Step Timing")]
    [SerializeField] private float walkStepInterval = 0.48f;
    [SerializeField] private float sprintStepInterval = 0.34f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField] private bool sprintOnlyInCrisis = true;

    private CharacterController controller;
    private Vector3 lastPosition;
    private float stepTimer;
    private bool isCrisis;
    private int currentClipIndex;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        if (footstepSource != null)
        {
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.pitch = 1f;
            footstepSource.spatialBlend = 1f;
            footstepSource.dopplerLevel = 0f;
        }

        lastPosition = transform.position;
        stepTimer = walkStepInterval;
        currentClipIndex = 0;
    }

    private void OnEnable()
    {
        TrySubscribe();
        SyncInitialState();
    }

    private void Start()
    {
        TrySubscribe();
        SyncInitialState();
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }

    private void Update()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        bool shouldPlaySteps = ShouldPlayFootsteps();

        if (!shouldPlaySteps)
        {
            stepTimer = Mathf.Min(stepTimer, GetStepInterval());
            lastPosition = transform.position;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayNextFootstep();
            stepTimer = GetStepInterval();
        }

        lastPosition = transform.position;
    }

    private bool ShouldPlayFootsteps()
    {
        if (!controller.isGrounded)
            return false;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        bool hasMoveInput = new Vector2(inputX, inputZ).sqrMagnitude >= minInputAmount * minInputAmount;

        if (!hasMoveInput)
            return false;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        float realSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        return realSpeed >= minRealSpeed;
    }

    private float GetStepInterval()
    {
        bool sprinting;

        if (sprintOnlyInCrisis)
            sprinting = isCrisis && Input.GetKey(sprintKey);
        else
            sprinting = Input.GetKey(sprintKey);

        return sprinting ? sprintStepInterval : walkStepInterval;
    }

    private void PlayNextFootstep()
    {
        AudioClip clip = footstepClips[currentClipIndex];

        AdvanceClipIndex();

        if (clip == null)
            return;

        footstepSource.pitch = 1f;
        footstepSource.PlayOneShot(clip, volume);
    }

    private void AdvanceClipIndex()
    {
        currentClipIndex++;

        if (currentClipIndex >= footstepClips.Length)
            currentClipIndex = 0;
    }

    private void TrySubscribe()
    {
        if (GameManager.Instance?.EventManager == null)
            return;

        GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
        GameManager.Instance.EventManager.OnCrisisModeChanged += SetCrisisMode;
    }

    private void SyncInitialState()
    {
        if (GameManager.Instance?.GameStateManager == null)
            return;

        isCrisis = GameManager.Instance.GameStateManager.IsCrisisMode;
    }

    private void SetCrisisMode(bool enabled)
    {
        isCrisis = enabled;
    }
}