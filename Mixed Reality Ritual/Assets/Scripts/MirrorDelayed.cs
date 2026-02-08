// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using static Meta.XR.Movement.MSDKUtility;
using static Meta.XR.Movement.Retargeting.CharacterRetargeterConfig;

namespace Meta.XR.Movement.Utils
{
    /// <summary>
    /// Component that mirrors transforms from a target hierarchy to this hierarchy.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class MirrorDelayed : MonoBehaviour
    {
        [BurstCompile]
        private struct GetPoseJob : IJobParallelForTransform
        {
            [WriteOnly]
            public NativeSlice<NativeTransform> DelayPoses;

            [ReadOnly]
            public bool IsLocal;

            [ReadOnly]
            public int Playhead;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                DelayPoses[index] = IsLocal
                    ? new NativeTransform(transform.localRotation, transform.localPosition, transform.localScale)
                    : new NativeTransform(transform.rotation, transform.position, transform.localScale);
            }
        }

        [BurstCompile]
        private struct CopyPoseJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeSlice<NativeTransform> DelayPoses;

            [ReadOnly]
            public int Playhead;

            [ReadOnly]
            public bool IsLocal;

            [ReadOnly]
            public bool MirrorPositions;

            [ReadOnly]
            public bool MirrorRotations;

            [ReadOnly]
            public bool MirrorScales;

