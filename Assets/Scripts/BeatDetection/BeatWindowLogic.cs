using System.Collections.Generic;
using System.Linq;
using BeatDetection.DataStructures;
using UnityEngine;

namespace BeatDetection
{
    [RequireComponent(typeof(MyAudioManager))]
    public class BeatWindowLogic : MonoBehaviour, IEasyListener
    {
        public delegate void BeatResultEventHandler(BeatResult beatResult);

        [Header("Beat Window Settings")] public float perfectThreshold = 0.05f;

        public float goodThreshold = 0.125f;

        [Header("Cooldown Settings")] 
        public float cooldownDuration = 0.4f;
        private readonly List<float> beatTimelinePositions = new();
        private readonly List<BeatCooldownData> cooldowns = new();
        private float initialOffset;

        private int lastBeat;
        private float lastBeatTime;
    
        public event BeatResultEventHandler BeatResultEvent;

        public void OnTick(EasyEvent audioEvent)
        {
            // Remove any cooldowns that have expired
            cooldowns.RemoveAll(cooldown => Time.time - cooldown.cooldownStartTime >= cooldownDuration || audioEvent.CurrentBeat > cooldown.cooldownBeat);
        }

        public void OnBeat(EasyEvent audioEvent)
        {
            float beatLength = audioEvent.BeatLength();
            float nextBeatTime = lastBeatTime + beatLength;
            beatTimelinePositions.Add(nextBeatTime);
            beatTimelinePositions.Add(Time.time);
            lastBeatTime = nextBeatTime;

            foreach (BeatCooldownData cooldown in cooldowns) cooldown.cooldownBeat = audioEvent.CurrentBeat;

            lastBeat = audioEvent.CurrentBeat;
        }

        // Get the result of the current beat. If an ID is specified, a cooldown will be applied to prevent multiple results from being returned in quick succession.
        // Beat results checked when a cooldown is active will return BeatResult.Mashed.
        public BeatResult getBeatResult(string id = "", bool notify = true)
        {
            float timingAccuracy = Mathf.Abs(Time.time - findClosestBeat());
            BeatResult beatResult = determineBeatType(timingAccuracy);
        
            BeatCooldownData matchingBeatCooldown = cooldowns.FirstOrDefault(cooldown => cooldown.id == id);
            if (matchingBeatCooldown == null && id.Length > 0)
            {
                // Add a new cooldown if one doesn't exist
                cooldowns.Add(new BeatCooldownData(id, Time.time, lastBeat));
            }
            else
            {
                if (notify) notifyBeatResult(BeatResult.Mashed);
                return BeatResult.Mashed;
            }


            if (notify) notifyBeatResult(beatResult);
            return beatResult;
        }

        private float findClosestBeat()
        {
            float closestBeat = float.MaxValue;

            // Iterate through stored beat times and find the closest one
            foreach (float beatTime in beatTimelinePositions)
            {
                float distance = Mathf.Abs(Time.time - beatTime);
                if (distance < Mathf.Abs(Time.time - closestBeat)) closestBeat = beatTime;
            }

            return closestBeat + initialOffset;
        }

        private BeatResult determineBeatType(float timingAccuracy)
        {
            if (timingAccuracy < perfectThreshold) return BeatResult.Perfect;
            if (timingAccuracy < goodThreshold) return BeatResult.Good;

            return BeatResult.Missed;
        }

        private void notifyBeatResult(BeatResult beatResult)
        {
            BeatResultEvent?.Invoke(beatResult);
        }
    }
}