using UnityEngine;

public class PlanetaryMover : MonoBehaviour
{
    [SerializeField] private bool falls = true;
    private float acceleration = 1.5f;
    private float velocity = 0;
    private bool rising = false;
    private bool rotating = true;
    private float max_y = 10000;
    private Quaternion rotationStep;
    private float rotationSpeed = 1.7f;

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
    }
}
