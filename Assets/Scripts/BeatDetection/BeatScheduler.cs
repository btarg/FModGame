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
            int queueCount = actionQueue.Count;

            for (int i = 0; i < queueCount; i++)
            {
                ScheduledAction scheduledAction = actionQueue.Peek(); // Peek instead of Dequeue

                if (scheduledAction.RemainingBeats > 0)
                {
                    // Decrement the remaining beats directly
                    scheduledAction.RemainingBeats--;

                    // Execute the action if there are no remaining beats
                    if (scheduledAction.RemainingBeats == 0) scheduledAction.Action.Invoke();
                }
                else
                {
                    // Remove the action from the queue if there are no remaining beats
                    actionQueue.Dequeue();
                }
            }
        }

        public void scheduleFunction(Action action, int beats)
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
