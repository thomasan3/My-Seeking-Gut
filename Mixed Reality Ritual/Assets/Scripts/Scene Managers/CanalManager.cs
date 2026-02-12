using UnityEngine;

public class CanalManager : MonoBehaviour
{
    public GameObject tubePrefab;
    public Transform tubeEnd;
    public Transform tubeParent;
    public float tubeHeight = 5f;
    public Transform tubeTrigger;

    public bool isShrinking = false;
    public bool isMovingUp = false;
    public float shrinkSpeed = 0.5f;


    public float speed;
    [SerializeField] Transform canalParent;

    [SerializeField] VRWhiteFadeSimple fadeToWhite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
    }

    // Update is called once per frame
    void Update()
    {
        if (isShrinking && tubeParent != null)
        {
            Vector3 scale = tubeParent.localScale;

            // Reduce X and Z only (width & depth)
            scale.x -= shrinkSpeed * Time.deltaTime;
            scale.z -= shrinkSpeed * Time.deltaTime;

            // Clamp so it doesn't go negative
            float minWidth = 0.1f;
            scale.x = Mathf.Max(scale.x, minWidth);
            scale.z = Mathf.Max(scale.z, minWidth);

            tubeParent.localScale = scale;
        }
        if (isMovingUp)
        {
            canalParent.Translate(Vector3.up * speed * Time.deltaTime);
        }
    }

    void HandleStateChange(string state, float seconds)
    {
        if (state == "Fall")
        {
            isMovingUp = true;
            isShrinking = true;
            Debug.Log("Falling Canal event has begun");
        }

        if(state == "Whiteness")
        {
            fadeToWhite.FadeToWhite();
            Debug.Log("fade To White");
        }

        if(state == "End")
        {
            isMovingUp = false;
            isShrinking = false;
            Debug.Log("Fade to Passthrough");
        }
    }

    public void SpawnExtraTube()
    {
        Debug.Log("tubePrefab = " + tubePrefab);
        Debug.Log("tubeEnd = " + tubeEnd);
        Debug.Log("tubeParent = " + tubeParent);

        // Spawn tube UNDER the current end
        Vector3 spawnPosition = tubeEnd.position + tubeEnd.up * -tubeHeight;

        GameObject newTube = Instantiate(tubePrefab, spawnPosition, tubeEnd.rotation, tubeParent);
        newTube.transform.SetParent(tubeParent);



        // Update the new tube's endpoint
        tubeEnd = newTube.GetComponentInChildren<Collider>().transform;

        End trigger = newTube.GetComponentInChildren<End>();

        if (trigger == null)
        {
            Debug.LogError("New tube has no End trigger component!");
        }
        else
        {
            // Assign references so the trigger works
            trigger.canalManager = this;
        }

        Debug.Log("Spawned tube + moved trigger to new end");
    }
}
