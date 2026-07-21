using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the first half of the experience: the orb's entrance, pause,
/// guided travel through the room, final stopping point, and proximity-based
/// handoff to A_OrbIntroSequence.
///
/// IMPORTANT HIERARCHY EXPECTATION:
/// - This component normally lives on Neb_orb_Pivot.
/// - orbPivot should reference Neb_orb_Pivot.
/// - spinVisual should reference Neb_orb_VisualPivot, not the doppelganger.
/// - orbSequence should reference the A_OrbIntroSequence component.
/// - playerHead should reference CenterEyeAnchor.
///
/// The path is locked once, using the participant's starting head position and
/// forward direction. After that, the orb remains in world space instead of
/// following the participant's head.
/// </summary>
public class OrbLureController : MonoBehaviour
{
    // Scene object links. Incorrect assignments here are the most common setup problem.
    [Header("References")]
    public Transform orbPivot;
    public Transform spinVisual;
    public Transform playerHead;
    public A_OrbIntroSequence orbSequence;
    public GameObject orbRootToShowHide;

    [Header("Opening Delay")]
    public float startDelaySeconds = 3f;

    // These values define the first reveal from behind the participant to upper-right.
    [Header("Locked World-Space Entrance")]
    public float behindMeters = 1.2f;
    public float rightMeters = 1.4f;
    public float rightForwardMeters = 2.8f;
    public float heightAboveHead = 1.2f;
    public float entranceDurationSeconds = 2.5f;
    public float rightHoldSeconds = 1.5f;

    [Header("Right-Side Travel")]
    public float rightTravelForwardMeters = 5f;
    public float rightTravelDurationSeconds = 10f;

    [Header("Centering Travel")]
    public float finalForwardMeters = 8f;
    public float finalRightMeters = 0f;
    public float finalHeightAboveStartHead = 0.5f;
    public float centerTravelDurationSeconds = 5f;

    [Header("Orb Motion")]
    public float idleSpinDegreesPerSecond = 8f;
    public Vector3 spinAxisLocal = new Vector3(0f, 1f, 0f);
    public float hoverAmplitude = 0.08f;
    public float hoverSpeed = 0.6f;

    // Safety correction used during all lure movement and while waiting at the end.
    [Header("Participant Spacing")]
    public float minimumSpacingMeters = 1.52f;
    public float spacingCorrectionSpeed = 2f;
    public float maximumCorrectionMetersPerSecond = 1.5f;

    // Either proximity or timeout can start the second script.
    [Header("Main Sequence Trigger")]
    public float requiredDistanceToStart = 1.8f;
    public float forceStartAfterSeconds = 120f;

    [Header("Debug")]
    public bool autoStart = true;

    private Coroutine lureCoroutine;
    private bool lureFinished;
    private bool mainSequenceStarted;
    private float stoppedTimer;

    private Vector3 lockedForward;
    private Vector3 lockedRight;
    private Vector3 startingHeadPosition;

    private Vector3 behindPoint;
    private Vector3 upperRightPoint;
    private Vector3 rightTravelEndPoint;
    private Vector3 finalCenterPoint;

