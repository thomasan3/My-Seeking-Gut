using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OrbIntroSequence : MonoBehaviour
{
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

    [Header("Events")]
    public UnityEvent onDoppelgangerFadeIn = new();
    public UnityEvent onAbsorbStart = new();

    [Header("Start Placement (world-anchored unless followPlayer is enabled)")]
    public float startForward = 4.0f;
    public float startHeight = 2.2f;  
    public float startRight = 0.0f;
    public bool snapOrbOnStart = true;

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

    public float doppelChestYOffsetFromHead = -0.55f;

    [Header("Debug")]
    public bool autoStart = true;

    private Coroutine m_sequence;

    private readonly List<Material> m_doppelMats = new();
    private readonly List<Color> m_doppelBaseColors = new();

    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_Color = Shader.PropertyToID("_Color");

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

        if (snapOrbOnStart)
            PlaceOrbStart();

        spinVisual.localScale = Vector3.one * scaleStart;
        KeepOutside(scaleStart);

        if (doppelgangerRoot != null)
            doppelgangerRoot.SetActive(false);

        if (autoStart)
            m_sequence = StartCoroutine(Sequence());
    }

    private void PlaceOrbStart()
    {
        nebOrb.position = GetRelativePos(startForward, startHeight, startRight);
        nebOrb.rotation = Quaternion.LookRotation(-FlattenForward(playerHead.forward), Vector3.up);
    }

    private IEnumerator Sequence()
    {
        Vector3 startAnchor = GetRelativePos(startForward, startHeight, startRight);
        nebOrb.position = startAnchor;
        KeepOutside(scaleStart);

        yield return HoverPhase(hoverDuration, startAnchor);

        Vector3 descendAnchor = GetRelativePos(descendForward, descendHeight, descendRight);
        yield return MovePhase(descendDuration, startAnchor, descendAnchor);

        yield return GrowAndSpinUpPhase(growAndSpinUpDuration, descendAnchor);

        if (doppelgangerRoot != null)
        {
            CacheDoppelMaterials();
            PrepareDoppelInvisibleNoFlash();

            doppelgangerRoot.SetActive(true);
            onDoppelgangerFadeIn?.Invoke();

            yield return RevealDoppelWhileOrbLives(doppelFadeDuration);
        }

        onAbsorbStart?.Invoke();

        Vector3 absorbPos =
            (doppelgangerChest != null) ? doppelgangerChest.position :
            (absorbTarget != null) ? absorbTarget.position :
            GetRelativePos(absorbForward, absorbHeight, 0);

        yield return AbsorbPhase(absorbDuration, nebOrb.position, absorbPos);

        if (alignDoppelToUserAfterAbsorb)
            AlignDoppelToUserY();

        nebOrb.gameObject.SetActive(false);
    }

    private IEnumerator HoverPhase(float duration, Vector3 anchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            if (followPlayer)
                anchor = Vector3.Lerp(anchor, GetRelativePos(startForward, startHeight, startRight), Time.deltaTime * followLerp);

            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmplitude;
            nebOrb.position = anchor + Vector3.up * bob;

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = scaleStart * pulse;
            spinVisual.localScale = Vector3.one * visualScale;
            KeepOutside(visualScale);

            SpinSelf(spinIdle);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = anchor;
        spinVisual.localScale = Vector3.one * scaleStart;
        KeepOutside(scaleStart);
    }

    private IEnumerator MovePhase(float duration, Vector3 fromAnchor, Vector3 toAnchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Smooth01(t / duration);

            if (followPlayer)
                toAnchor = GetRelativePos(descendForward, descendHeight, descendRight);

            Vector3 basePos = Vector3.Lerp(fromAnchor, toAnchor, a);
            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * (hoverBobAmplitude * 0.35f);
            nebOrb.position = basePos + Vector3.up * bob;

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = scaleStart * pulse;
            spinVisual.localScale = Vector3.one * visualScale;
            KeepOutside(visualScale);

            SpinSelf(spinIdle);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = toAnchor;
        spinVisual.localScale = Vector3.one * scaleStart;
        KeepOutside(scaleStart);
    }

    private IEnumerator GrowAndSpinUpPhase(float duration, Vector3 anchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            if (followPlayer)
                anchor = Vector3.Lerp(anchor, GetRelativePos(descendForward, descendHeight, descendRight), Time.deltaTime * followLerp);

            float bob = Mathf.Sin(Time.time * (hoverBobSpeed * 0.8f)) * (hoverBobAmplitude * 0.25f);
            nebOrb.position = anchor + Vector3.up * bob;

            float sRamp = scaleRamp.Evaluate(u);
            float baseScale = Mathf.Lerp(scaleStart, scaleCharged, sRamp);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = baseScale * pulse;

            spinVisual.localScale = Vector3.one * visualScale;
            KeepOutside(visualScale);

            float r = spinRamp.Evaluate(u);
            float spin = Mathf.Lerp(spinIdle, spinCharged, r);
            SpinSelf(spin);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = anchor;
        spinVisual.localScale = Vector3.one * scaleCharged;
        KeepOutside(scaleCharged);
    }

    private IEnumerator RevealDoppelWhileOrbLives(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float shaped = doppelFadeCurve.Evaluate(u);

            SpinSelf(spinCharged);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            SetDoppelAlpha(shaped);

            yield return null;
        }

        SetDoppelAlpha(1f);
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
            KeepOutside(visualScale);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = targetPos;
        spinVisual.localScale = Vector3.one * scaleAbsorbEnd;
        KeepOutside(scaleAbsorbEnd);
    }


    private void CacheDoppelMaterials()
    {
        m_doppelMats.Clear();
        m_doppelBaseColors.Clear();

        if (doppelgangerRoot == null) return;

        var renderers = doppelgangerRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m == null) continue;
                m_doppelMats.Add(m);
                m_doppelBaseColors.Add(ReadColor(m));
                ForceMaterialTransparentIfPossible(m);
            }
        }
    }

    private void PrepareDoppelInvisibleNoFlash()
    {
        SetDoppelAlpha(0f);
        if (keepDoppelCenteredInOrb)
            MoveDoppelToOrbCenter();
    }

    private void SetDoppelAlpha(float a)
    {
        for (int i = 0; i < m_doppelMats.Count; i++)
        {
            var m = m_doppelMats[i];
            var baseC = m_doppelBaseColors[i];
            WriteColor(m, new Color(baseC.r, baseC.g, baseC.b, a));
        }
    }

    private static Color ReadColor(Material m)
    {
        if (m.HasProperty(ID_BaseColor)) return m.GetColor(ID_BaseColor);
        if (m.HasProperty(ID_Color)) return m.GetColor(ID_Color);
        return Color.white;
    }

    private static void WriteColor(Material m, Color c)
    {
        if (m.HasProperty(ID_BaseColor)) m.SetColor(ID_BaseColor, c);
        else if (m.HasProperty(ID_Color)) m.SetColor(ID_Color, c);
    }

    private static void ForceMaterialTransparentIfPossible(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.renderQueue = 3000;
    }

    private void MoveDoppelToOrbCenter()
    {
        if (doppelgangerRoot == null) return;

        Vector3 orbCenter = nebOrb.position + nebOrb.TransformVector(doppelLocalOffset);

        if (doppelgangerChest != null)
        {
            Vector3 delta = orbCenter - doppelgangerChest.position;
            doppelgangerRoot.transform.position += delta;
        }
        else
        {
            doppelgangerRoot.transform.position = orbCenter;
        }
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

    private void KeepOutside(float visualScale)
    {
        if (playerHead == null) return;

        float radius = Mathf.Max(0.001f, baseOrbRadiusMeters * visualScale);
        float minDist = radius + cameraSafetyMarginMeters;

        Vector3 camPos = playerHead.position;
        Vector3 dir = nebOrb.position - camPos;

        float dist = dir.magnitude;
        if (dist < 0.0001f) dir = FlattenForward(playerHead.forward);
        else dir /= dist;

        if (dist < minDist)
            nebOrb.position = camPos + dir * minDist;
    }

    private void AlignDoppelToUserY()
    {
        if (doppelgangerRoot == null || playerHead == null || doppelgangerChest == null) return;

        float desiredChestY = playerHead.position.y + doppelChestYOffsetFromHead;
        float deltaY = desiredChestY - doppelgangerChest.position.y;

        Vector3 p = doppelgangerRoot.transform.position;
        p.y += deltaY;
        doppelgangerRoot.transform.position = p;
    }

    private static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
