using FMODUnity;
using UnityEngine;

public class EasyRhythmAudioManager : MonoBehaviour
{
    //[EventRef] public string myEventPath; // A reference to the FMOD event we want to use
    [SerializeField] private EventReference myEventPath;

    // You can pass an array of IEasyListeners through to the FMOD event, but we have to serialize them as objects.
    // You have to drag the COMPONENT that implements the IEasyListener into the object, or it won't work properly
    [RequireInterface(typeof(IEasyListener))]
    public Object[] myEventListeners;

    private EasyEvent currentEvent;
    public static EasyRhythmAudioManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }

    private void Start()
    {
        StartPlayingEvent(myEventPath);
    }

    public void Update()
    {
    }

    public EasyEvent GetCurrentEvent()
    {
        return currentEvent;
    }

    public bool AddCurrentEventListener(IEasyListener listener)
    {
        if (currentEvent == null) return false;
        currentEvent.AddListener(listener);
        return true;
    }

    public void StartPlayingEvent(EventReference eventReference)
    {
        StopPlayingEvent();
        currentEvent = new EasyEvent(eventReference, myEventListeners);
        currentEvent.start();
    }

    public void StopPlayingEvent()
    {
        if (currentEvent == null) return;
        if (currentEvent.IsPlaying()) currentEvent.stop();
    }
}