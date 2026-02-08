using UnityEngine;

public class CanalController : MonoBehaviour
{
    public GameObject tubePrefab;
    public Transform tubeEnd;
    public Transform tubeParent;
    public float tubeHeight = 5f;
    public Transform tubeTrigger;

    public bool isShrinking = false;
    public float shrinkSpeed = 0.5f;

    private void Update()
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
            trigger.canalController = this;
        }

        Debug.Log("Spawned tube + moved trigger to new end");
    }

    public void Shrink()
    {
        isShrinking = true;
    }
}
