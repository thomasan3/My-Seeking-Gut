using UnityEngine;

public class PlanetaryMover : MonoBehaviour
{
    private float acceleration = 1.5f;
    private float velocity = 0;
    private bool rising = false;
    private bool rotating = false;
    private float max_y = 10000;
    private Quaternion rotationStep;
    private float rotationSpeed = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
        
        Vector3 randomAxis = Random.onUnitSphere;
        rotationStep = Quaternion.AngleAxis(rotationSpeed, randomAxis);
    }

    // Update is called once per frame
    void Update()
    {
        if(rising)
        {
            if(transform.position.y > max_y){Destroy(gameObject);}
            transform.position = transform.position + Vector3.up*velocity*Time.deltaTime;
            velocity+=acceleration*Time.deltaTime;
        }
        if(rotating)
        {
            transform.rotation *= Quaternion.Slerp(Quaternion.identity, rotationStep, Time.deltaTime);
        }
    }

    void HandleStateChange(string state,float seconds)
    {
        if (state=="Planet Movement")
        {
            rotating = true;
        }
        if (state=="Fall")
        {
            rising = true;
        }
    }
}
