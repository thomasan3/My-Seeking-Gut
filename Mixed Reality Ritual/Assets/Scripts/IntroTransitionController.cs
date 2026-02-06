using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class IntroTransitionController : MonoBehaviour
{
    [Header("Assign")]
    public MonoBehaviour faderComponent;     // drag PassthroughFaderUnderlay here (the component that has TogglePassthrough)
    public GameObject introVRWorld;          // INTRO_VR_WORLD
    public VideoPlayer nebulaVideo;          // VideoPlayer on NebulaScreen

    [Header("Backstage Control (Left Controller)")]
    public OVRInput.Button triggerButton = OVRInput.Button.Three; // X

    [Header("Timing")]
    public float revealVRDelay = 0.6f;      // wait while screen goes black
    public float cooldown = 1.5f;

    private bool started = false;
    private System.Reflection.MethodInfo toggleMethod;

    void Awake()
    {
        if (faderComponent != null)
            toggleMethod = faderComponent.GetType().GetMethod("TogglePassthrough");
    }

    void Start()
    {
        if (introVRWorld) introVRWorld.SetActive(false);
        if (nebulaVideo) nebulaVideo.Stop();
    }

    void Update()
    {
        if (started) return;

        if (OVRInput.GetDown(triggerButton, OVRInput.Controller.LTouch))
        {
            started = true;
            StartCoroutine(RunIntro());
        }
    }

    IEnumerator RunIntro()
    {
        // fade into black (passthrough -> black)
        toggleMethod?.Invoke(faderComponent, null);

        // wait until mostly black, then enable VR world + start nebula
        yield return new WaitForSeconds(revealVRDelay);

        if (introVRWorld) introVRWorld.SetActive(true);
        if (nebulaVideo) nebulaVideo.Play();

        yield return new WaitForSeconds(cooldown);
    }
}
