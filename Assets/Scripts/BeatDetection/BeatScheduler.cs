using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace BeatDetection
{
    [RequireComponent(typeof(MyAudioManager))]
    public class BeatScheduler : MonoBehaviour, IEasyListener
    {
        private readonly Queue<ScheduledAction> actionQueue = new();
        private int previousBar;
        private int beatsLeftInBar = 0;

        public void OnBeat(EasyEvent audioEvent)
        {
            // get amount of beats left in the current bar
            beatsLeftInBar = 4 - audioEvent.CurrentBeat % 4;
            
            while (actionQueue.Count > 0)
            {
                ScheduledAction scheduledAction = actionQueue.Peek();

                // Decrement the remaining beats directly
                scheduledAction.RemainingBeats--;

                // Execute the action if there are no remaining beats
                if (scheduledAction.RemainingBeats == 0)
                {
                    scheduledAction.Action.Invoke();
                    actionQueue.Dequeue();
                }
                else
                {
                    // If the first action in the queue still has remaining beats, break the loop
                    break;
                }
            }
        }

        public void ScheduleFunction(Action action, int beats)
        {
            var nextBeatTime = MyAudioManager.Instance.beatWindowLogic.nextBeatTime;
            var timeUntilNextBeat = nextBeatTime - Time.time;
            WaitAndRun(timeUntilNextBeat, () => actionQueue.Enqueue(new ScheduledAction(action, beats)));
        }
        public void RunOnNextBeat(Action action)
        {
            ScheduleFunction(action, 1);
        }
        public void RunOnNextBar(Action action, int offset = 0)
        {
            ScheduleFunction(action, beatsLeftInBar + offset);
        }
        private async void WaitAndRun(float time, Action action)
        {
            Debug.Log($"Waiting for {time} milliseconds");
            await Task.Delay(TimeSpan.FromMilliseconds(time));
            action.Invoke();
        }

        private class ScheduledAction
        {
            public ScheduledAction(Action action, int remainingBeats)
            {
                Action = action;
                RemainingBeats = remainingBeats;
            }

            public Action Action { get; }
            public int RemainingBeats { get; set; }
        }
    }
}
