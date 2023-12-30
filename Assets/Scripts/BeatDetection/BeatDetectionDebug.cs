using BeatDetection.DataStructures;
using UnityEngine;

namespace BeatDetection
{
    public class BeatDetectionDebug : MonoBehaviour
    {
        private void Start()
        {
            MyAudioManager.Instance.beatWindowLogic.BeatResultEvent += OnBeatResult;
            MyAudioManager.Instance.beatScheduler.scheduleFunction(() => Debug.Log("scheduled function"), 4);
        }

        private void OnBeatResult(BeatResult beatResult)
        {
            Debug.Log("Received beat result from event: " + beatResult);
        }

        private void OnDestroy()
        {
            MyAudioManager.Instance.beatWindowLogic.BeatResultEvent -= OnBeatResult;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) { Debug.Log("result without event: " + MyAudioManager.Instance.beatWindowLogic.getBeatResult("playerCooldown")); }
        }
    }
}