using UnityEngine;

public class End : MonoBehaviour
{
    public CanalController canalController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canalController.SpawnExtraTube();
            Debug.Log("TubeEndTrigger: Player reached end early!");
        }
    }

}
