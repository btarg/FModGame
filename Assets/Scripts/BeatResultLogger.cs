using System;
using UnityEngine;

public class BeatResultLogger : MonoBehaviour
{
    private void Start()
    {
        // Subscribe to the BeatResultEvent
        if (BeatWindowLogic.Instance != null)
        {
            BeatWindowLogic.Instance.BeatResultEvent += OnBeatResult;
        }
        else
        {
            Debug.LogError("Beat Window Logic instance not found.");
        }
    }

    private void OnBeatResult(BeatResult beatResult)
    {
        // Handle the beat result
        Debug.Log("Received beat result from event: " + beatResult);

        // You can add your custom logic here based on the beat result
        // For example, trigger animations, update the score, etc.
    }

    private void OnDestroy()
    {
        // Unsubscribe from the BeatResultEvent when the script is destroyed
        if (BeatWindowLogic.Instance != null)
        {
            BeatWindowLogic.Instance.BeatResultEvent -= OnBeatResult;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) { Debug.Log("result without event: " + BeatWindowLogic.Instance.GetBeatResult("playerCooldown")); }
    }
}