    /// <summary>
    /// Finds the main camera automatically when Player Head was not assigned.
    /// Explicitly assigning CenterEyeAnchor in the Inspector is still safer.
    /// </summary>
    private void Awake()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;
    }

    /// <summary>
    /// Validates references, prevents the second script from auto-starting,
    /// hides the orb visual until the entrance begins, and optionally starts
    /// the lure automatically.
    /// </summary>
    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        orbSequence.autoStart = false;

        if (orbRootToShowHide != null)
            orbRootToShowHide.SetActive(false);

        if (autoStart)
            lureCoroutine = StartCoroutine(RunLure());
    }

    /// <summary>
    /// Runs only after the orb reaches its final position. The orb keeps
    /// spinning, preserves participant spacing, and waits for either the user
    /// to approach or the safety timeout to expire.
    /// </summary>
    private void Update()
    {
        if (lureFinished && !mainSequenceStarted)
        {
            SpinOrb(idleSpinDegreesPerSecond);
            MaintainMinimumSpacing();

            stoppedTimer += Time.deltaTime;

            float distance = Vector3.Distance(playerHead.position, orbPivot.position);
            bool participantArrived = distance <= requiredDistanceToStart;
            bool safetyTimeoutReached = stoppedTimer >= forceStartAfterSeconds;

            if (participantArrived || safetyTimeoutReached)
                StartMainSequence();
        }
    }

    /// <summary>
    /// Public restart entry point. This can be called by another script, event,
    /// timeline, or button. Any currently running lure coroutine is stopped.
    /// </summary>
    public void BeginLure()
    {
        if (lureCoroutine != null)
            StopCoroutine(lureCoroutine);

        lureCoroutine = StartCoroutine(RunLure());
    }

    /// <summary>
    /// Executes the entire entrance path in order: delay, behind-to-right
    /// entrance, right-side hold, long right-side travel, and centering travel.
    /// </summary>
    private IEnumerator RunLure()
    {
        lureFinished = false;
        mainSequenceStarted = false;
        stoppedTimer = 0f;

        if (startDelaySeconds > 0f)
            yield return new WaitForSeconds(startDelaySeconds);

        LockPathToStartingPose();
        BuildWorldSpacePath();

        orbPivot.position = behindPoint;

        if (orbRootToShowHide != null)
            orbRootToShowHide.SetActive(true);

        yield return MoveAlongSegment(behindPoint, upperRightPoint, entranceDurationSeconds);
        yield return HoldAtPoint(upperRightPoint, rightHoldSeconds);
        yield return MoveAlongSegment(upperRightPoint, rightTravelEndPoint, rightTravelDurationSeconds);
        yield return MoveAlongSegment(rightTravelEndPoint, finalCenterPoint, centerTravelDurationSeconds);

        orbPivot.position = finalCenterPoint;
        lureFinished = true;
        stoppedTimer = 0f;
    }

    /// <summary>
    /// Captures the participant's initial position and flattened facing
    /// direction. These vectors are intentionally not updated later, which
    /// keeps the animation fixed in world space.
    /// </summary>
    private void LockPathToStartingPose()
    {
        startingHeadPosition = playerHead.position;

        lockedForward = playerHead.forward;
        lockedForward.y = 0f;

        if (lockedForward.sqrMagnitude < 0.0001f)
            lockedForward = Vector3.forward;

        lockedForward.Normalize();
        lockedRight = Vector3.Cross(Vector3.up, lockedForward).normalized;
    }

    /// <summary>
    /// Converts the Inspector distances into four fixed world-space points.
    /// All movement fields in the Inspector ultimately affect these points.
    /// </summary>
    private void BuildWorldSpacePath()
    {
        behindPoint =
            startingHeadPosition
            - lockedForward * behindMeters
            + Vector3.up * heightAboveHead;

        upperRightPoint =
            startingHeadPosition
            + lockedForward * rightForwardMeters
            + lockedRight * rightMeters
            + Vector3.up * heightAboveHead;

        rightTravelEndPoint =
            startingHeadPosition
            + lockedForward * rightTravelForwardMeters
            + lockedRight * rightMeters
            + Vector3.up * heightAboveHead;

        finalCenterPoint =
            startingHeadPosition
            + lockedForward * finalForwardMeters
            + lockedRight * finalRightMeters
            + Vector3.up * finalHeightAboveStartHead;
    }

    /// <summary>
    /// Smoothly moves the orb between two fixed points while adding hover,
    /// rotation, and minimum-distance correction every frame.
    /// </summary>
    private IEnumerator MoveAlongSegment(Vector3 from, Vector3 to, float duration)
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
            float easedTime = Smooth01(normalizedTime);

            Vector3 pathPosition = Vector3.Lerp(from, to, easedTime);
            float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            pathPosition += Vector3.up * hoverOffset;

            orbPivot.position = pathPosition;
            MaintainMinimumSpacing();
            FaceBackAlongLockedPath();
            SpinOrb(idleSpinDegreesPerSecond);

            yield return null;
        }

        orbPivot.position = to;
    }

    /// <summary>
    /// Holds the orb near one point for a specified time while preserving the
    /// hover, spin, orientation, and participant-spacing behaviors.
    /// </summary>
    private IEnumerator HoldAtPoint(Vector3 point, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            orbPivot.position = point + Vector3.up * hoverOffset;

            MaintainMinimumSpacing();
            FaceBackAlongLockedPath();
            SpinOrb(idleSpinDegreesPerSecond);

            yield return null;
        }

        orbPivot.position = point;
    }

    /// <summary>
    /// Prevents the participant from getting closer than Minimum Spacing.
    /// Correction is horizontal so the orb does not unexpectedly rise.
    /// </summary>
    private void MaintainMinimumSpacing()
    {
        Vector3 headPosition = playerHead.position;
        Vector3 fromHeadToOrb = orbPivot.position - headPosition;
        float currentDistance = fromHeadToOrb.magnitude;

        if (currentDistance >= minimumSpacingMeters)
            return;

        Vector3 correctionDirection = Vector3.ProjectOnPlane(fromHeadToOrb, Vector3.up);

        if (correctionDirection.sqrMagnitude < 0.0001f)
            correctionDirection = lockedForward;

        correctionDirection.Normalize();

        if (Vector3.Dot(correctionDirection, lockedForward) < 0f)
            correctionDirection = lockedForward;

        float missingDistance = minimumSpacingMeters - currentDistance;
        float correction = Mathf.Min(
            missingDistance * spacingCorrectionSpeed,
            maximumCorrectionMetersPerSecond
        ) * Time.deltaTime;

        orbPivot.position += correctionDirection * correction;
    }

    /// <summary>
    /// Keeps the orb pivot facing back toward the participant's original side
    /// of the path. Only the visual child spins independently.
    /// </summary>
    private void FaceBackAlongLockedPath()
    {
        orbPivot.rotation = Quaternion.LookRotation(-lockedForward, Vector3.up);
    }

    /// <summary>
    /// Rotates only the assigned visual child. Keeping spin on a child avoids
    /// rotating the movement pivot or the doppelganger hierarchy.
    /// </summary>
    private void SpinOrb(float degreesPerSecond)
    {
        if (spinVisual == null)
            return;

        Vector3 axis = spinAxisLocal.sqrMagnitude > 0.0001f
            ? spinAxisLocal.normalized
            : Vector3.up;

        spinVisual.localRotation *= Quaternion.AngleAxis(
            degreesPerSecond * Time.deltaTime,
            axis
        );
    }

    /// <summary>
    /// Hands control to A_OrbIntroSequence exactly once.
    /// </summary>
    private void StartMainSequence()
    {
        if (mainSequenceStarted)
            return;

        mainSequenceStarted = true;
        orbSequence.BeginMainSequenceFromCurrentPosition();
    }

    /// <summary>
    /// Checks required object references and uses the intro sequence's spin
    /// visual as a fallback when Spin Visual was not assigned here.
    /// </summary>
    private bool ValidateReferences()
    {
        if (orbPivot == null)
        {
            Debug.LogError("[OrbLureController] Assign Orb Pivot.");
            return false;
        }

        if (playerHead == null)
        {
            Debug.LogError("[OrbLureController] Assign Player Head or ensure Camera.main exists.");
            return false;
        }

        if (orbSequence == null)
        {
            Debug.LogError("[OrbLureController] Assign A_OrbIntroSequence.");
            return false;
        }

        if (spinVisual == null)
            spinVisual = orbSequence.spinVisual;

        return true;
    }

    /// <summary>
    /// Smoothstep easing used to remove harsh starts and stops from travel.
    /// </summary>
    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}