            /// <inheritdoc cref="IJobParallelForTransform.Execute(int, TransformAccess)"/>
            [BurstCompile]
            public void Execute(int index, TransformAccess transform)
            {
                var bodyPose = DelayPoses[index];

                if (MirrorRotations)
                {
                    if (IsLocal)
                    {
                        transform.localRotation = bodyPose.Orientation;
                    }
                    else
                    {
                        transform.rotation = bodyPose.Orientation;
                    }
                }

                if (MirrorPositions && !(index==0))
                {
                    if (IsLocal)
                    {
                        transform.localPosition = bodyPose.Position;
                    }
                    else
                    {
                        transform.position = bodyPose.Position;
                    }
                }

                if (MirrorScales)
                {
                    transform.localScale = bodyPose.Scale;
                }
            }
        }

        /// <summary>
        /// The target transform hierarchy to mirror from.
        /// </summary>
        [SerializeField]
        private Transform _target;

        /// <summary>
        /// Whether to use local or world space transformations.
        /// When true, mirrors local position, rotation, and scale. When false, mirrors world position and rotation.
        /// </summary>
        [SerializeField]
        private bool _isLocal = true;

        /// <summary>
        /// Whether to use Unity's job system for better performance.
        /// When true, transform operations are processed in parallel using Burst-compiled jobs.
        /// </summary>
        private bool _useJobs = true;

        /// <summary>
        /// Whether to mirror position values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorPositions = true;

        /// <summary>
        /// Whether to mirror rotation values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorRotations = true;

        /// <summary>
        /// Whether to mirror scale values from the target transforms.
        /// </summary>
        [SerializeField]
        private bool _mirrorScales = true;

        [SerializeField]
        private float _delaySeconds = 2.0f; 

        [SerializeField]
        private float _refreshRate = 72.0f;

        [SerializeField]
        private GameObject[] _visibleComponents;

        /// <summary>
        /// Array of joint pairs that define the mapping between source and target transforms.
        /// Each pair contains a reference to a transform in this hierarchy and its corresponding transform in the target hierarchy.
        /// </summary>
        [SerializeField]
        private JointPair[] _bonePairs;


        private NativeArray<NativeTransform> _bonePoses;
        private NativeArray<NativeTransform> _delayPoses;
        private int _playhead = 0;

        private int _totalFrames = 0;
        private bool _bufferFilled = false;
        private TransformAccessArray _bones;
        private TransformAccessArray _targetBones;

        /// <summary>
        /// Initializes the transform arrays for mirroring when the component starts.
        /// </summary>
        public void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SetVisible(false);

            OVRPlugin.systemDisplayFrequency = _refreshRate;

            var bones = new Transform[_bonePairs.Length];
            var targetBones = new Transform[_bonePairs.Length];
            for (var i = 0; i < _bonePairs.Length; i++)
            {
                bones[i] = _bonePairs[i].Joint;
                targetBones[i] = _bonePairs[i].ParentJoint;
            }

            _bones = new TransformAccessArray(bones);
            _targetBones = new TransformAccessArray(targetBones);
            //_bonePoses = new NativeArray<NativeTransform>(
            //    _bonePairs.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            _delayPoses = new NativeArray<NativeTransform>(
                _bonePairs.Length * (int)(_delaySeconds * _refreshRate), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Updates the transforms each frame after all other updates have completed.
        /// </summary>
        public void LateUpdate()
        {
            if (_bonePairs == null || _bonePairs.Length == 0)
            {
                return;
            }

            if (Application.isPlaying && _useJobs && _totalFrames > _refreshRate)
            {
                UpdateJobs();
            }
            _totalFrames++;
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_bones.isCreated)
            {
                _bones.Dispose();
            }

            if (_targetBones.isCreated)
            {
                _targetBones.Dispose();
            }

            if (_bonePoses.IsCreated)
            {
                _bonePoses.Dispose();
            }

            if (_delayPoses.IsCreated)
            {
                _delayPoses.Dispose();
            }
        }

        /// <summary>
        /// Finds matching bone pairs between this hierarchy and the target hierarchy based on name.
        /// Uses a multi-stage matching approach: exact names first, then normalized names, then contains matching.
        /// </summary>
        private void FindBonePairs()
        {
            if (_target == null)
            {
                return;
            }

            var transforms = GetComponentsInChildren<Transform>();
            var targetTransforms = _target.GetComponentsInChildren<Transform>();
            var bonePairs = new List<JointPair>();

            // Always add root pair
            bonePairs.Add(new JointPair
            {
                Joint = transform,
                ParentJoint = _target
            });

            var unmatchedSources = new List<Transform>(transforms);
            var unmatchedTargets = new List<Transform>(targetTransforms);

            // Stage 1: Exact name matching
            for (int i = unmatchedSources.Count - 1; i >= 0; i--)
            {
                var source = unmatchedSources[i];
                for (int j = unmatchedTargets.Count - 1; j >= 0; j--)
                {
                    var target = unmatchedTargets[j];
                    if (source.name == target.name)
                    {
                        bonePairs.Add(new JointPair()
                        {
                            Joint = source,
                            ParentJoint = target
                        });
                        unmatchedSources.RemoveAt(i);
                        unmatchedTargets.RemoveAt(j);
                        break;
                    }
                }
            }

            // Check if we found matches for at least 50% of bones
            float matchPercentage = (float)bonePairs.Count / transforms.Length;
            if (matchPercentage < 0.5f && unmatchedSources.Count > 0)
            {
                // Stage 2: Normalized name matching (remove prefixes/suffixes and special characters)
                for (int i = unmatchedSources.Count - 1; i >= 0; i--)
                {
                    var source = unmatchedSources[i];
                    var normalizedSourceName = NormalizeBoneName(source.name);

                    for (int j = unmatchedTargets.Count - 1; j >= 0; j--)
                    {
                        var target = unmatchedTargets[j];
                        var normalizedTargetName = NormalizeBoneName(target.name);

                        if (normalizedSourceName == normalizedTargetName)
                        {
                            bonePairs.Add(new JointPair()
                            {
                                Joint = source,
                                ParentJoint = target
                            });
                            unmatchedSources.RemoveAt(i);
                            unmatchedTargets.RemoveAt(j);
                            break;
                        }
                    }
                }

                // Stage 3: Contains matching for remaining bones
                for (int i = unmatchedSources.Count - 1; i >= 0; i--)
                {
                    var source = unmatchedSources[i];
                    var normalizedSourceName = NormalizeBoneName(source.name);

                    for (int j = unmatchedTargets.Count - 1; j >= 0; j--)
                    {
                        var target = unmatchedTargets[j];
                        var normalizedTargetName = NormalizeBoneName(target.name);

                        if (normalizedTargetName.Contains(normalizedSourceName) || normalizedSourceName.Contains(normalizedTargetName))
                        {
                            bonePairs.Add(new JointPair()
                            {
                                Joint = source,
                                ParentJoint = target
                            });
                            unmatchedSources.RemoveAt(i);
                            unmatchedTargets.RemoveAt(j);
                            break;
                        }
                    }
                }
            }

            _bonePairs = bonePairs.ToArray();
        }

        /// <summary>
        /// Normalizes bone names by removing common prefixes, suffixes, and special characters.
        /// </summary>
        /// <param name="boneName">The original bone name</param>
        /// <returns>Normalized bone name for matching</returns>
        private string NormalizeBoneName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return string.Empty;

            string normalized = boneName.ToLowerInvariant();

            // Remove common prefixes (like character names followed by colon)
            int colonIndex = normalized.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < normalized.Length - 1)
            {
                normalized = normalized.Substring(colonIndex + 1);
            }

            // Remove common prefixes
            string[] commonPrefixes = { "mixamorig:", "chr_", "character_", "rig_", "bone_", "joint_" };
            foreach (var prefix in commonPrefixes)
            {
                if (normalized.StartsWith(prefix))
                {
                    normalized = normalized.Substring(prefix.Length);
                    break;
                }
            }

            // Remove common suffixes
            string[] commonSuffixes = { "_bone", "_joint", "_ctrl", "_control", ".001", ".002", ".003" };
            foreach (var suffix in commonSuffixes)
            {
                if (normalized.EndsWith(suffix))
                {
                    normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                    break;
                }
            }

            // Remove or replace special characters
            normalized = normalized.Replace("_", "").Replace("-", "").Replace(".", "").Replace(" ", "");

            return normalized;
        }

        public void SetVisible(bool visible)
        {
            foreach (GameObject obj in _visibleComponents)
            {
                obj.SetActive(visible);
            }
            
        }

        private void UpdateJobs()
        {
            
            int readhead = (_playhead + _bonePairs.Length) % _delayPoses.Length;

             if (!_bufferFilled && readhead == 0)
            {
                _bufferFilled = true;
                SetVisible(true);
            }

            var writeSlice = new NativeSlice<NativeTransform>(_delayPoses, _playhead, _bonePairs.Length);
            var readSlice  = new NativeSlice<NativeTransform>(_delayPoses, readhead,  _bonePairs.Length);

            var getBonesJob = new GetPoseJob
            {
                IsLocal = _isLocal,
                DelayPoses = writeSlice,
                Playhead = _playhead
            };

            var copyBonesJob = new CopyPoseJob
                {
                    IsLocal = _isLocal,
                    DelayPoses = readSlice,
                    MirrorPositions = _mirrorPositions,
                    MirrorRotations = _mirrorRotations,
                    MirrorScales = _mirrorScales,
                    Playhead = readhead
                };

            var getBonesJobHandle = getBonesJob.Schedule(_targetBones);
            copyBonesJob.Schedule(_bones, getBonesJobHandle).Complete();

            _playhead = readhead;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(MirrorDelayed)), UnityEditor.CanEditMultipleObjects]
        public class MirrorDelayedEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var MirrorDelayed = target as MirrorDelayed;
                if (GUILayout.Button("Find Bone Pairs"))
                {
                    if (MirrorDelayed != null)
                    {
                        UnityEditor.EditorUtility.SetDirty(MirrorDelayed);
                        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(MirrorDelayed);
                        MirrorDelayed.FindBonePairs();
                    }
                }

                if (GUILayout.Button("Manual Update"))
                {
                    if (MirrorDelayed != null)
                    {
                        MirrorDelayed.LateUpdate();
                        UnityEditor.EditorUtility.SetDirty(MirrorDelayed);
                    }
                }

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
#endif
    }
}
