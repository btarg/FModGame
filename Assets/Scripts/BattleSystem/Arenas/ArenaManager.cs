using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransformList
{
    public List<Transform> Positions;
}

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    public List<TransformList> PlayerPositions;
    public List<TransformList> EnemyPositions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}