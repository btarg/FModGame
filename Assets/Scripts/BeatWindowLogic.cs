using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BeatResult
{
    Missed,
    Mashed,
    Good,
    Perfect
}

public class BeatWindowLogic : MonoBehaviour, IEasyListener
{
    // Define a delegate for the beat result event
    public delegate void BeatResultEventHandler(BeatResult beatResult);

    [Header("Beat Window Settings")]
    public float perfectThreshold = 0.05f;
    public float goodThreshold = 0.125f;
    [Header("Cooldown Settings")]
    public float cooldownDuration = 0.5f;
    private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();
    private readonly List<float> beatTimelinePositions = new List<float>();
    private float initialOffset;
    private float lastBeatTime;
    
    private int lastBeat;

    public static BeatWindowLogic Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        initialOffset = Time.time;
    }

    public void OnTick(EasyEvent audioEvent)
    {
        foreach (var id in cooldowns.Keys.ToList())
        {
            if (Time.time - cooldowns[id] >= cooldownDuration || audioEvent.CurrentBeat > lastBeat)
            {
                cooldowns.Remove(id);
            }
        }
    }

    public void OnBeat(EasyEvent audioEvent)
    {
        // Calculate the time of the next beat and store it
        var nextBeatTime = lastBeatTime + audioEvent.BeatLength();
        beatTimelinePositions.Add(nextBeatTime);
        beatTimelinePositions.Add(Time.time);

        // Update the last beat time
        lastBeatTime = nextBeatTime;
        lastBeat = audioEvent.CurrentBeat;
    }

    // Define the beat result event
    public event BeatResultEventHandler BeatResultEvent;

    public BeatResult GetBeatResult(string cooldownID = null, bool notify = true)
    {
        if (string.IsNullOrEmpty(cooldownID))
        {
            // If id is null or empty, applyCooldown is false
            NotifyBeatResult(BeatResult.Mashed);
            return BeatResult.Mashed;
        }

        // Get accuracy based on the closest beat
        var timingAccuracy = Mathf.Abs(Time.time - FindClosestBeat());
        var beatResult = DetermineBeatType(timingAccuracy);

        // Anti spam
        if (!cooldowns.ContainsKey(cooldownID))
        {
            cooldowns[cooldownID] = Time.time;
        }
        else
        {
            NotifyBeatResult(BeatResult.Mashed);
            return BeatResult.Mashed;
        }

        if (notify) NotifyBeatResult(beatResult);
        return beatResult;
    }

    private float FindClosestBeat()
    {
        var closestBeat = float.MaxValue;

        // Iterate through stored beat times and find the closest one
        foreach (var beatTime in beatTimelinePositions)
        {
            var distance = Mathf.Abs(Time.time - beatTime);
            if (distance < Mathf.Abs(Time.time - closestBeat)) closestBeat = beatTime;
        }

        return closestBeat + initialOffset;
    }

    private BeatResult DetermineBeatType(float timingAccuracy)
    {
        // Determine the beat type based on timing accuracy and thresholds
        if (timingAccuracy < perfectThreshold) return BeatResult.Perfect;
        if (timingAccuracy < goodThreshold) return BeatResult.Good;

        return BeatResult.Missed;
    }

    private void NotifyBeatResult(BeatResult beatResult)
    {
        BeatResultEvent?.Invoke(beatResult);
    }
}
