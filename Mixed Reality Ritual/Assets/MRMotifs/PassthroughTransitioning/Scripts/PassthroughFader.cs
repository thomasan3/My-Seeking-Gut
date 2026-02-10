// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using MRMotifs.SharedAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MRMotifs.PassthroughTransitioning
{
    [MetaCodeSample("MRMotifs-PassthroughTransitioning")]
    public class PassthroughFader : MonoBehaviour
    {
        private enum FadeDirection { Normal, RightToLeft, TopToBottom, InsideOut }
        private enum PassthroughViewingMode { Underlay, Selective }
        private enum FaderState { MR, VR, InTransition }

        private FaderState State => Mathf.Approximately(m_targetAlpha, 1f) ? FaderState.MR :
            Mathf.Approximately(m_targetAlpha, 0f) ? FaderState.VR :
            FaderState.InTransition;

        [Header("Passthrough Fader Settings")]
        [SerializeField] private PassthroughViewingMode passthroughViewingMode = PassthroughViewingMode.Selective;
        [Range(0.01f, 100f)]
        [SerializeField] private float selectiveDistance = 5f;
        [SerializeField] private float fadeSpeed = 1f;
        [SerializeField] private FadeDirection fadeDirection = FadeDirection.TopToBottom;

        [Header("Fade Events")]
        [SerializeField] private UnityEvent onStartFadeIn = new();
        [SerializeField] private UnityEvent onStartFadeOut = new();
        [SerializeField] private UnityEvent onFadeInComplete = new();
        [SerializeField] private UnityEvent onFadeOutComplete = new();

        [Header("Input")]
        [SerializeField] private bool allowUIFallback = true;
        [SerializeField] private bool enableControllerToggle = true;
        [SerializeField] private OVRInput.RawButton controllerToggleRaw = OVRInput.RawButton.X;
        [SerializeField] private bool blockWhileInTransition = true;

        [Header("VR World Toggle")]
        [SerializeField] private GameObject introVrWorld;                  // drag INTRO_VR_WORLD here
        [SerializeField] private bool startInPassthrough = true;
        [SerializeField] private bool disableVrWorldWhenInPassthrough = true;

        [Header("Auto Transition On Start")]
        [SerializeField] private bool autoTransitionToVR = true;          // turn this on
        [SerializeField] private float autoTransitionDelay = 10f;          // 10 seconds default

        // internal flags
        private bool m_enableVrWorldAfterFadeOut = false;

        private OVRPassthroughLayer m_oVRPassthroughLayer;
        private Camera m_mainCamera;
        private Material m_material;
        private MeshRenderer m_meshRenderer;
        private MenuPanel m_menuPanel;
        private Button m_passthroughButton;
        private Color m_skyboxBackgroundColor;
        private float m_targetAlpha;
        private const float FADE_TOLERANCE = 0.001f;

        private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");
        private static readonly int s_direction = Shader.PropertyToID("_FadeDirection");

        private void Awake()
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera != null)
                m_skyboxBackgroundColor = m_mainCamera.backgroundColor;

            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

            m_meshRenderer = GetComponent<MeshRenderer>();
            if (m_meshRenderer != null)
                m_material = m_meshRenderer.material;

            m_menuPanel = FindAnyObjectByType<MenuPanel>();
            if (allowUIFallback && m_menuPanel != null)
            {
                m_passthroughButton = m_menuPanel.PassthroughFaderButton;
                if (m_passthroughButton != null)
                    m_passthroughButton.onClick.AddListener(TogglePassthrough);
            }

            m_oVRPassthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
            m_oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);

            SetupPassthrough();

            if (startInPassthrough)
                ForceStartInMR();

#if UNITY_ANDROID
            CheckIfPassthroughIsRecommended();
