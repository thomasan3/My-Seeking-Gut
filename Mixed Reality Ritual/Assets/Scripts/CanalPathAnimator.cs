using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CanalPathAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [Tooltip("Assign the CenterEyeAnchor from your Meta Camera Rig.")]
    [SerializeField] private Transform centerEyeAnchor;

    [Header("Duration Settings")]
    [Tooltip("Total time in seconds the path animation should take.")]
    [SerializeField] private float durationInSeconds = 10f;
    [Tooltip("Speed profile along the track (0.0 = Start, 1.0 = End). Values around 1.0 are normal.")]
    [SerializeField] private AnimationCurve speedOverDistance = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Checkpoints")]
    [Tooltip("List of knot indexes that should trigger the CheckpointReached action.")]
    [SerializeField] private List<int> targetKnotIndexes = new List<int>();

    /// <summary>
    /// Event triggered whenever the animator reaches a knot index specified in 'targetKnotIndexes'.
    /// </summary>
    public static Action CheckpointReached;

    private float currentDistance = 0f;
    private bool isPlaying = false;
    private float totalLength;
    private float speedCorrectionFactor = 1f;

    private readonly List<float> knotNormalizedPositions = new List<float>();
    private int nextKnotIndex = 0;

    private void LateUpdate()
    {
        if (splineContainer == null || !isPlaying) return;
        if (totalLength <= 0f) return;

        // 1. Calculate normalized progress (0.0 to 1.0)
        float normProgress = Mathf.Clamp01(currentDistance / totalLength);

        // 2. Check checkpoint triggers
        CheckKnotReached(normProgress);

        // 3. Compute speed in meters per second
        float baseSpeed = totalLength / Mathf.Max(0.01f, durationInSeconds);
        float relativeCurveValue = Mathf.Max(0.01f, speedOverDistance.Evaluate(normProgress));
        float speed = relativeCurveValue * baseSpeed * speedCorrectionFactor;

        // 4. Advance distance along path
        currentDistance += speed * Time.deltaTime;

        // 5. Compute new world position on Spline
        float updatedNorm = Mathf.Clamp01(currentDistance / totalLength);
        
        // SplineContainer.EvaluatePosition returns World Space position
        Vector3 targetSplinePos = splineContainer.EvaluatePosition(updatedNorm);

        // 6. Update position cleanly (Knot 0 was snapped to head position, so zero offset needed)
        transform.position = targetSplinePos;

        // 7. Check animation completion
        if (updatedNorm >= 1.0f)
        {
            isPlaying = false;
        }
    }

    public void Play()
    {
        if (splineContainer == null) return;

        // 1. Align Knot 0 directly to the VR Player's current position to eliminate initial snap
        SnapKnotZeroToPlayer();

        // 2. Calculate total length after modifying Knot 0
        totalLength = splineContainer.CalculateLength();
        if (totalLength <= 0f) return;

        // 3. Cache knot thresholds along the updated spline
        CacheKnotPositions();

        // 4. Calculate exact curve scaling factor relative to durationInSeconds
        speedCorrectionFactor = CalculateRequiredSpeedMultiplier(durationInSeconds,totalLength);

        currentDistance = 0f;
        isPlaying = true;
    }

    private void SnapKnotZeroToPlayer()
    {
        if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count == 0) return;

        Vector3 targetWorldPosition = transform.position;

        // Use head anchor position if assigned
        if (centerEyeAnchor != null)
        {
            targetWorldPosition = centerEyeAnchor.position;
        }

        // Convert world target position to the SplineContainer's local space
        Vector3 localTargetPos = splineContainer.transform.InverseTransformPoint(targetWorldPosition);

        // Modify Knot 0
        Spline spline = splineContainer.Spline;
        BezierKnot knot0 = spline[0];
        knot0.Position = (Unity.Mathematics.float3)localTargetPos;
        spline[0] = knot0;
    }

    private void CacheKnotPositions()
    {
        knotNormalizedPositions.Clear();
        nextKnotIndex = 0;

        int knotCount = splineContainer.Spline.Count;

        for (int i = 0; i < knotCount; i++)
        {
            float normPos = SplineUtility.GetNormalizedInterpolation(
                splineContainer.Spline, 
                i, 
                PathIndexUnit.Knot
            );
            
            knotNormalizedPositions.Add(normPos);
        }
    }

    private void CheckKnotReached(float currentNormProgress)
    {
        while (nextKnotIndex < knotNormalizedPositions.Count && currentNormProgress >= knotNormalizedPositions[nextKnotIndex])
        {
            OnKnotReached(nextKnotIndex, splineContainer.Spline[nextKnotIndex]);
            nextKnotIndex++;
        }
    }

    private void OnKnotReached(int knotIndex, BezierKnot knot)
    {
        if (targetKnotIndexes.Contains(knotIndex))
        {
            CheckpointReached?.Invoke();
            Debug.Log($"Checkpoint Reached! Knot Index: {knotIndex}");
        }
    }

    /// <summary>
    /// Calculates the inverse curve scale factor to guarantee exact duration timing.
    /// </summary>
    private float CalculateRequiredSpeedMultiplier(float targetDuration, float trackLength)
    {
        if (targetDuration <= 0f || trackLength <= 0f) return 1f;

        float baseSpeed = trackLength / targetDuration;
        int samples = 500;
        float dx = trackLength / samples; // Distance per step in meters
        float totalSimulatedTime = 0f;

        for (int i = 0; i < samples; i++)
        {
            float progress = (i + 0.5f) / samples;
            // Evaluate curve shape
            float relativeCurveSpeed = Mathf.Max(0.01f, speedOverDistance.Evaluate(progress));
            
            float stepSpeed = relativeCurveSpeed * baseSpeed;
            
            // dt = dx / v
            totalSimulatedTime += dx / stepSpeed;
        }

        // Ratio of how long the unscaled curve took vs how long we want it to take:
        // If simulated time took 4s, but target is 10s, return 4/10 = 0.4x speed.
        return totalSimulatedTime / targetDuration;
    }
}