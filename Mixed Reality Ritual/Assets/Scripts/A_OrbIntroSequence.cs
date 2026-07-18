using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class A_OrbIntroSequence : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign Neb_orb_Pivot.")]
    public Transform nebOrb;

    [Tooltip("Assign Neb_orb_VisualPivot, or the visual object that should spin and scale.")]
    public Transform spinVisual;

    [Tooltip("Assign CenterEyeAnchor.")]
    public Transform playerHead;

    [Tooltip("Assign the root GameObject of the doppelganger.")]
    public GameObject doppelgangerRoot;

    [Tooltip("Assign the doppelganger Chest, UpperChest, or Spine2 bone.")]
    public Transform doppelgangerChest;

    [Header("World Height")]
    [Tooltip("Lowest world-space Y allowed for the orb pivot. This does not increase with orb scale, so the orb will not fly upward while growing.")]
    public float minimumOrbCenterY = 0.15f;

    [Tooltip("Lowest world-space Y allowed for the doppelganger root.")]
    public float minimumDoppelRootY = 0f;

    [Header("Orb Ground Hover")]
    [Tooltip("Keeps the bottom of the growing orb slightly above the ground.")]
    public bool keepOrbAboveGround = true;

    [Tooltip("Desired space between the bottom of the orb and the ground.")]
    public float orbGroundClearance = 0.1f;

    [Tooltip("Maximum amount the orb center may rise during growth to avoid a sudden flight upward.")]
    public float maximumGroundLiftDuringGrowth = 0.75f;

    [Header("Doppelganger Fade")]
    [Tooltip("Seconds required for the doppelganger to become fully visible.")]
    public float doppelFadeDuration = 6f;

    [Tooltip("Controls the fade speed across the full fade duration.")]
    public AnimationCurve doppelFadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Keeps the assigned chest bone centered inside the orb during the fade.")]
    public bool keepDoppelCenteredInOrb = true;

    [Tooltip("Moves the doppelganger inside the orb without changing the orb position. Use a negative Y value to lower the doppelganger.")]
    public Vector3 doppelLocalOffset = new Vector3(0f, -0.3f, 0f);

    [Tooltip("Makes the doppelganger face the participant before and during the fade.")]
    public bool facePlayerDuringFade = true;

    [Tooltip("Corrects the model's built-in forward direction. Try 90, -90, 0, or 180.")]
    public float doppelFacingYawOffset = 90f;

    [Header("Full-Size Manifestation")]
    [Tooltip("Optional pause at full size before the doppelganger begins fading in.")]
    public float fullSizeHoldBeforeFade = 0f;

    [Tooltip("Optional pause after the doppelganger is fully visible and before the orb starts shrinking.")]
    public float fullSizeHoldAfterFade = 0.25f;

    [Header("Turn Off Object When Absorbed")]
    [Tooltip("Assign Neb_orb_VisualPivot. Only this visual object is disabled after absorption; the doppelganger remains visible.")]
    public GameObject turnOffOnAbsorb;

    [Header("Events")]
    public UnityEvent onDoppelgangerFadeIn = new();
    public UnityEvent onAbsorbStart = new();

    [Header("Optional Standalone Start")]
    [Tooltip("Used only when Auto Start is enabled.")]
    public float startForward = 4f;

    [Tooltip("Used only when Auto Start is enabled.")]
    public float startHeight = 2.2f;

    [Tooltip("Used only when Auto Start is enabled.")]
    public float startRight = 0f;

    [Tooltip("Places the orb at the standalone start position when Auto Start is enabled.")]
    public bool snapOrbOnStart = true;

    [Header("Growth")]
    [Tooltip("Seconds required for the orb to accelerate and grow.")]
    public float growAndSpinUpDuration = 10f;

    [Tooltip("Small vertical hovering motion while growing.")]
    public float growHoverAmplitude = 0.03f;

    [Tooltip("Speed of the hovering motion while growing.")]
    public float growHoverSpeed = 0.5f;

    [Tooltip("Extra space added between the user and the nearest surface of the growing orb.")]
    public float growExtraSurfaceGap = 0.6f;

    [Tooltip("How smoothly the orb center moves backward while growing.")]
    public float growBackAwaySmoothing = 5f;

    [Header("Heartbeat Pulse")]
    public float heartbeatStrength = 0.04f;
    public float heartbeatRate = 1.2f;
    public AnimationCurve heartbeatCurve;

    [Header("Spin")]
    public Vector3 spinAxisLocal = new Vector3(0f, 1f, 0f);
    public float spinIdle = 8f;
    public float spinCharged = 800f;
    public AnimationCurve spinRamp =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Scale")]
    public float scaleStart = 1f;
    public float scaleCharged = 6f;
    public AnimationCurve scaleRamp =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Orb Size And User Spacing")]
    [Tooltip("Approximate radius of the visible orb when scale equals 1.")]
    public float baseOrbRadiusMeters = 0.25f;

    [Tooltip("Minimum protected space between the user and the nearest surface of the orb.")]
    public float cameraSafetyMarginMeters = 0.35f;

    [Header("Shrink In Front Of Doppelganger")]
    [Tooltip("Seconds required for the orb to shrink after the doppelganger is fully visible.")]
    public float shrinkDuration = 5f;

    [Tooltip("Orb scale reached before it enters the chest.")]
    public float smallOrbScale = 0.12f;

    [Tooltip("Distance in front of the chest where the orb finishes shrinking.")]
    public float shrinkFrontOfChestMeters = 0.35f;

    [Tooltip("Vertical offset for the small orb in front of the chest.")]
    public float shrinkFrontOfChestHeight = 0f;

    public AnimationCurve shrinkCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Chest Absorption")]
    [Tooltip("Seconds required for the small orb to enter the doppelganger's chest.")]
    public float absorbDuration = 1.5f;

    [Tooltip("Optional fallback target when Doppelganger Chest is not assigned.")]
    public Transform absorbTarget;

    [Tooltip("Fallback forward position relative to the user.")]
    public float absorbForward = 0.6f;

    [Tooltip("Fallback height relative to the user's head.")]
    public float absorbHeight = -0.2f;

    [Header("Post-Absorb Doppel Lowering")]
    [Tooltip("Smoothly lowers or raises the doppelganger after the orb has disappeared into its chest.")]
    public bool lowerDoppelAfterAbsorb = true;

    [Tooltip("Desired doppelganger chest height relative to the participant's head.")]
    public float doppelChestYOffsetFromHead = -0.55f;

    [Tooltip("Seconds required for the post-absorb lowering movement.")]
    public float postAbsorbLowerDuration = 2f;

    [Header("Debug")]
    [Tooltip("Leave disabled when OrbLureController starts this sequence.")]
    public bool autoStart = false;

    private Coroutine sequenceCoroutine;

    private readonly List<Material> doppelMaterials = new();
    private readonly List<Color> doppelBaseColors = new();

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

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
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        if (spinVisual == null)
            spinVisual = nebOrb;

        spinVisual.localScale = Vector3.one * scaleStart;

        if (doppelgangerRoot != null)
            doppelgangerRoot.SetActive(false);

        if (autoStart)
        {
            if (snapOrbOnStart)
                PlaceStandaloneStart();

            BeginMainSequenceFromCurrentPosition();
        }
    }

    public void BeginMainSequenceFromCurrentPosition()
    {
        if (!isActiveAndEnabled)
            return;

        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);

        sequenceCoroutine = StartCoroutine(MainSequence());
    }

    private void PlaceStandaloneStart()
    {
        nebOrb.position = ClampOrbCenterY(
            GetRelativePosition(startForward, startHeight, startRight)
        );

        Vector3 forward = FlattenForward(playerHead.forward);
        nebOrb.rotation = Quaternion.LookRotation(-forward, Vector3.up);
    }

    private IEnumerator MainSequence()
    {
        Vector3 growthStartPosition = ClampOrbCenterY(nebOrb.position);

        yield return GrowAndSpinUpPhase(
            growAndSpinUpDuration,
            growthStartPosition
        );

        if (fullSizeHoldBeforeFade > 0f)
            yield return HoldFullSize(fullSizeHoldBeforeFade);

        if (doppelgangerRoot != null)
        {
            CacheDoppelMaterials();
            PrepareDoppelInvisibleNoFlash();

            doppelgangerRoot.SetActive(true);

            if (keepDoppelCenteredInOrb)
                CenterDoppelgangerInOrb();

            if (facePlayerDuringFade)
                FaceDoppelTowardPlayer();

            onDoppelgangerFadeIn?.Invoke();

            yield return FadeInDoppelganger(doppelFadeDuration);
        }

        if (fullSizeHoldAfterFade > 0f)
            yield return HoldFullSize(fullSizeHoldAfterFade);

        onAbsorbStart?.Invoke();

        Vector3 shrinkTarget = GetShrinkTargetInFrontOfChest();

        yield return ShrinkInFrontOfChest(
            shrinkDuration,
            nebOrb.position,
            shrinkTarget
        );

        Vector3 chestTarget = GetChestAbsorbTarget();

        yield return AbsorbIntoChest(
            absorbDuration,
            nebOrb.position,
            chestTarget
        );

        // The orb visual is already hidden at the exact end of AbsorbIntoChest().
        // Lower the doppelganger only after the orb has completely disappeared.
        // Its facing direction is not changed here.
        if (lowerDoppelAfterAbsorb)
            yield return LowerDoppelToUserHeight(postAbsorbLowerDuration);

        sequenceCoroutine = null;
    }

    private IEnumerator GrowAndSpinUpPhase(
        float duration,
        Vector3 startingPosition
    )
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        Vector3 growthDirection = Vector3.ProjectOnPlane(
            startingPosition - playerHead.position,
            Vector3.up
        );

        if (growthDirection.sqrMagnitude < 0.0001f)
            growthDirection = FlattenForward(playerHead.forward);

        growthDirection.Normalize();

        float startingHorizontalDistance = Vector3.Distance(
            Vector3.ProjectOnPlane(startingPosition, Vector3.up),
            Vector3.ProjectOnPlane(playerHead.position, Vector3.up)
        );

        float startingRadius = Mathf.Max(
            0.001f,
            baseOrbRadiusMeters * scaleStart
        );

        float savedSurfaceGap = Mathf.Max(
            cameraSafetyMarginMeters,
            startingHorizontalDistance - startingRadius
        ) + growExtraSurfaceGap;

        // Keep the same vertical center throughout growth.
        // This prevents the radius from pushing the orb upward as it grows.
        float fixedCenterY = Mathf.Max(
            minimumOrbCenterY,
            startingPosition.y
        );

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float scaleProgress =
                scaleRamp.Evaluate(normalizedTime);

            float baseScale =
                Mathf.Lerp(scaleStart, scaleCharged, scaleProgress);

            float pulse =
                1f + GetHeartbeatPulse() * heartbeatStrength;

            float visualScale = baseScale * pulse;
            spinVisual.localScale = Vector3.one * visualScale;

            float currentRadius = Mathf.Max(
                0.001f,
                baseOrbRadiusMeters * visualScale
            );

            float requiredCenterDistance =
                savedSurfaceGap + currentRadius;

            Vector3 desiredPosition =
                playerHead.position
                + growthDirection * requiredCenterDistance;

            float hover =
                Mathf.Sin(Time.time * growHoverSpeed)
                * growHoverAmplitude;

            float desiredCenterY = fixedCenterY + hover;

            if (keepOrbAboveGround)
            {
                float groundSafeCenterY =
                    minimumOrbCenterY
                    + currentRadius
                    + orbGroundClearance;

                float maximumAllowedCenterY =
                    fixedCenterY
                    + Mathf.Max(0f, maximumGroundLiftDuringGrowth);

                groundSafeCenterY =
                    Mathf.Min(
                        groundSafeCenterY,
                        maximumAllowedCenterY
                    );

                desiredCenterY =
                    Mathf.Max(
                        desiredCenterY,
                        groundSafeCenterY
                    );
            }

            desiredPosition.y = desiredCenterY;
            desiredPosition = ClampOrbCenterY(desiredPosition);

            float smoothing =
                1f - Mathf.Exp(
                    -Mathf.Max(0.01f, growBackAwaySmoothing)
                    * Time.deltaTime
                );

            nebOrb.position = Vector3.Lerp(
                nebOrb.position,
                desiredPosition,
                smoothing
            );

            float spinProgress =
                spinRamp.Evaluate(normalizedTime);

            float spinSpeed =
                Mathf.Lerp(
                    spinIdle,
                    spinCharged,
                    spinProgress
                );

            SpinOrb(spinSpeed);

            yield return null;
        }

        spinVisual.localScale = Vector3.one * scaleCharged;
        nebOrb.position = ClampOrbCenterY(nebOrb.position);
    }

    private IEnumerator HoldFullSize(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SpinOrb(spinCharged);
            yield return null;
        }
    }

    private IEnumerator FadeInDoppelganger(float duration)
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float alpha =
                doppelFadeCurve.Evaluate(normalizedTime);

            SpinOrb(spinCharged);

            if (keepDoppelCenteredInOrb)
                CenterDoppelgangerInOrb();

            if (facePlayerDuringFade)
                FaceDoppelTowardPlayer();

            SetDoppelAlpha(alpha);

            yield return null;
        }

        SetDoppelAlpha(1f);

        if (facePlayerDuringFade)
            FaceDoppelTowardPlayer();
    }

    private void FaceDoppelTowardPlayer()
    {
        if (doppelgangerRoot == null || playerHead == null)
            return;

        Vector3 towardPlayer =
            playerHead.position
            - doppelgangerRoot.transform.position;

        towardPlayer.y = 0f;

        if (towardPlayer.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation =
            Quaternion.LookRotation(
                towardPlayer.normalized,
                Vector3.up
            );

        Quaternion modelOffset =
            Quaternion.Euler(
                0f,
                doppelFacingYawOffset,
                0f
            );

        doppelgangerRoot.transform.rotation =
            lookRotation * modelOffset;
    }

    private IEnumerator ShrinkInFrontOfChest(
        float duration,
        Vector3 startPosition,
        Vector3 targetPosition
    )
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float progress =
                shrinkCurve.Evaluate(normalizedTime);

            float visualScale =
                Mathf.Lerp(
                    scaleCharged,
                    smallOrbScale,
                    progress
                );

            Vector3 desiredPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            nebOrb.position =
                ClampOrbCenterY(desiredPosition);

            spinVisual.localScale =
                Vector3.one * visualScale;

            SpinOrb(spinCharged);

            // The doppelganger does not spin, lower, or rotate during shrink.
            yield return null;
        }

        nebOrb.position = ClampOrbCenterY(targetPosition);
        spinVisual.localScale = Vector3.one * smallOrbScale;
    }

    private IEnumerator AbsorbIntoChest(
        float duration,
        Vector3 startPosition,
        Vector3 chestPosition
    )
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float progress =
                Smooth01(normalizedTime);

            Vector3 desiredPosition =
                Vector3.Lerp(
                    startPosition,
                    chestPosition,
                    progress
                );

            nebOrb.position =
                ClampOrbCenterY(desiredPosition);

            float visualScale =
                Mathf.Lerp(
                    smallOrbScale,
                    0.01f,
                    progress
                );

            spinVisual.localScale =
                Vector3.one * visualScale;

            SpinOrb(spinCharged);

            yield return null;
        }

        nebOrb.position = ClampOrbCenterY(chestPosition);
        spinVisual.localScale = Vector3.one * 0.01f;

        // The orb keeps spinning for the entire chest-entry motion,
        // then disappears immediately when it reaches the chest.
        if (turnOffOnAbsorb != null)
            turnOffOnAbsorb.SetActive(false);
    }

    private Vector3 GetShrinkTargetInFrontOfChest()
    {
        if (doppelgangerChest == null)
            return GetChestAbsorbTarget();

        Vector3 towardPlayer =
            playerHead.position
            - doppelgangerChest.position;

        towardPlayer.y = 0f;

        if (towardPlayer.sqrMagnitude < 0.0001f)
            towardPlayer = -FlattenForward(playerHead.forward);

        towardPlayer.Normalize();

        Vector3 target =
            doppelgangerChest.position
            + towardPlayer * shrinkFrontOfChestMeters
            + Vector3.up * shrinkFrontOfChestHeight;

        return ClampOrbCenterY(target);
    }

    private Vector3 GetChestAbsorbTarget()
    {
        if (doppelgangerChest != null)
            return ClampOrbCenterY(doppelgangerChest.position);

        if (absorbTarget != null)
            return ClampOrbCenterY(absorbTarget.position);

        return ClampOrbCenterY(
            GetRelativePosition(
                absorbForward,
                absorbHeight,
                0f
            )
        );
    }

    private void CacheDoppelMaterials()
    {
        doppelMaterials.Clear();
        doppelBaseColors.Clear();

        if (doppelgangerRoot == null)
            return;

        Renderer[] renderers =
            doppelgangerRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rendererComponent in renderers)
        {
            Material[] materials =
                rendererComponent.materials;

            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                doppelMaterials.Add(material);
                doppelBaseColors.Add(
                    ReadMaterialColor(material)
                );

                ConfigureMaterialForTransparency(material);
            }
        }
    }

    private void PrepareDoppelInvisibleNoFlash()
    {
        if (keepDoppelCenteredInOrb)
            CenterDoppelgangerInOrb();

        if (facePlayerDuringFade)
            FaceDoppelTowardPlayer();

        SetDoppelAlpha(0f);
    }

    private void CenterDoppelgangerInOrb()
    {
        if (doppelgangerRoot == null)
            return;

        Vector3 orbCenter =
            nebOrb.position
            + nebOrb.TransformVector(doppelLocalOffset);

        if (doppelgangerChest != null)
        {
            Vector3 movement =
                orbCenter - doppelgangerChest.position;

            Vector3 newRootPosition =
                doppelgangerRoot.transform.position
                + movement;

            doppelgangerRoot.transform.position =
                ClampDoppelRootY(newRootPosition);
        }
        else
        {
            doppelgangerRoot.transform.position =
                ClampDoppelRootY(orbCenter);
        }
    }

    private void SetDoppelAlpha(float alpha)
    {
        float clampedAlpha =
            Mathf.Clamp01(alpha);

        for (int i = 0; i < doppelMaterials.Count; i++)
        {
            Material material =
                doppelMaterials[i];

            Color baseColor =
                doppelBaseColors[i];

            Color fadedColor =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    clampedAlpha
                );

            WriteMaterialColor(
                material,
                fadedColor
            );
        }
    }

    private static Color ReadMaterialColor(
        Material material
    )
    {
        if (material.HasProperty(BaseColorId))
            return material.GetColor(BaseColorId);

        if (material.HasProperty(ColorId))
            return material.GetColor(ColorId);

        return Color.white;
    }

    private static void WriteMaterialColor(
        Material material,
        Color color
    )
    {
        if (material.HasProperty(BaseColorId))
            material.SetColor(BaseColorId, color);
        else if (material.HasProperty(ColorId))
            material.SetColor(ColorId, color);
    }

    private static void ConfigureMaterialForTransparency(
        Material material
    )
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );
        }

        material.renderQueue = 3000;
    }

    private IEnumerator LowerDoppelToUserHeight(float duration)
    {
        if (
            doppelgangerRoot == null
            || doppelgangerChest == null
            || playerHead == null
        )
        {
            yield break;
        }

        float desiredChestY =
            Mathf.Max(
                minimumDoppelRootY,
                playerHead.position.y
                + doppelChestYOffsetFromHead
            );

        float verticalDifference =
            desiredChestY
            - doppelgangerChest.position.y;

        Vector3 startPosition =
            doppelgangerRoot.transform.position;

        Vector3 targetPosition =
            startPosition
            + Vector3.up * verticalDifference;

        targetPosition =
            ClampDoppelRootY(targetPosition);

        float safeDuration =
            Mathf.Max(0.0001f, duration);

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float progress =
                Smooth01(normalizedTime);

            doppelgangerRoot.transform.position =
                ClampDoppelRootY(
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        progress
                    )
                );

            yield return null;
        }

        doppelgangerRoot.transform.position =
            targetPosition;
    }

    private void SpinOrb(float degreesPerSecond)
    {
        Vector3 axis =
            spinAxisLocal.sqrMagnitude > 0.0001f
            ? spinAxisLocal.normalized
            : Vector3.up;

        spinVisual.localRotation *=
            Quaternion.AngleAxis(
                degreesPerSecond * Time.deltaTime,
                axis
            );
    }

    private float GetHeartbeatPulse()
    {
        float phase =
            Mathf.Repeat(
                Time.time * heartbeatRate,
                1f
            );

        return heartbeatCurve.Evaluate(phase);
    }

    private Vector3 GetRelativePosition(
        float forwardMeters,
        float heightMeters,
        float rightMeters
    )
    {
        Vector3 forward =
            FlattenForward(playerHead.forward);

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward
            ).normalized;

        return playerHead.position
               + forward * forwardMeters
               + Vector3.up * heightMeters
               + right * rightMeters;
    }

    private Vector3 ClampOrbCenterY(Vector3 position)
    {
        if (position.y < minimumOrbCenterY)
            position.y = minimumOrbCenterY;

        return position;
    }

    private Vector3 ClampDoppelRootY(Vector3 position)
    {
        if (position.y < minimumDoppelRootY)
            position.y = minimumDoppelRootY;

        return position;
    }

    private bool ValidateReferences()
    {
        if (nebOrb == null)
        {
            Debug.LogError(
                "[A_OrbIntroSequence] Assign Neb Orb."
            );

            return false;
        }

        if (playerHead == null)
        {
            Debug.LogError(
                "[A_OrbIntroSequence] Assign Player Head or ensure Camera.main exists."
            );

            return false;
        }

        return true;
    }

    private static Vector3 FlattenForward(Vector3 forward)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}