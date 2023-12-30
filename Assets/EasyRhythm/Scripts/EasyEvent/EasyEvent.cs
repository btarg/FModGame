using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using AOT;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using STOP_MODE = FMOD.Studio.STOP_MODE;

//[Serializable]
public class EasyEvent
{
    private EVENT_CALLBACK beatCallback;

    // EASY ACCESS TO TIMELINE INFO

    private int currentMusicBeat_Temp; // used to store current music beat temporarily, to check if the beat has updated
    private EventInstance eventInstance;

    private string
        lastMarker_Temp = ""; // used to store current marker beat temporarily, to check if the marker has updated

    private float
        lastMarkerPos_Temp; // used to store current marker position temporarily, to check if the marker has updated

    [HideInInspector] public List<IEasyListener> listeners = new();

    public bool ListenForMarkers = true;

    //[FMODUnity.EventRef]
    public EventReference path; // location of event

    /// <summary>
    ///     A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    public EasyEvent(EventReference eventPath)
    {
        path = eventPath;
        Setup();
    }

    /// <summary>
    ///     A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    /// <param name="eventListener">An object that will listen for the beat and FMOD markers.</param>
    /// <param name="listenForMarkers">Whether a method should be invoked when a matching FMOD marker is passed.</param>
    public EasyEvent(EventReference eventPath, Object eventListener, bool listenForMarkers = true)
    {
        if (eventListener is IEasyListener) AddListener((IEasyListener)eventListener);

        path = eventPath;
        ListenForMarkers = listenForMarkers;
        Setup();
    }

    /// <summary>
    ///     A wrapper for FMOD audio events that provides simpler handling and callbacks.
    /// </summary>
    /// <param name="eventPath">The FMOD EventRef to set up the audio event.</param>
    /// <param name="eventListeners">A list of objects that will listen for the beat and FMOD markers.</param>
    /// <param name="listenForMarkers">Whether a method should be invoked when a matching FMOD marker is passed.</param>
    public EasyEvent(EventReference eventPath, Object[] eventListeners, bool listenForMarkers = true)
    {
        foreach (Object listener in eventListeners)
            if (listener is IEasyListener)
                AddListener((IEasyListener)listener);

        path = eventPath;
        //AddListeners(eventListeners);
        ListenForMarkers = listenForMarkers;
        Setup();
    }

    public string EventName { get; }

    public int CurrentBeat { get; set; }

    public int CurrentBar { get; set; }

    public float CurrentTempo { get; set; }

    public string LastMarker { get; set; }

    public float LastMarkerPos { get; set; }

    public float CurrentTimelinePosition { get; set; }

    public int TimeSignatureUpper { get; set; }

    public int TimeSignatureLower { get; set; }

    public void Setup()
    {
        if (EasyEventUpdater.instance == null)
        {
            GameObject updater = new("EasyEvent Updater");
            updater.AddComponent<EasyEventUpdater>();
        }

        LastMarker = new StringWrapper();
        beatCallback = BeatEventCallback;

        eventInstance = RuntimeManager.CreateInstance(path); // creates an instance of the music event

        eventInstance.setCallback(beatCallback,
            EVENT_CALLBACK_TYPE.TIMELINE_BEAT | EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        // // Gets name of FMOD event
        // var tempString = path.Path.Split('/');
        // eventName = tempString[tempString.Length - 1];

        EasyEventUpdater.OnUpdate += Update;
    }

    [MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    public RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        // https://github.com/jleaney/EasyRhythm-for-FMOD/issues/1
        EventInstance instance = new(instancePtr);

        // Retrieve the user data
        IntPtr timelineInfoPtr;

        instance.getUserData(out timelineInfoPtr);

        if (timelineInfoPtr != IntPtr.Zero)
        {
            // Get the object to store beat and marker details
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            //TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;
        }

        switch (type)
        {
            case EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
            {
                TIMELINE_BEAT_PROPERTIES parameter =
                    (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));
                CurrentBeat = parameter.beat; // current beat number (eg. 1-4)
                CurrentBar = parameter.bar; // current bar number
                CurrentTempo = parameter.tempo; // current tempo
                CurrentTimelinePosition = parameter.position; // current playback position
                TimeSignatureUpper = parameter.timesignatureupper;
                TimeSignatureLower = parameter.timesignaturelower;
            }
                break;
            case EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
            {
                TIMELINE_MARKER_PROPERTIES parameter =
                    (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr,
                        typeof(TIMELINE_MARKER_PROPERTIES));
                LastMarker = parameter.name;
                LastMarkerPos = parameter.position;
                break;
            }
        }

        return RESULT.OK;
    }

