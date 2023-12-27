using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class MyAudioManager : MonoBehaviour
{
    public EventReference myEventPath;
    public EasyEvent myAudioEvent;

    [RequireInterface(typeof(IEasyListener))]
    public Object[] listeners;

    public static MyAudioManager Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Initialize EasyEvent here with the provided listeners
        myAudioEvent = new EasyEvent(myEventPath, listeners);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!myAudioEvent.IsPlaying())
            {
                myAudioEvent.start();
            }
            else
            {
                myAudioEvent.stop();
            }
        }
    }

    public int CurrentBeat
    {
        get
        {
            // Check if the audio event is playing before getting the current beat
            if (myAudioEvent.IsPlaying())
            {
                return myAudioEvent.CurrentBeat;
            }
            else
            {
                return 0; // Return a default value when the audio is not playing
            }
        }
    }
}