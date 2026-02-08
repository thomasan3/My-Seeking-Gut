using UnityEngine;

public class MoveForward : MonoBehaviour
{
    public float speed = 5f; // Adjust speed in the Unity Inspector
    public EventController eventController;

    // Update is called once per frame
    void Update()
    {
        // Move the object forward relative to its local Z-axis (the blue arrow)
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Trigger entered");
            eventController.ContinueSequence();
        }
    }
}

