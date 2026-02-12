using UnityEngine;

public class ConstellationSpawner : MonoBehaviour
{
    [SerializeField] private float frequency;
    [SerializeField] private GameObject firmament;
    [SerializeField] private Transform main_camera;
    [SerializeField] private int max_galaxies;
    private int galaxy_ct = 0;
    private float t = 0f;
    private float interval;
    private bool rising = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interval = 1f / frequency;
        EventManager.GameStateChange += HandleStateChange;
    }

    // Update is called once per frame
    void Update()
    {
        if(galaxy_ct < max_galaxies && !rising)
        {
            t += Time.deltaTime;
            if (t >= interval)
            {
                if(rising){return;}
                GameObject f = Instantiate(firmament, transform);
                f.transform.position = main_camera.position;
                f.GetComponent<ConstellationMovement>().main_camera = main_camera;
                t -= interval;
                galaxy_ct++;

                
                f.AddComponent<PlanetaryMover>();
            }
        }
    }

    void HandleStateChange(string state, float seconds)
    {
        if(state == "Fall")
        {
            rising = true;
        }
    }
}
