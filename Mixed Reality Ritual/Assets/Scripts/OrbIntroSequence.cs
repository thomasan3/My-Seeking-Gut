using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OrbIntroSequence : MonoBehaviour
{
    [SerializeField] private FadingUniversal fader;
    [SerializeField] private GameObject doppelgangerMesh;
    [SerializeField] private float playerTravelDistance;

    [Header("References")]
    [Tooltip("PIVOT / MOVER. Recommended: Neb_orb_Pivot (empty parent).")]
    public Transform nebOrb;

    [Tooltip("VISUAL that spins/scales/pulses. Recommended: Neb_orb (child). If empty, falls back to nebOrb.")]
    public Transform spinVisual;

    [Tooltip("If empty, uses Camera.main.transform.")]
    public Transform playerHead;

    [Tooltip("Root of the doppelganger object (the whole character).")]
    public GameObject doppelgangerRoot;

    [Tooltip("Assign the actual Chest or UpperChest bone transform (NOT neck). Used for centering + absorb target.")]
    public Transform doppelgangerChest;

    [Header("Doppelganger Fade (built-in, no FadeIn.cs)")]
    [Tooltip("Seconds to fade invisible -> visible. Try 3–8 for a slow 'forming' look.")]
    public float doppelFadeDuration = 6f;

    [Tooltip("Shape of the fade. Make it slow at first if you want 'forming' not popping.")]
    public AnimationCurve doppelFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("When true, the doppel stays centered inside the orb while fading/after.")]
    public bool keepDoppelCenteredInOrb = true;

    [Tooltip("Offset INSIDE the orb (meters). Use Y negative to lower the body inside the orb.")]
    public Vector3 doppelLocalOffset = new Vector3(0, -0.3f, 0);

    [Tooltip("If true, during the fade the doppel rotates with the orb (spins together).")]
    public bool spinDoppelWithOrbDuringFade = true;

    [Header("NEW: Keep Doppel Spinning Until Absorb")]
    [Tooltip("If true, once the doppel is fully visible it will KEEP spinning with the orb until the absorb finishes.")]
    public bool keepDoppelSpinningUntilAbsorb = true;

    [Header("NEW: Turn Off Object When Absorbed")]
    [Tooltip("Optional: Assign an object to immediately hide when the absorb finishes (ex: the orb mesh, VFX, Nebula child, etc.).")]
    public GameObject turnOffOnAbsorb;

    [Header("Events")]
    public UnityEvent onDoppelgangerFadeIn = new();
    public UnityEvent onAbsorbStart = new();

    [Header("Phase Durations")]
    public float hoverDuration = 3.0f;
    public float descendDuration = 6.0f;
    public float growAndSpinUpDuration = 10.0f;
    public float absorbDuration = 4.0f;

    [Header("Descend Target (in front, lower)")]
    public float descendForward = 3.5f;
    public float descendHeight = 1.4f;
    public float descendRight = 0.0f;

    [Header("Hover/Bob")]
    public float hoverBobAmplitude = 0.12f;
    public float hoverBobSpeed = 0.6f;

    [Header("Heartbeat Pulse")]
    public float heartbeatStrength = 0.04f;
    public float heartbeatRate = 1.2f;
    public AnimationCurve heartbeatCurve = null;

    [Header("Spin (self-spin only)")]
    public Vector3 spinAxisLocal = new Vector3(0, 1, 0);
    public float spinIdle = 8f;
    public float spinCharged = 300f;
    public AnimationCurve spinRamp = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale")]
    public float scaleStart = 1.0f;
    public float scaleCharged = 8.0f;
    public float scaleAbsorbEnd = 0.05f;
    public AnimationCurve scaleRamp = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Keep Player Outside Orb")]
    public float baseOrbRadiusMeters = 0.25f;
    public float cameraSafetyMarginMeters = 0.35f;

    [Header("Absorb Target (fallback if no chest)")]
    public Transform absorbTarget;
    public float absorbForward = 0.6f;
    public float absorbHeight = -0.2f;

    [Header("Follow Player (OFF for world-anchored orb)")]
    public bool followPlayer = false;
    public float followLerp = 8f;

    [Header("Post-Absorb Doppel Alignment")]
    public bool alignDoppelToUserAfterAbsorb = true;

    [Tooltip("We only change the doppel's Y after absorb so it matches the user's sitting/standing feel.")]
    public float doppelChestYOffsetFromHead = -0.55f;

    [Tooltip("Seconds to smoothly lower/raise doppel after absorb (prevents snapping).")]
    public float postAbsorbLowerDuration = 2.0f;

    [Header("Final Facing")]
    [Tooltip("After absorb (and after lowering), force the doppel to face the player.")]
    public bool facePlayerAfterAbsorb = true;

    [Tooltip("How fast it turns to face you after absorb.")]
    public float faceTurnSpeed = 8f;

    [Header("Debug")]
    public bool autoStart = true;

    private Coroutine m_sequence;

    private readonly List<Material> m_doppelMats = new();
    private readonly List<Color> m_doppelBaseColors = new();

    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_Color = Shader.PropertyToID("_Color");

    private bool m_doppelFadeComplete = false;

    private void Awake()
    {
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        if (heartbeatCurve == null || heartbeatCurve.length == 0)
        {
            heartbeatCurve = new AnimationCurve(
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.08f, 1.00f),
                new Keyframe(0.16f, 0.00f),
                new Keyframe(0.26f, 0.65f),
                new Keyframe(0.34f, 0.00f),
                new Keyframe(1.00f, 0.00f)
            );
        }
    }

    private void Start()
    {
        if (nebOrb == null)
        {
            Debug.LogError("[OrbIntroSequence] Assign nebOrb in Inspector.");
            enabled = false;
            return;
        }

        if (playerHead == null)
        {
            Debug.LogError("[OrbIntroSequence] playerHead missing and Camera.main not found.");
            enabled = false;
            return;
        }

        if (spinVisual == null)
            spinVisual = nebOrb;

        spinVisual.localScale = Vector3.one * scaleStart;

        if (doppelgangerRoot != null)
            doppelgangerRoot.SetActive(false);

        if (autoStart)
            m_sequence = StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        m_doppelFadeComplete = false;
        Vector3 startAnchor = nebOrb.position;

        yield return HoverPhase(hoverDuration, nebOrb.position);

        yield return GrowAndSpinUpPhase(growAndSpinUpDuration, nebOrb.position);

        if (doppelgangerRoot != null)
        {
            doppelgangerRoot.SetActive(true);
            fader.StartFadeRenderer(doppelgangerMesh,doppelFadeDuration,1,0);
        }

        yield return AbsorbPhase(absorbDuration, nebOrb.position, doppelgangerChest.position);

        if (turnOffOnAbsorb != null)
            turnOffOnAbsorb.SetActive(false);


        if (facePlayerAfterAbsorb)
            yield return FaceDoppelToPlayer_Speed(faceTurnSpeed);

        nebOrb.gameObject.SetActive(false);
    }


    private IEnumerator HoverPhase(float duration, Vector3 anchor)
    {
        float farthestZ = playerHead.position.z;;
        float maxZ = playerTravelDistance+farthestZ;

        while (farthestZ < maxZ)
        {
            if (playerHead.position.z > farthestZ) {farthestZ = playerHead.position.z;}

            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmplitude;

            nebOrb.position = anchor + Vector3.up * bob + Vector3.forward*Mathf.Lerp(nebOrb.position.z, farthestZ, followLerp);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = scaleStart * pulse;

            spinVisual.localScale = Vector3.one * visualScale;

            SpinSelf(spinIdle);

            yield return null;
        }
    }

    private IEnumerator GrowAndSpinUpPhase(float duration, Vector3 anchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            float bob = Mathf.Sin(Time.time * (hoverBobSpeed * 0.8f)) * (hoverBobAmplitude * 0.25f);
            nebOrb.position = anchor + Vector3.up * bob;

            float sRamp = scaleRamp.Evaluate(u);
            float baseScale = Mathf.Lerp(scaleStart, scaleCharged, sRamp);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = baseScale * pulse;

            spinVisual.localScale = Vector3.one * visualScale;

            float r = spinRamp.Evaluate(u);
            float spin = Mathf.Lerp(spinIdle, spinCharged, r);
            SpinSelf(spin);

            yield return null;
        }

        nebOrb.position = anchor;
        spinVisual.localScale = Vector3.one * scaleCharged;
    }

    private IEnumerator AbsorbPhase(float duration, Vector3 startPos, Vector3 targetPos)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Smooth01(t / duration);

            nebOrb.position = Vector3.Lerp(startPos, targetPos, a);

            SpinSelf(spinCharged);

            float visualScale = Mathf.Lerp(scaleCharged, scaleAbsorbEnd, a);
            spinVisual.localScale = Vector3.one * visualScale;

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            if (keepDoppelSpinningUntilAbsorb && m_doppelFadeComplete && doppelgangerRoot != null)
                doppelgangerRoot.transform.rotation = spinVisual.rotation;

            yield return null;
        }
        spinVisual.localScale = Vector3.one * scaleAbsorbEnd;
    }

    private void MoveDoppelToOrbCenter()
    {
        if (doppelgangerRoot == null) return;

        Vector3 orbCenter = nebOrb.position + nebOrb.TransformVector(doppelLocalOffset);

        if (doppelgangerChest != null)
        {
            Vector3 delta = orbCenter - doppelgangerChest.position;
            doppelgangerRoot.transform.position = doppelgangerRoot.transform.position + delta;
        }
        else
        {
            doppelgangerRoot.transform.position = orbCenter;
        }
    }


    private IEnumerator FaceDoppelToPlayer_Speed(float degPerSecond)
    {
        if (doppelgangerRoot == null || playerHead == null) yield break;

        Vector3 toPlayer = playerHead.position - doppelgangerRoot.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) yield break;

        Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

        if (degPerSecond <= 0f)
        {
            doppelgangerRoot.transform.rotation = targetRot;
            yield break;
        }

        while (true)
        {
            Vector3 v = playerHead.position - doppelgangerRoot.transform.position;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0001f)
                targetRot = Quaternion.LookRotation(v.normalized, Vector3.up);

            float maxStep = degPerSecond * Time.deltaTime;
            doppelgangerRoot.transform.rotation = Quaternion.RotateTowards(doppelgangerRoot.transform.rotation, targetRot, maxStep);

            float remaining = Quaternion.Angle(doppelgangerRoot.transform.rotation, targetRot);
            if (remaining <= 0.25f) 
                break;

            yield return null;
        }

        doppelgangerRoot.transform.rotation = targetRot;
    }


    private void SpinSelf(float degreesPerSecond)
    {
        Vector3 axis = spinAxisLocal.normalized;
        spinVisual.localRotation *= Quaternion.AngleAxis(degreesPerSecond * Time.deltaTime, axis);
    }

    private float GetHeartbeatPulse()
    {
        float phase = Mathf.Repeat(Time.time * heartbeatRate, 1f);
        return heartbeatCurve.Evaluate(phase);
    }

    private Vector3 GetRelativePos(float forwardMeters, float heightMeters, float rightMeters)
    {
        Vector3 forward = FlattenForward(playerHead.forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        return playerHead.position
               + forward * forwardMeters
               + Vector3.up * heightMeters
               + right * rightMeters;
    }

    private static Vector3 FlattenForward(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return forward.normalized;
    }

    private static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
