using System.Collections;
using UnityEngine;

/// <summary>
/// Starts the experience in Quest passthrough, waits for an adjustable amount
/// of time, fades to black, switches into the virtual world, fades back in,
/// and then enables the existing OrbLureController.
///
/// SETUP:
/// 1. Put this script on an always-active empty GameObject.
/// 2. Assign the OVRPassthroughLayer from the Camera Rig.
/// 3. Assign a black fade sphere/quad Renderer parented to CenterEyeAnchor.
/// 4. Assign the root containing the virtual environment.
/// 5. Assign the OrbLureController component.
/// 6. Disable OrbLureController in the Inspector before pressing Play/building.
/// </summary>
public class PassthroughIntroController : MonoBehaviour
{
    [Header("Passthrough")]
    [Tooltip("Assign the OVRManager component from the Camera Rig.")]
    public OVRManager ovrManager;

    [Tooltip("Assign the OVRPassthroughLayer component or Passthrough Building Block.")]
    public OVRPassthroughLayer passthroughLayer;

    [Header("Timing")]
    [Tooltip("How long the participant remains in passthrough before the transition begins.")]
    public float passthroughDuration = 30f;

    [Tooltip("How long it takes to fade from passthrough to full black.")]
    public float fadeToBlackDuration = 1.5f;

    [Tooltip("How long the screen stays completely black while the worlds switch.")]
    public float blackHoldDuration = 0.5f;

    [Tooltip("How long it takes to fade from black into the virtual scene.")]
    public float fadeFromBlackDuration = 1.5f;

    [Header("Black Fade Object")]
    [Tooltip("Renderer of a black sphere or quad attached to CenterEyeAnchor.")]
    public Renderer fadeRenderer;

    [Tooltip("Name of the color property used by the fade material. URP usually uses _BaseColor.")]
    public string fadeColorProperty = "_BaseColor";

    [Header("Virtual Experience")]
    [Tooltip("Root GameObject containing the virtual environment. Do not include the Camera Rig or this controller.")]
    public GameObject virtualWorldRoot;

    [Tooltip("Assign the existing OrbLureController component. Leave that component disabled in the Inspector.")]
    public MonoBehaviour orbLureController;

    [Tooltip("Optional extra objects that must remain hidden until VR begins.")]
    public GameObject[] objectsHiddenDuringPassthrough;

    [Header("Testing")]
    [Tooltip("Skip the wait when testing the transition in the Unity Editor or headset.")]
    public bool skipPassthroughWait = false;

    [Tooltip("Press the X button on the left Touch controller to start the transition early.")]
    public bool allowLeftXToSkip = true;

    private Material fadeMaterial;
    private int fadeColorId;
    private bool transitionStarted;

    private void Awake()
    {
        if (fadeRenderer != null)
        {
            // Renderer.material creates a runtime-only copy so the project asset
            // is not permanently altered by the fade.
            fadeMaterial = fadeRenderer.material;
            fadeColorId = Shader.PropertyToID(fadeColorProperty);
        }
    }

    private void Start()
    {
        PrepareStartingState();
        StartCoroutine(PassthroughOpening());
    }

    private void Update()
    {
        if (
            allowLeftXToSkip
            && !transitionStarted
            && OVRInput.GetDown(OVRInput.RawButton.X)
        )
        {
            transitionStarted = true;
        }
    }

    private void PrepareStartingState()
    {
        // Passthrough must be enabled in BOTH OVRManager and the layer.
        if (ovrManager == null)
            ovrManager = OVRManager.instance;

        if (ovrManager != null)
            ovrManager.isInsightPassthroughEnabled = true;

        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = true;
            passthroughLayer.hidden = false;
        }

        Debug.Log("[PassthroughIntroController] Starting in passthrough.");

        // The fade object remains active, but starts fully transparent.
        if (fadeRenderer != null)
        {
            fadeRenderer.gameObject.SetActive(true);
            SetFadeAlpha(0f);
        }

        // Hide the virtual world during the real-room opening.
        if (virtualWorldRoot != null)
            virtualWorldRoot.SetActive(false);

        if (objectsHiddenDuringPassthrough != null)
        {
            foreach (GameObject objectToHide in objectsHiddenDuringPassthrough)
            {
                if (objectToHide != null)
                    objectToHide.SetActive(false);
            }
        }

        // Prevent OrbLureController.Start() from running until the VR reveal.
        if (orbLureController != null)
            orbLureController.enabled = false;
    }

    private IEnumerator PassthroughOpening()
    {
        if (!skipPassthroughWait)
        {
            float elapsed = 0f;

            while (elapsed < passthroughDuration && !transitionStarted)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        transitionStarted = true;

        // Cover the passthrough view.
        yield return Fade(0f, 1f, fadeToBlackDuration);

        if (blackHoldDuration > 0f)
            yield return new WaitForSeconds(blackHoldDuration);

        // Switch worlds only while the participant sees complete black.
        if (passthroughLayer != null)
        {
            passthroughLayer.hidden = true;
            passthroughLayer.enabled = false;
        }

        if (ovrManager != null)
            ovrManager.isInsightPassthroughEnabled = false;

        Debug.Log("[PassthroughIntroController] Passthrough disabled; enabling VR world.");

        if (virtualWorldRoot != null)
            virtualWorldRoot.SetActive(true);

        if (objectsHiddenDuringPassthrough != null)
        {
            foreach (GameObject objectToShow in objectsHiddenDuringPassthrough)
            {
                if (objectToShow != null)
                    objectToShow.SetActive(true);
            }
        }

        // Reveal the virtual world.
        yield return Fade(1f, 0f, fadeFromBlackDuration);

        // The existing lure script now starts normally.
        // Unity calls its Start() the first time the component becomes enabled.
        if (orbLureController != null)
        {
            orbLureController.enabled = true;
            Debug.Log("[PassthroughIntroController] Orb lure enabled.");
        }

        // Hide the fade geometry after it becomes transparent.
        if (fadeRenderer != null)
            fadeRenderer.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (fadeMaterial == null)
            yield break;

        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        SetFadeAlpha(startAlpha);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / safeDuration);

            float smoothTime =
                normalizedTime
                * normalizedTime
                * (3f - 2f * normalizedTime);

            SetFadeAlpha(
                Mathf.Lerp(startAlpha, endAlpha, smoothTime)
            );

            yield return null;
        }

        SetFadeAlpha(endAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeMaterial == null)
            return;

        Color color = Color.black;

        if (fadeMaterial.HasProperty(fadeColorId))
            color = fadeMaterial.GetColor(fadeColorId);
        else if (fadeMaterial.HasProperty("_Color"))
            color = fadeMaterial.GetColor("_Color");

        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = Mathf.Clamp01(alpha);

        if (fadeMaterial.HasProperty(fadeColorId))
            fadeMaterial.SetColor(fadeColorId, color);
        else if (fadeMaterial.HasProperty("_Color"))
            fadeMaterial.SetColor("_Color", color);
    }
}