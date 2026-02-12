using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class A_OrbIntroSequence : MonoBehaviour
{
    [Header("References")]
    public Transform nebOrb;
    public Transform spinVisual;
    public Transform playerHead;
    public GameObject doppelgangerRoot;
    public Transform doppelgangerChest;

    [Header("World Ground Clamp")]
    public float minWorldY = 0f;

    [Header("Doppel Fade")]
    public float doppelFadeDuration = 6f;
    public AnimationCurve doppelFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool keepDoppelCenteredInOrb = true;
    public Vector3 doppelLocalOffset = new Vector3(0, -0.3f, 0);

    [Header("Turn Off Object When Absorbed")]
    public GameObject turnOffOnAbsorb;

    [Header("Events")]
    public UnityEvent onDoppelgangerFadeIn = new();
    public UnityEvent onAbsorbStart = new();

    [Header("Start Placement (fallback if lure disabled)")]
    public float startForward = 4.0f;
    public float startHeight = 2.2f;
    public float startRight = 0.0f;
    public bool snapOrbOnStart = true;

    [Header("Phase Durations")]
    public float hoverDuration = 3.0f;
    public float descendDuration = 6.0f;
    public float growAndSpinUpDuration = 10.0f;
    public float absorbDuration = 4.0f;

    [Header("Descend Target")]
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

    [Header("Spin")]
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

    [Header("Post-Absorb Doppel Alignment")]
    public bool alignDoppelToUserAfterAbsorb = true;
    public float doppelChestYOffsetFromHead = -0.55f;
    public float postAbsorbLowerDuration = 2.0f;

    [Header("Final Facing")]
    public bool facePlayerAfterAbsorb = true;
    public float faceTurnSpeed = 8f;

    [Header("Lure: Right travel -> Slide to center -> Center travel -> Stop")]
    public bool enableLure = true;

    public float lureStartRight = 1.4f;
    public float lureStartForward = 2.8f;
    public float lureHeightAboveHead = 1.2f;

    public float lureForwardSpeed = 0.35f;

    public float lureRightTravelSeconds = 10.0f;
    public float lureSlideToCenterSeconds = 5.0f;
    public float lureCenterTravelSeconds = 5.0f;

    public float lureEndRight = 0.0f;
    public float lureEndHeight = 1.4f;

    public float lureMinSpacing = 2.2f;
    public float lureBackAwayStrength = 2.2f;
    public float lureMaxBackAwaySpeed = 1.2f;

    public float lureStartDistance = 2.2f;
    public float lureForceStartAfterSeconds = 120f;

    [Header("Grow Spacing")]
    public float growExtraSpacing = 0.8f;

    [Header("Sequence Start After Lure")]
    public bool skipHoverAndDescendAfterLure = true;

    [Header("Debug")]
    public bool autoStart = true;

    private Coroutine m_sequence;

    private readonly List<Material> m_doppelMats = new();
    private readonly List<Color> m_doppelBaseColors = new();

    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_Color = Shader.PropertyToID("_Color");

    private Vector3 m_lockForward;
    private Vector3 m_lockRight;
    private Vector3 m_lureStartHeadPos;
    private Vector3 m_lureStoppedPos;

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
            Debug.LogError("[A_OrbIntroSequence] Assign nebOrb.");
            enabled = false;
            return;
        }

        if (playerHead == null)
        {
            Debug.LogError("[A_OrbIntroSequence] playerHead missing and Camera.main not found.");
            enabled = false;
            return;
        }

        if (spinVisual == null)
            spinVisual = nebOrb;

        if (!enableLure && snapOrbOnStart)
            PlaceOrbStart();

        spinVisual.localScale = Vector3.one * scaleStart;

        if (doppelgangerRoot != null)
            doppelgangerRoot.SetActive(false);

        if (autoStart)
            m_sequence = StartCoroutine(Sequence());
    }

    private void PlaceOrbStart()
    {
        nebOrb.position = ClampMinY(GetRelativePos(startForward, startHeight, startRight));
        nebOrb.rotation = Quaternion.LookRotation(-FlattenForward(playerHead.forward), Vector3.up);
    }

    private IEnumerator Sequence()
    {
        if (enableLure)
            yield return LurePhase();

        Vector3 startAnchor = ClampMinY(nebOrb.position);

        if (!enableLure || !skipHoverAndDescendAfterLure)
        {
            yield return HoverPhase(hoverDuration, startAnchor);

            Vector3 descendAnchor = ClampMinY(GetRelativePos(descendForward, descendHeight, descendRight));
            yield return MovePhase(descendDuration, startAnchor, descendAnchor);

            startAnchor = descendAnchor;
        }

        yield return GrowAndSpinUpPhase(growAndSpinUpDuration, startAnchor);

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

        absorbPos = ClampMinY(absorbPos);

        yield return AbsorbPhase(absorbDuration, nebOrb.position, absorbPos);

        if (turnOffOnAbsorb != null)
            turnOffOnAbsorb.SetActive(false);

        if (alignDoppelToUserAfterAbsorb)
            yield return AlignDoppelToUserY_Smooth(postAbsorbLowerDuration);

        if (facePlayerAfterAbsorb)
            yield return FaceDoppelToPlayer_Speed(faceTurnSpeed);

        nebOrb.gameObject.SetActive(false);
    }

    private IEnumerator LurePhase()
    {
        m_lockForward = FlattenForward(playerHead.forward);
        m_lockRight = Vector3.Cross(Vector3.up, m_lockForward).normalized;
        m_lureStartHeadPos = playerHead.position;

        float totalForwardTravelTime = Mathf.Max(0f, lureRightTravelSeconds + lureSlideToCenterSeconds + lureCenterTravelSeconds);

        float t = 0f;

        while (t < totalForwardTravelTime)
        {
            t += Time.deltaTime;

            float forwardTravel = lureStartForward + lureForwardSpeed * t;

            float rightOffset;
            float heightOffset;

            if (t <= lureRightTravelSeconds)
            {
                rightOffset = lureStartRight;
                heightOffset = lureHeightAboveHead;
            }
            else if (t <= lureRightTravelSeconds + lureSlideToCenterSeconds)
            {
                float u = (t - lureRightTravelSeconds) / Mathf.Max(0.0001f, lureSlideToCenterSeconds);
                float s = Smooth01(u);
                rightOffset = Mathf.Lerp(lureStartRight, lureEndRight, s);
                heightOffset = Mathf.Lerp(lureHeightAboveHead, lureEndHeight, s);
            }
            else
            {
                rightOffset = lureEndRight;
                heightOffset = lureEndHeight;
            }

            Vector3 desired = m_lureStartHeadPos
                              + m_lockForward * forwardTravel
                              + m_lockRight * rightOffset
                              + Vector3.up * heightOffset;

            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * (hoverBobAmplitude * 0.2f);
            desired += Vector3.up * bob;

            nebOrb.position = ClampMinY(Vector3.Lerp(nebOrb.position, desired, Time.deltaTime * 4f));
            nebOrb.rotation = Quaternion.LookRotation(-m_lockForward, Vector3.up);

            SpinSelf(spinIdle);

            EnforceMinDistance(scaleStart, lureMinSpacing);

            yield return null;
        }

        m_lureStoppedPos = nebOrb.position;

        float wait = 0f;
        while (true)
        {
            wait += Time.deltaTime;

            nebOrb.position = ClampMinY(m_lureStoppedPos);
            nebOrb.rotation = Quaternion.LookRotation(-m_lockForward, Vector3.up);

            SpinSelf(spinIdle);

            EnforceMinDistance(scaleStart, lureMinSpacing);

            float dist = Vector3.Distance(playerHead.position, nebOrb.position);
            bool closeEnough = dist <= lureStartDistance;
            bool forced = wait >= lureForceStartAfterSeconds;

            if (closeEnough || forced)
                yield break;

            yield return null;
        }
    }

    private IEnumerator HoverPhase(float duration, Vector3 anchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmplitude;
            nebOrb.position = ClampMinY(anchor + Vector3.up * bob);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = scaleStart * pulse;

            spinVisual.localScale = Vector3.one * visualScale;

            EnforceMinDistance(visualScale, lureMinSpacing);

            SpinSelf(spinIdle);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = ClampMinY(anchor);
        spinVisual.localScale = Vector3.one * scaleStart;
        EnforceMinDistance(scaleStart, lureMinSpacing);
    }

    private IEnumerator MovePhase(float duration, Vector3 fromAnchor, Vector3 toAnchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Smooth01(t / Mathf.Max(0.0001f, duration));

            Vector3 basePos = Vector3.Lerp(fromAnchor, toAnchor, a);
            float bob = Mathf.Sin(Time.time * hoverBobSpeed) * (hoverBobAmplitude * 0.35f);

            nebOrb.position = ClampMinY(basePos + Vector3.up * bob);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = scaleStart * pulse;

            spinVisual.localScale = Vector3.one * visualScale;

            EnforceMinDistance(visualScale, lureMinSpacing);

            SpinSelf(spinIdle);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = ClampMinY(toAnchor);
        spinVisual.localScale = Vector3.one * scaleStart;
        EnforceMinDistance(scaleStart, lureMinSpacing);
    }

    private IEnumerator GrowAndSpinUpPhase(float duration, Vector3 anchor)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

            float bob = Mathf.Sin(Time.time * (hoverBobSpeed * 0.8f)) * (hoverBobAmplitude * 0.25f);
            nebOrb.position = ClampMinY(anchor + Vector3.up * bob);

            float sRamp = scaleRamp.Evaluate(u);
            float baseScale = Mathf.Lerp(scaleStart, scaleCharged, sRamp);

            float pulse = 1f + GetHeartbeatPulse() * heartbeatStrength;
            float visualScale = baseScale * pulse;

            spinVisual.localScale = Vector3.one * visualScale;

            float growMin = Mathf.Max(lureMinSpacing, (baseOrbRadiusMeters * visualScale + cameraSafetyMarginMeters) + growExtraSpacing);
            EnforceMinDistance(visualScale, growMin);

            float r = spinRamp.Evaluate(u);
            float spin = Mathf.Lerp(spinIdle, spinCharged, r);
            SpinSelf(spin);

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = ClampMinY(anchor);
        spinVisual.localScale = Vector3.one * scaleCharged;

        float finalMin = Mathf.Max(lureMinSpacing, (baseOrbRadiusMeters * scaleCharged + cameraSafetyMarginMeters) + growExtraSpacing);
        EnforceMinDistance(scaleCharged, finalMin);
    }

    private IEnumerator RevealDoppelWhileOrbLives(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
            float shaped = doppelFadeCurve.Evaluate(u);

            SpinSelf(spinCharged);

            float currentScale = spinVisual.localScale.x;
            EnforceMinDistance(currentScale, Mathf.Max(lureMinSpacing, baseOrbRadiusMeters * currentScale + cameraSafetyMarginMeters));

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
            float a = Smooth01(t / Mathf.Max(0.0001f, duration));

            nebOrb.position = ClampMinY(Vector3.Lerp(startPos, targetPos, a));

            SpinSelf(spinCharged);

            float visualScale = Mathf.Lerp(scaleCharged, scaleAbsorbEnd, a);
            spinVisual.localScale = Vector3.one * visualScale;

            EnforceMinDistance(visualScale, Mathf.Max(lureMinSpacing, baseOrbRadiusMeters * visualScale + cameraSafetyMarginMeters));

            if (keepDoppelCenteredInOrb)
                MoveDoppelToOrbCenter();

            yield return null;
        }

        nebOrb.position = ClampMinY(targetPos);
        spinVisual.localScale = Vector3.one * scaleAbsorbEnd;
        EnforceMinDistance(scaleAbsorbEnd, Mathf.Max(lureMinSpacing, baseOrbRadiusMeters * scaleAbsorbEnd + cameraSafetyMarginMeters));
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
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);

        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);

        m.renderQueue = 3000;
    }

    private void MoveDoppelToOrbCenter()
    {
        if (doppelgangerRoot == null) return;

        Vector3 orbCenter = nebOrb.position + nebOrb.TransformVector(doppelLocalOffset);
        orbCenter = ClampMinY(orbCenter);

        if (doppelgangerChest != null)
        {
            Vector3 delta = orbCenter - doppelgangerChest.position;
            doppelgangerRoot.transform.position = ClampMinY(doppelgangerRoot.transform.position + delta);
        }
        else
        {
            doppelgangerRoot.transform.position = orbCenter;
        }
    }

    private IEnumerator AlignDoppelToUserY_Smooth(float duration)
    {
        if (doppelgangerRoot == null || playerHead == null || doppelgangerChest == null) yield break;

        float desiredChestY = Mathf.Max(minWorldY, playerHead.position.y + doppelChestYOffsetFromHead);
        float deltaY = desiredChestY - doppelgangerChest.position.y;

        Vector3 startPos = doppelgangerRoot.transform.position;
        Vector3 endPos = ClampMinY(startPos + new Vector3(0, deltaY, 0));

        if (duration <= 0f)
        {
            doppelgangerRoot.transform.position = endPos;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Smooth01(t / Mathf.Max(0.0001f, duration));
            doppelgangerRoot.transform.position = ClampMinY(Vector3.Lerp(startPos, endPos, a));
            yield return null;
        }

        doppelgangerRoot.transform.position = endPos;
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

        return playerHead.position + forward * forwardMeters + Vector3.up * heightMeters + right * rightMeters;
    }

    private static Vector3 FlattenForward(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return forward.normalized;
    }

    private void EnforceMinDistance(float visualScale, float minDistOverride)
    {
        if (playerHead == null || nebOrb == null) return;

        float baseMin = baseOrbRadiusMeters * visualScale + cameraSafetyMarginMeters;
        float minDist = Mathf.Max(baseMin, minDistOverride);

        Vector3 head = playerHead.position;
        Vector3 delta = nebOrb.position - head;
        float dist = delta.magnitude;

        if (dist >= minDist) return;

        Vector3 dir = (dist > 0.0001f) ? (delta / dist) : FlattenForward(playerHead.forward);
        dir.y = 0f;

        if (Vector3.Dot(dir, FlattenForward(playerHead.forward)) < 0.0f)
            dir = FlattenForward(playerHead.forward);

        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        float need = (minDist - dist);
        float push = Mathf.Min(need * lureBackAwayStrength, lureMaxBackAwaySpeed * Time.deltaTime);

        nebOrb.position = ClampMinY(nebOrb.position + dir * push);
    }

    private Vector3 ClampMinY(Vector3 p)
    {
        if (p.y < minWorldY) p.y = minWorldY;
        return p;
    }

    private static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    public void StartSequenceFromCurrentOrbPosition()
    {
        
    }
}
