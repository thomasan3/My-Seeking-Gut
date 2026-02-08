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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interval = 1f / frequency;
    }

    // Update is called once per frame
    void Update()
    {
        if(galaxy_ct < max_galaxies)
        {
            t += Time.deltaTime;
            if (t >= interval)
            {
                GameObject f = Instantiate(firmament, transform);
                f.transform.position = main_camera.position;
                f.GetComponent<ConstellationMovement>().main_camera = main_camera;
                t -= interval;
                galaxy_ct++;
            }
        }
    }
}
