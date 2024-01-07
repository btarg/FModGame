using UnityEngine;
using System;
using BeatDetection.DataStructures;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BeatDetection.QTE
{
    public class SimpleQTE : MonoBehaviour
    {
        public GameObject outerCircle;
        public GameObject innerCircle;
        private PlayerInput playerInput;
        private bool canInputQTE;
        
        Sequence sequence;
        
        public float inputWindow = 0.5f;
        private Action<BeatResult> callback;
        
        private bool hasInvoked = false;

        private void Awake()
        {
            Reset();
            playerInput = new PlayerInput();
            playerInput.Enable();
            playerInput.UI.Submit.performed += OnQTEButtonPressed;
            sequence = DOTween.Sequence();
        }
        
        
        private void OnQTEButtonPressed(InputAction.CallbackContext ctx)
        {
            if (outerCircle.activeSelf && innerCircle.activeSelf)
            {
                if (canInputQTE)
                {
                    var result = MyAudioManager.Instance.beatWindowLogic.getBeatResult("QTE", false);
                    Debug.Log($"QTE result: {result}");
                    callback?.Invoke(result);
                }
                else
                {
                    Debug.Log("Out of the input window!");
                    callback?.Invoke(BeatResult.Missed);
                }
                hasInvoked = true;
            }
            Reset();
        }

        public void StartQTE(int totalBeats, Action<BeatResult> functionToRun = null)
        {
            if (functionToRun == null) return;
            callback = functionToRun;
            var beatWindowLogic = MyAudioManager.Instance.beatWindowLogic;
            
            // TODO: improve the accuracy
            var targetScale = innerCircle.transform.localScale.x * 0.9f;
            
            outerCircle.SetActive(true);
            innerCircle.SetActive(true);
            
            float tweenDuration = (beatWindowLogic.lastBeatDuration * totalBeats);
            float delayBeforeInput = tweenDuration - inputWindow;
            
            // Schedule the function to enable QTE input
            Invoke(nameof(EnableQTEInput), delayBeforeInput);
            
            // Start the QTE by shrinking the outer circle to the size of the inner circle over the duration of a beat
            // We want the circles to overlap for the duration of the beat
            Tween tween = outerCircle.transform.DOScale(targetScale, tweenDuration);
            sequence = DOTween.Sequence();
            sequence.Append(tween).onComplete += () =>
            {
                outerCircle.GetComponent<Image>().material.DOFade(0, 0.1f);
                // keep shrinking the inner circle until it disappears
                outerCircle.transform.DOScale(0, 0.1f);
                innerCircle.GetComponent<Image>().material.DOFade(0, 0.1f).onComplete += () =>
                {
                    outerCircle.SetActive(false);
                    innerCircle.SetActive(false);
                    // Reset the alpha to 1 for the next QTE
                    outerCircle.GetComponent<Image>().material.color = new Color(1, 1, 1, 1);
                    innerCircle.GetComponent<Image>().material.color = new Color(1, 1, 1, 1);
                    
                    if (!hasInvoked)
                    {
                        callback?.Invoke(BeatResult.Missed);
                    }
                    
                    Reset();
                };
            };
        }

        private void EnableQTEInput()
        {
            canInputQTE = true;
        }

        private void Reset()
        {
            canInputQTE = false;
            outerCircle.SetActive(false);
            innerCircle.SetActive(false);
            innerCircle.transform.localScale = new Vector3(1, 1, 1);
            outerCircle.transform.localScale = new Vector3(4, 4, 1);
            hasInvoked = false;
            sequence.Kill();
        }
    }
}