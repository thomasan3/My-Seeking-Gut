using UnityEngine;
using System.Collections;

public class OrbLureController : MonoBehaviour
{
    [Header("References")]
    public Transform orbPivot;                 // Neb_orb_Pivot (NOT parented to rig)
    public Transform playerHead;               // CenterEyeAnchor
    public A_OrbIntroSequence orbSequence;     // your big sequence script
    public GameObject orbRootToShowHide;       // optional root/mesh

    [Header("Start (Black)")]
    public float startDelaySeconds = 1.0f;

    [Header("Spawn From Behind -> Right -> Front (LOCKED PATH)")]
    public float behindMeters = 1.2f;
    public float rightMeters = 1.0f;
    public float frontMeters = 2.6f;

    [Header("HEIGHT")]
    [Tooltip("How high above the player's head the orb should be during the lure.")]
    public float heightAboveHead = 1.2f;  // <- make this match your old scene feel (try 1.0–2.0)

    [Header("Lure Motion")]
    public float moveDurationSeconds = 12f;
    public float moveSmoothing = 3.5f;

    [Header("Spacing (player can’t get too close)")]
    public float minSpacing = 1.6f;          // desired minimum distance
    public float pushBackStrength = 2.5f;    // how strongly orb backs away when too close
    public float maxPushBackPerSecond = 1.2f; // prevents teleport-y jumps

    [Header("Start the main animation when...")]
    public float requiredDistanceToStart = 2.0f;
    public float forceStartAfterSeconds = 120f;

    private bool _lureDone;
    private bool _sequenceStarted;
    private float _timeSinceStop;

    // We lock the basis vectors at lure start so it doesn't "flip" when you turn
    private Vector3 _lockedForward;
    private Vector3 _lockedRight;
    private Vector3 _lockedUp;

    // Locked path points (world positions)
    private Vector3 _pBehind, _pRight, _pFront;

    void Start()
    {
        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;

        if (orbRootToShowHide != null) orbRootToShowHide.SetActive(false);

        if (orbSequence != null) orbSequence.autoStart = false;

        Invoke(nameof(BeginLure), startDelaySeconds);
    }

    void BeginLure()
    {
        if (orbPivot == null || playerHead == null) return;

        if (orbRootToShowHide != null) orbRootToShowHide.SetActive(true);

        // LOCK direction at start (prevents snapping behind when player turns)
        _lockedForward = playerHead.forward; _lockedForward.y = 0; _lockedForward.Normalize();
        _lockedRight = playerHead.right; _lockedRight.y = 0; _lockedRight.Normalize();
        _lockedUp = Vector3.up;

        float y = playerHead.position.y + heightAboveHead;

        _pBehind = new Vector3(playerHead.position.x, y, playerHead.position.z) - _lockedForward * behindMeters;
        _pRight = new Vector3(playerHead.position.x, y, playerHead.position.z) + _lockedRight * rightMeters - _lockedForward * (behindMeters * 0.25f);
        _pFront = new Vector3(playerHead.position.x, y, playerHead.position.z) + _lockedForward * frontMeters + _lockedRight * (rightMeters * 0.25f);

        // Start at behind
        orbPivot.position = _pBehind;

        StartCoroutine(LureMove());
    }

    IEnumerator LureMove()
    {
        float t = 0f;

        while (t < moveDurationSeconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / moveDurationSeconds);
            float s = u * u * (3f - 2f * u);

            // two-leg lerp: behind->right then right->front
            Vector3 desired = (s < 0.5f)
                ? Vector3.Lerp(_pBehind, _pRight, s / 0.5f)
                : Vector3.Lerp(_pRight, _pFront, (s - 0.5f) / 0.5f);

            // Apply spacing safety (never pushes behind; pushes along locked forward/right plane)
            desired = ApplyMinSpacing(desired);

            orbPivot.position = Vector3.Lerp(orbPivot.position, desired, Time.deltaTime * moveSmoothing);

            yield return null;
        }

        _lureDone = true;
        _timeSinceStop = 0f;
    }

    void Update()
    {
        if (_sequenceStarted || !_lureDone || orbPivot == null || playerHead == null) return;

        _timeSinceStop += Time.deltaTime;

        float dist = Vector3.Distance(playerHead.position, orbPivot.position);
        bool closeEnough = dist <= requiredDistanceToStart;
        bool forced = _timeSinceStop >= forceStartAfterSeconds;

        if (closeEnough || forced)
        {
            _sequenceStarted = true;

            if (orbSequence != null)
                orbSequence.StartSequenceFromCurrentOrbPosition();
        }
    }

    Vector3 ApplyMinSpacing(Vector3 desired)
    {
        Vector3 head = playerHead.position;
        Vector3 toOrb = desired - head;
        float dist = toOrb.magnitude;

        if (dist < 0.001f) toOrb = _lockedForward;
        else toOrb /= dist;

        if (dist >= minSpacing) return desired;

        float need = (minSpacing - dist);

        // push away, but limit per second to prevent "teleport snap"
        float push = Mathf.Min(need * pushBackStrength, maxPushBackPerSecond * Time.deltaTime);

        // Push in a direction that can't go "behind": prefer pushing forward/right plane
        Vector3 pushDir = (Vector3.ProjectOnPlane(toOrb, Vector3.up)).normalized;
        if (pushDir.sqrMagnitude < 0.0001f) pushDir = _lockedForward;

        return desired + pushDir * push;
    }
}