    /// <summary>
    ///     Adds a listener to this audio event.
    ///     The listener will hear OnBeat() and invoke any methods that match FMOD markers.
    /// </summary>
    /// <param name="listener"></param>
    public void AddListener(IEasyListener listener)
    {
        listeners.Add(listener);
    }

    public void AddListeners(Object[] newListeners)
    {
        foreach (Object listener in newListeners)
            if (listener is IEasyListener)
                listeners.Add((IEasyListener)listener);
    }

    /// <summary>
    ///     Removes a listener from this audio event.
    ///     The listener will no longer respond to OnBeat() or any FMOD Markers.
    /// </summary>
    /// <param name="listener"></param>
    public void RemoveListener(IEasyListener listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>
    ///     Removes an array of listeners from this audio event.
    /// </summary>
    /// <param name="listenersToRemove"></param>
    public void RemoveListeners(IEasyListener[] listenersToRemove)
    {
        foreach (IEasyListener listener in listenersToRemove) listeners.Remove(listener);
    }

    /// <summary>
    ///     Removes all listeners from this audio event.
    /// </summary>
    public void RemoveAllListeners()
    {
        listeners.Clear();
    }

    public void start()
    {
        eventInstance.start();
    }

    public void SetPaused(bool paused)
    {
        eventInstance.setPaused(paused);
    }

    /// <summary>
    ///     Stops the audio event.
    /// </summary>
    /// <param name="allowFadeOut"></param>
    public void stop(bool allowFadeOut = true)
    {
        STOP_MODE stopMode = allowFadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE;

        eventInstance.stop(stopMode);
    }

    public void Update()
    {
        if (IsPlaying())
        {
            foreach (IEasyListener listener in listeners.ToArray()) listener.OnTick(this);
            CheckNewMarker();
            CheckOnBeat();
        }
    }

    public bool IsPlaying()
    {
        PLAYBACK_STATE state;

        eventInstance.getPlaybackState(out state);

        return state == PLAYBACK_STATE.PLAYING ? true : false;
    }

    // Checks if a new marker has been passed
    private void CheckNewMarker()
    {
        if (LastMarkerPos != lastMarkerPos_Temp)
        {
            lastMarkerPos_Temp = LastMarkerPos; // update temp marker value

            string markerName = LastMarker;
            lastMarker_Temp = markerName;

            //if (!ListenForMarkers) return;

            CheckInvokeFromMarker(markerName);
        }
    }

    // If a matching method from the marker is found in the listener it will be invoked
    private void CheckInvokeFromMarker(string marker)
    {
        if (marker.Contains(" "))
        {
            string[] strings = marker.Split(' ');
            marker = null;

            foreach (string stringPart in strings) marker += stringPart;
        }

        foreach (MonoBehaviour listener in listeners)
        {
            Type T = listener.GetType();
            foreach (MethodInfo m in T.GetMethods())
                if (m.Name == marker)
                {
                    listener.SendMessage(marker, this);
                    break;
                }
        }
    }

    private void CheckOnBeat()
    {
        // Checks if beat has changed and calls OnBeat event to all subscribers
        if (currentMusicBeat_Temp != CurrentBeat)
        {
            currentMusicBeat_Temp = CurrentBeat;

            if (listeners == null || listeners.Count <= 0) return;

            foreach (IEasyListener listener in listeners.ToArray()) listener.OnBeat(this);
        }
    }

    /// <summary>
    ///     Returns an array representing the time signature.
    ///     Index 0 represents the value of the upper part of the time signature
    ///     Index 1 represents the value of the lower part of the time signature
    /// </summary>
    /// <returns></returns>
    public int[] TimeSigAsArray()
    {
        return new[] { TimeSignatureUpper, TimeSignatureLower };
    }

    /// <summary>
    ///     Returns a string representing the time signature
    ///     eg. 4/4
    /// </summary>
    /// <returns></returns>
    public string TimeSigAsString()
    {
        return TimeSignatureUpper + "/" + TimeSignatureLower;
    }

    // Returns length of beat
    public float BeatLength()
    {
        float beatLength = 60 / CurrentTempo;

        if (beatLength == Mathf.Infinity)
        {
            Debug.LogWarning("Hit infinity");
            return 0; // Stops from return infinity at beginning of song
        }

        return beatLength;
    }

    private void OnDestroy()
    {
        eventInstance.stop(STOP_MODE.IMMEDIATE);
        EasyEventUpdater.OnUpdate -= Update;
        RemoveAllListeners();
        eventInstance.setUserData(IntPtr.Zero);
        eventInstance.release();
    }
}