#endif
        }

        private void Start()
        {
            // Auto transition after X seconds
            if (startInPassthrough && autoTransitionToVR)
                StartCoroutine(AutoTransitionRoutine());
        }

        private IEnumerator AutoTransitionRoutine()
        {
            yield return new WaitForSeconds(autoTransitionDelay);

            // Only run if we're still in MR and not already transitioning
            if (State == FaderState.MR)
                TransitionToVR();
        }

        private void Update()
        {
            if (!enableControllerToggle) return;
            if (blockWhileInTransition && State == FaderState.InTransition) return;

            if (OVRInput.GetDown(controllerToggleRaw))
                TogglePassthrough();
        }

        private void OnDestroy()
        {
            if (m_passthroughButton != null)
                m_passthroughButton.onClick.RemoveListener(TogglePassthrough);

            if (m_oVRPassthroughLayer != null)
                m_oVRPassthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
        }

        private void ForceStartInMR()
        {
            m_oVRPassthroughLayer.enabled = true;
            m_targetAlpha = 1f;

            if (m_material != null)
                m_material.SetFloat(s_invertedAlpha, 1f);

            if (passthroughViewingMode == PassthroughViewingMode.Underlay && m_mainCamera != null)
            {
                m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                m_mainCamera.backgroundColor = Color.clear;
            }

            if (introVrWorld != null && disableVrWorldWhenInPassthrough)
                introVrWorld.SetActive(false);
        }

        private void SetupPassthrough()
        {
            if (passthroughViewingMode == PassthroughViewingMode.Underlay)
            {
                var maxCamView = m_mainCamera.farClipPlane - 0.01f;
                transform.localScale = new Vector3(maxCamView, maxCamView, maxCamView);
                m_meshRenderer.enabled = false;
            }
            else
            {
                transform.localScale = new Vector3(selectiveDistance, selectiveDistance, selectiveDistance);
                m_meshRenderer.enabled = true;
            }
        }

        private void CheckIfPassthroughIsRecommended()
        {
            if (m_mainCamera == null) return;

            if (OVRManager.IsPassthroughRecommended())
            {
                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    m_mainCamera.backgroundColor = Color.clear;
                }
                else
                {
                    m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                    m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                }

                m_material.SetFloat(s_invertedAlpha, 1);
            }
            else
            {
                m_oVRPassthroughLayer.enabled = false;
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                m_material.SetFloat(s_invertedAlpha, 0);
            }
        }

        // ---- NEW: explicit transition helpers ----
        public void TransitionToVR()
        {
            UpdateFadeDirection();

            // Enable VR world only AFTER fade finishes (so it’s “fade to black → reveal”)
            m_enableVrWorldAfterFadeOut = true;

            if (passthroughViewingMode == PassthroughViewingMode.Underlay)
            {
                m_meshRenderer.enabled = true;
                m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
            }

            m_targetAlpha = 0f;
            onStartFadeOut?.Invoke();
            StopAllCoroutines();
            StartCoroutine(FadeToTarget());
        }

        public void TransitionToMR()
        {
            UpdateFadeDirection();

            m_enableVrWorldAfterFadeOut = false;

            m_oVRPassthroughLayer.enabled = true;
            onStartFadeIn?.Invoke();
            StopAllCoroutines();
            StartCoroutine(FadeToTarget());
        }

        public void TogglePassthrough()
        {
            switch (State)
            {
                case FaderState.MR:
                    TransitionToVR();
                    break;

                case FaderState.VR:
                    TransitionToMR();
                    break;

                case FaderState.InTransition:
                    // optional: ignore mid-transition toggles
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateFadeDirection()
        {
            if (m_material != null)
                m_material.SetInt(s_direction, (int)fadeDirection);
        }

        private void OnPassthroughLayerResumed(OVRPassthroughLayer passthroughLayer)
        {
            if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                m_meshRenderer.enabled = true;

            m_targetAlpha = 1;
            StopAllCoroutines();
            StartCoroutine(FadeToTarget());
        }

        private IEnumerator FadeToTarget()
        {
            var currentAlpha = m_material.GetFloat(s_invertedAlpha);

            while (Mathf.Abs(currentAlpha - m_targetAlpha) > FADE_TOLERANCE)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, m_targetAlpha, fadeSpeed * Time.deltaTime);
                m_material.SetFloat(s_invertedAlpha, currentAlpha);
                yield return null;
            }

            // If we ended at MR (passthrough visible)
            if (Mathf.Abs(m_targetAlpha - 1f) < FADE_TOLERANCE)
            {
                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    m_mainCamera.backgroundColor = Color.clear;
                }

                onFadeInComplete?.Invoke();

                if (disableVrWorldWhenInPassthrough && introVrWorld != null)
                    introVrWorld.SetActive(false);
            }
            else
            {
                // We ended at VR (passthrough fully gone)
                m_oVRPassthroughLayer.enabled = false;

                if (passthroughViewingMode == PassthroughViewingMode.Underlay)
                {
                    m_mainCamera.clearFlags = CameraClearFlags.Skybox;
                    m_mainCamera.backgroundColor = m_skyboxBackgroundColor;
                }

                onFadeOutComplete?.Invoke();

                // IMPORTANT: turn on VR world here (after fade-out completed)
                if (m_enableVrWorldAfterFadeOut && introVrWorld != null)
                    introVrWorld.SetActive(true);
            }

            m_meshRenderer.enabled = (passthroughViewingMode != PassthroughViewingMode.Underlay);
        }
    }
}
