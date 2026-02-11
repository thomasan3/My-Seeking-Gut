using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [SerializeField] private GameObject passthrough;
    [SerializeField] private GameObject orb;
    [SerializeField] private GameObject orb_parent;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject volume;
    [SerializeField] private FadingUniversal fader;
    private float orb_fade_time = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
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
            orb_parent.SetActive(true);
            floor.SetActive(true);
            fader.StartFadeRenderer(orb,orb_fade_time,1,0);
            fader.StartFadeRenderer(floor,orb_fade_time,1,0);
        }

        if(state == "Constellation")
        {
            fader.StartFadeRenderer(floor,3,0);
        }
    }
}
