using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotationSpeed = 10.0f; // Adjust this value to control rotation speed
    void Update()
    {
        // Rotate around the Y-axis
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}