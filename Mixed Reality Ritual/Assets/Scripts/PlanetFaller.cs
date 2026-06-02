using UnityEngine;

public class PlanetFaller : MonoBehaviour
{

    private bool rising;
    private float velocity = 0f;
    private float acceleration = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
    }

    // Update is called once per frame
    void Update()
    {
        if(rising)
        {
            transform.position = transform.position + Vector3.up*velocity*Time.deltaTime;
            velocity+=acceleration*Time.deltaTime;
        }
    }

    void HandleStateChange(string state,float seconds)
    {
        if (state=="Fall")
        {
            rising = true;
        }
    }
}