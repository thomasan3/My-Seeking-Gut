using UnityEngine;
using System;

[Serializable]
public struct TimedEvent
{
    public string name;
    public float seconds;
}


public class EventManager : MonoBehaviour
{
    [Header("Events: (Name, Seconds)")]
    [Header("Seconds = -1 for triggered events")]
    [SerializeField] private TimedEvent[] events;

    private float timer = 0f;
    [SerializeField] 
    private string gameState;
    private int eventIndex = 0;
    private float waitingTime;

    public static Action<string,float> GameStateChange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameState = events[eventIndex].name;
        waitingTime = events[eventIndex].seconds;
    }

    void NextEvent()
    {
        eventIndex++;
        gameState = events[eventIndex].name;
        waitingTime = events[eventIndex].seconds;
        GameStateChange?.Invoke(gameState,waitingTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingTime < 0)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= waitingTime)
        {
            timer = 0f;
            NextEvent();
        }
    }
}
