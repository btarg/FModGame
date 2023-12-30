using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeatDetection
{
    [RequireComponent(typeof(BeatWindowLogic))]
    [RequireComponent(typeof(BeatScheduler))]
    public class MyAudioManager : MonoBehaviour
    {
        public EventReference defaultMusicPath;

        [RequireInterface(typeof(IEasyListener))]
        public Object[] listeners;

        private EasyEvent currentMusicEvent;

        public static MyAudioManager Instance { get; private set; }
        public BeatWindowLogic beatWindowLogic { get; private set; }
        public BeatScheduler beatScheduler { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Add BeatWindowLogic and BeatScheduler components to the GameObject
            beatWindowLogic = GetComponent<BeatWindowLogic>();
            beatScheduler = GetComponent<BeatScheduler>();

            // Now add them to listeners
            new List<Object>(listeners) { beatWindowLogic, beatScheduler }.CopyTo(listeners = new Object[listeners.Length + 2], 0);

            // Default music event
            playMusic(defaultMusicPath);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!currentMusicEvent.IsPlaying())
                    currentMusicEvent.start();
                else
                    currentMusicEvent.stop();
            }
        }

        public void playMusic(string musicEventPath)
        {
            stopMusic();
            currentMusicEvent = new EasyEvent(EventReference.Find(musicEventPath), listeners);
            currentMusicEvent.start();
        }

        public void playMusic(EventReference musicEventPath)
        {
            stopMusic();
            currentMusicEvent = new EasyEvent(musicEventPath, listeners);
        }

        public void setPaused(bool paused)
        {
            currentMusicEvent?.SetPaused(paused);
        }

        public void stopMusic()
        {
            if (currentMusicEvent != null && currentMusicEvent.IsPlaying()) currentMusicEvent.stop();
        }
    }
}
