using Meta.XR.Movement.Utils;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [SerializeField] private GameObject passthrough;
    [SerializeField] private GameObject orb;
    [SerializeField] private GameObject orb_parent;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject volume;
    [SerializeField] private FadingUniversal fader;
    [SerializeField] private GameObject dop;
    [SerializeField] private GameObject dopMesh;
    [SerializeField] private GameObject dopBones;

    [SerializeField] private GameObject dop_light;
    [SerializeField] private OVRScreenFade screenFade;
    [SerializeField] private GameObject whiteFadeBall;

    [SerializeField] private OVRManager ovrManager;
    [SerializeField] private GameObject canalOpenning;
    [SerializeField] private GameObject canalSecondary;
    [SerializeField] private CanalPathAnimator canalPathAnimator;
    private float obj_fade_time = 7f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
        dop.SetActive(false);
        floor.SetActive(false);
        dop_light.SetActive(false);
        canalOpenning.SetActive(false);
        canalSecondary.SetActive(false);
        whiteFadeBall.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
    }

    void HandleStateChange(string state,float seconds)
    {
        if(state == "Intro Passthrough Fade")
        {
            fader.StartFadePassthrough(passthrough,seconds,0);
        }

        if(state == "Orb")
        {
            dop.SetActive(true);
            //orb_parent.SetActive(true);
            floor.SetActive(true);
            dop_light.SetActive(true);
            //fader.StartFadeRenderer(orb,orb_fade_time,1,0);
            fader.StartFadeRenderer(floor,obj_fade_time,1f,0f);
            fader.StartFadeRenderer(dopMesh,obj_fade_time,1f,0f);
            fader.StartFadeLighting(dop_light,obj_fade_time*.6f,1f,0f);
        }

        if(state == "Constellation")
        {
            fader.StartFadeRenderer(floor,5f,0f,1f,true);
            fader.StartFadeVolume(volume,5f,0f,1f);
        }

        

        if(state == "Dopple Fade")
        {
            dopBones.GetComponent<MirrorDelayed>().SyncUp();
            fader.StartFadeRenderer(dopMesh,obj_fade_time*2,0f,1f);
        }

        if(state == "Fall")
        {
            //ovrManager.usePositionTracking = false;
            canalOpenning.SetActive(true);
            canalPathAnimator.Play();
        }

        if(state == "CanalRender")
        {
            canalSecondary.SetActive(true); 
        }

        if(state == "Whiteness")
        {
            whiteFadeBall.SetActive(true);
            whiteFadeBall.GetComponent<VRWhiteFadeSimple>().StartFade(Color.white,3f);
            //screenFade.fadeColor = Color.white;
            //screenFade.FadeIn();
        }

        if(state == "Darkness End")
        {
            whiteFadeBall.SetActive(true);
            whiteFadeBall.GetComponent<VRWhiteFadeSimple>().StartFade(Color.black,3f);
            //screenFade.fadeColor = Color.black;
            //screenFade.FadeIn();
        }

       

        if(state == "End")
        {
            fader.StartFadePassthrough(passthrough,seconds,1);
        }
    }
}
