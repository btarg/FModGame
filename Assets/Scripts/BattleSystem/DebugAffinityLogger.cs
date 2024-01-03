using UnityEngine;
using BattleSystem.DataTypes;
using BattleSystem.ScriptableObjects.Characters;
using System.IO;

public class DebugAffinityLogger : MonoBehaviour
{
    public Character character; // Assign this in the inspector

    void Start()
    {
        string path = Path.Combine(Application.persistentDataPath, "AffinityLog.json");
        bool loadedAffinityLog = AffinityLog.LoadFromFile(path);

        if (!loadedAffinityLog)
        {
            // create some sample data
            AffinityLog.LogStrength(character.name, ElementType.Fire, StrengthType.Resist);
            AffinityLog.LogWeakness(character.name, ElementType.Wind);
        }

        // Get the strengths and weaknesses encountered
        var strengths = AffinityLog.GetStrengthsEncountered(character.name);
        var weaknesses = AffinityLog.GetWeaknessesEncountered(character.name);

        // Print the strengths and weaknesses
        foreach (var strength in strengths)
        {
            Debug.Log("Logged strength: " + strength.Key + " " + strength.Value);
        }
        foreach (var weakness in weaknesses)
        {
            Debug.Log("Logged weakness: " + weakness);
        }

        // Save to a file
        AffinityLog.SaveToFile(path);
    }

    void Update()
    {
        // Load from a file every frame (just for demonstration, not recommended in a real game)
        AffinityLog.LoadFromFile("AffinityLog.json");
    }
}