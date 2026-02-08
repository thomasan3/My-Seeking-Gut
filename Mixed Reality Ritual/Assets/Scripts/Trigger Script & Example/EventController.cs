using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventController : MonoBehaviour
{
    [Header("Current Timer Value")]
    public float timer;

    [Header("Current Game State")]
    public int gameState;

    [Header("Event Times")]
    public float[] eventTimes;

    private List<Action> eventActions = new(); // list of events

    private bool waitingForTrigger = false;

    private void Start()
    {

        // Add all events to List
        eventActions.Add(Passthrough);
        eventActions.Add(Darkness);
        eventActions.Add(OrbOfLight);
        eventActions.Add(OrbMove);
        eventActions.Add(OrbToDopple);
        eventActions.Add(PlayWithDopple);
        eventActions.Add(ConstellationAppear);
        eventActions.Add(ConstellationPlay);
        eventActions.Add(DoppleDissapear);
        eventActions.Add(DoppleHugging);
        eventActions.Add(Falling);
        eventActions.Add(Canal);
        eventActions.Add(Whiteness);
        eventActions.Add(ToPassthrough);
    }

    private void Update()
    {
        // If waiting for trigger is true, wait to increment Timer
        if (waitingForTrigger)
        {
            return;
        }

        timer += Time.deltaTime;

        if (gameState < eventTimes.Length && timer >= eventTimes[gameState]) {
            eventActions[gameState]?.Invoke();
            gameState++;
            Debug.Log("gameState = " + gameState);
        }
    }

    /**
     * CONTINUE SEQUENCE
     * 
     * if you want to change waitingFortrigger in another script call this
     * Create EventController eventController; object
     * and call eventController.ContinueSequence();
     **/
    public void ContinueSequence()
    {
        Debug.Log("Touched Trigger, continuing to next event");
        waitingForTrigger = false;
    }

    /**
     * EVENTS
     * 
     * if you plan to add/delete Events
     * 1. Create function for event
     * 2. In Start(), add event to event list using 'eventActions.Add(#FunctionName);'
     *    OR
     *    Delete event from Start() method list
     **/
    void Passthrough()
    {
        Debug.Log("Passthrough disabled Event");
    }
    void Darkness()
    {
        Debug.Log("Darkness Event");
    }
    void OrbOfLight()
    {
        Debug.Log("Orb of Light Event");
    }
    void OrbMove()
    {
        Debug.Log("Orb Moving Event");
        Debug.Log("Waiting for trigger, timer stopped");
        // set waitingForTrigger to true to stop timer from incrementing
        waitingForTrigger = true;
       
    }
    void OrbToDopple()
    {
        Debug.Log("Orb to Dopple Event");
    }
    void PlayWithDopple()
    {
        Debug.Log("Play with Dopple Event");
    }
    void ConstellationAppear()
    {
        Debug.Log("Constellation appear Event");
    }
    void ConstellationPlay()
    {
        Debug.Log("Constellation Event");
    }
    void DoppleDissapear()
    {
        Debug.Log("Dissapear Event");
    }
    void DoppleHugging()
    {
        Debug.Log("Hugging Event");
    }
    void Falling()
    {
        Debug.Log("Falling Event");
    }

    public GameObject tubePrefab;
    public CanalController canalController;
    void Canal()
    {
        Debug.Log("Canal Event");
        canalController.Shrink();
    }

    void Whiteness()
    {
        tubePrefab.GetComponentInChildren<Collider>().enabled = false;
        Debug.Log("Whiteness Event");
    }
    void ToPassthrough()
    {
        Debug.Log("Passthrough enabled Event");
    }
}
