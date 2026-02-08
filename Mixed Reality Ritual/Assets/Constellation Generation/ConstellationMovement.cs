using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ConstellationMovement : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private bool debug_mode = false;
    [SerializeField] private GameObject debug_mesh = null;
    [SerializeField] private int orbitals = 3;
    [SerializeField] private int min_planet_count = 3;
    [SerializeField] private int max_planet_count = 10;
    public Transform main_camera;
    [SerializeField] private GameObject[] planet_prefabs;

    //Possible Values
    [Header("Focal Distance")]
    [SerializeField] private float base_dist = 2;
    [SerializeField] private float dist_growth_factor = 3;
    [SerializeField] private float dist_scatter = 0.2f;
    [SerializeField] private float dist_scatter_growth_factor = 1.3f;

    [Header("Minor Axis")]
    [SerializeField] private float base_radius = 5;
    [SerializeField] private float radius_growth_factor = 3;
    [SerializeField] private float radius_scatter = 0.2f;
    [SerializeField] private float radius_scatter_growth_factor = 1.3f;

    [Header("Period")]
    [SerializeField] private float base_period = 5;
    [SerializeField] private float period_growth_factor = 5;
    [SerializeField] private float period_scatter = 0.5f;
    [SerializeField] private float period_scatter_growth_factor = 1.1f;

    [Header("Astronomical Unit (Planet Distance)")]
    [SerializeField] private float base_au = 1.3f;
    [SerializeField] private float au_growth_factor = 3;
    [SerializeField] private float au_scatter = 0.2f;
    [SerializeField] private float au_scatter_growth_factor = 1.3f;

    [Header("Planet Size")]
    [SerializeField] private float base_size = 1.1f;
    [SerializeField] private float size_growth_factor = 4;
    [SerializeField] private float size_scatter = 0.3f;
    [SerializeField] private float size_scatter_growth_factor = 1.4f;

    [Header("Year (Planet Period)")]
    [SerializeField] private float base_year = 4;
    [SerializeField] private float year_growth_factor = 6;
    [SerializeField] private float year_scatter = 0.3f;
    [SerializeField] private float year_scatter_growth_factor = 1.3f;
    
    //Instance Values
    private Vector3 _origin;
    private Vector3 _direction;
    private Vector3 _perpendicular;
    private int _orbital;
    private float _dist;
    private float _radius;
    private float _period;

    private float _year;

    private float _phase = 0f;
    private float _phase_year = 0f;
    private GameObject _galaxy;

    private Quaternion _rot;


    private float _omega;
    private float _omega_year;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _origin = transform.position;
        _orbital = Random.Range(0,orbitals);

        _dist = QuadPick(base_dist,dist_growth_factor,dist_scatter,dist_scatter_growth_factor);
        _radius = QuadPick(base_radius,radius_growth_factor,radius_scatter,radius_scatter_growth_factor);
        _period = QuadPick(base_period,period_growth_factor,period_scatter,period_scatter_growth_factor);
        _year = QuadPick(base_year,year_growth_factor,year_scatter,year_scatter_growth_factor);

        _direction = Random.onUnitSphere;
        _perpendicular = RandomPerpendicular(_direction);
        _rot = Random.rotationUniform;

        _galaxy = new GameObject("Galaxy");
        _galaxy.transform.SetParent(transform, false);

        if(debug_mode)
        {
            Instantiate(debug_mesh,transform);
            Instantiate(debug_mesh,_galaxy.transform);
        }
        
        if (Vector3.Dot(main_camera.forward, _direction) > 0f)
        {
            _direction = -_direction;
        }

        transform.position += _direction * _dist;
        _galaxy.transform.localPosition = _direction * _radius;

        for(int p = 0; p < Random.Range(min_planet_count,max_planet_count+1); p++)
        {
            GeneratePlanet(_galaxy);
        }

        _omega = 2f * Mathf.PI / _period;
        _omega_year = 2f * Mathf.PI / _year;
    }

    void GeneratePlanet(GameObject parent)
    {
        float _size = QuadPick(base_size,size_growth_factor,size_scatter,size_scatter_growth_factor);
        float _au = QuadPick(base_au,au_growth_factor,au_scatter,au_scatter_growth_factor);

        Vector3 planet_dir = Random.onUnitSphere;

        GameObject p = Instantiate(planet_prefabs[Random.Range(0, planet_prefabs.Length)],parent.transform);
        p.transform.localScale *= _size;
        p.transform.SetLocalPositionAndRotation(_au * planet_dir, Random.rotationUniform);
    }

    float QuadPick(float b, float g, float s, float sg)
    {
        b *= (float)Math.Pow(g,_orbital);
        s *= (float)Math.Pow(sg,_orbital);

        return b * ( 1f + Random.Range(-s,s));
    }

    Vector3 RandomPerpendicular(Vector3 dir)
    {
        dir.Normalize();

        // pick any random direction
        Vector3 v = Random.onUnitSphere;

        // project it onto plane perpendicular to dir
        v -= Vector3.Dot(v, dir) * dir;

        // if extremely unlucky and nearly parallel, try again
        if (v.sqrMagnitude < 0.0001f)
            return RandomPerpendicular(dir);

        return v.normalized;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        
        _phase += _omega*dt;
        _phase_year += _omega_year*dt;

        float ct = Mathf.Cos(_phase);
        float st = Mathf.Sin(_phase);

        transform.position = _origin + _direction * (_dist * ct);

        _galaxy.transform.localPosition = (_direction * ct + _perpendicular * st) * _radius;

        Vector3 axis = _rot * Vector3.up;
        Quaternion spin = Quaternion.AngleAxis(_phase_year * Mathf.Rad2Deg, axis);

        _galaxy.transform.localRotation = spin * _rot;
    }
}
