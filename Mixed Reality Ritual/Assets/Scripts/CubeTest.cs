using UnityEngine;

public class CubeTest : MonoBehaviour
{
    private float time = 11;
    private bool coming = true;
    private FadingUniversal fu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fu = GetComponent<FadingUniversal>();   
    }

    // Update is called once per frame
    void Update()
    {
        time+=Time.deltaTime;
        if (time>10)
        {
            if(coming)
            {
                fu.StartFadeRenderer(gameObject,10,1,0);
                coming = false;
            }
            else
            {
                fu.StartFadeRenderer(gameObject,10,0,1);
                coming = true;
            }
            time = 0;
        }
    }
}
