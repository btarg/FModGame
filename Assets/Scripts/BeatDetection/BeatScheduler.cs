using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatDetection
{
    [RequireComponent(typeof(MyAudioManager))]
    public class BeatScheduler : MonoBehaviour, IEasyListener
    {
        private readonly Queue<ScheduledAction> actionQueue = new();

        public void OnBeat(EasyEvent audioEvent)
        {
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
            actionQueue.Enqueue(new ScheduledAction(action, beats));
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
