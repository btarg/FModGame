using UnityEngine;
using DG.Tweening;
using System;
using BeatDetection.DataStructures;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BeatDetection.QTE
{
    public class SimpleQTE : MonoBehaviour
    {
        public GameObject outerCircle;
        public GameObject innerCircle;
        private PlayerInput playerInput;
        private bool canInputQTE = false;
        
        public float inputWindow = 0.5f;
        private Action<BeatResult> callback;

        private void Awake()
        {
            outerCircle.SetActive(false);
            innerCircle.SetActive(false);
            playerInput = new PlayerInput();
            playerInput.Enable();
            playerInput.UI.Submit.performed += OnQTEButtonPressed;
        }

        private void Start()
        {
            MyAudioManager.Instance.beatScheduler.ScheduleFunction(() => StartQTE(4, (result) => helloWorld(result)), 6);
        }

        private void helloWorld(BeatResult result)
        {
            Debug.Log("Hello world! The result was: " + result);
        }
        
        private void OnQTEButtonPressed(InputAction.CallbackContext ctx)
        {
            if (!(outerCircle.activeSelf && innerCircle.activeSelf))
            {
                return;
            }
            Debug.Log("QTE button pressed!");

            if (canInputQTE)
            {
                var result = MyAudioManager.Instance.beatWindowLogic.getBeatResult("QTE", false);
                callback?.Invoke(result);
            }
            else
            {
                Debug.Log("Out of the input window!");
            }

            canInputQTE = false;
            outerCircle.SetActive(false);
            innerCircle.SetActive(false);
        }

        private void StartQTE(int totalBeats, Action<BeatResult> functionToRun = null)
        {
            if (functionToRun == null) return;
            callback = functionToRun;
            var beatWindowLogic = MyAudioManager.Instance.beatWindowLogic;

            // Calculate the target scale
            Vector3 targetScale = innerCircle.transform.localScale;
            outerCircle.SetActive(true);
            innerCircle.SetActive(true);

            // Calculate the delay before enabling QTE input
            float delayBeforeInput = (beatWindowLogic.lastBeatDuration * totalBeats) - inputWindow;
            
            // Schedule the function to enable QTE input
            Invoke(nameof(EnableQTEInput), delayBeforeInput);

            // Start the QTE by shrinking the outer circle to the size of the inner circle over the duration of a beat
            float tweenDuration = beatWindowLogic.lastBeatDuration * totalBeats;
            outerCircle.transform.DOScale(targetScale, tweenDuration).onComplete += () =>
            {
                // Start a fade out animation instead of deactivating the circles immediately
                float fadeDuration = 0.5f; // Adjust this to control the speed of the fade out
                outerCircle.GetComponent<Image>().material.DOFade(0, fadeDuration);
                innerCircle.GetComponent<Image>().material.DOFade(0, fadeDuration).onComplete += () =>
                {
                    outerCircle.SetActive(false);
                    innerCircle.SetActive(false);
                    // Reset the alpha to 1 for the next QTE
                    outerCircle.GetComponent<Image>().material.color = new Color(1, 1, 1, 1);
                    innerCircle.GetComponent<Image>().material.color = new Color(1, 1, 1, 1);
                };

                // we can still input shortly after so wait until the next beat to disable input
                MyAudioManager.Instance.beatScheduler.ScheduleFunction(() => canInputQTE = false, 1);
            };
        }

        private void EnableQTEInput()
        {
            canInputQTE = true;
        }
    }
}