using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ConstellationManager : MonoBehaviour
{

    [SerializeField] private GameObject constellationScene;
    [SerializeField] private float planetWaitSeconds;
    [SerializeField] private float lightFadeSeconds;
    [SerializeField] private GameObject lights;
    [SerializeField] private GameObject ellipseMaker;
    [SerializeField] private FadingUniversal fu;
    [SerializeField] private float planetFadeSeconds;
    [SerializeField] private GameObject starbox;
    [SerializeField] private GameObject ritObj;

    private bool rising = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleStateChange(string state, float seconds)
    {
        if(state=="Constellation")
        {
            transform.position = Vector3.Scale(Camera.main.transform.position,new Vector3(1,0,1));

            constellationScene.SetActive(true);
            for (int i = 0; i < constellationScene.transform.childCount;i++)
            {
                GameObject ch = constellationScene.transform.GetChild(i).gameObject;
                ch.SetActive(false);
            }

            StartCoroutine(ConstellationFader());

            lights.SetActive(true);
            for (int i = 0; i < lights.transform.childCount;i++)
            {
                GameObject ch = lights.transform.GetChild(i).gameObject;
                fu.StartFadeLighting(ch,lightFadeSeconds,1f,0f);
            }
        }

        if(state == "Stars")
        {
            
            starbox.SetActive(true);
            fu.StartFadeRenderer(starbox,30f,1);
        }


        if(state=="Planet Movement")
        {
            //ellipseMaker.SetActive(true);
        }

        if(state == "Fan")
        {
            ritObj.SetActive(true);
        }


        if(state=="Stop Planet")
        {
            rising=true;
        }
    }

    private IEnumerator ConstellationFader()
    {
        int cc = constellationScene.transform.childCount;
        List<int> randomIndexes = Enumerable.Range(0, cc).OrderBy(x => System.Guid.NewGuid()).ToList();
        for (int i = 0; i < cc;i++)
        {
            GameObject ch = constellationScene.transform.GetChild(randomIndexes[i]).gameObject;
            if(rising){yield break;}
            ch.SetActive(true);
            ch.AddComponent<PlanetaryMover>();
            fu.StartFadeRenderer(ch,planetFadeSeconds,1,0);//Mathf.Min(Random.Range(0.8f,1.5f),1f),0);
            float t = 0f;
            while(t < planetWaitSeconds)
            {
                
                t+=Time.deltaTime;
                yield return null;
            }
        }
    }
}
