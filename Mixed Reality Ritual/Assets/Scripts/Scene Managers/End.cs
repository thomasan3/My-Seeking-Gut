using UnityEngine;

public class End : MonoBehaviour
{
    public CanalManager canalManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canalManager.SpawnExtraTube();
            Debug.Log("TubeEndTrigger: Player reached end early!");
        }
    }

}
