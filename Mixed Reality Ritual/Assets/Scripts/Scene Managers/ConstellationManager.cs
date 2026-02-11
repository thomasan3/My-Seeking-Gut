using UnityEngine;

public class ConstellationManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventManager.GameStateChange += HandleStateChange;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleStateChange(string state, float seconds)
    {
        
    }
}
