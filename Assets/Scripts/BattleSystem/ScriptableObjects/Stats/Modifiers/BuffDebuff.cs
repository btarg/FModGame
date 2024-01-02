using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffDebuff", menuName = "Buff/Debuff")]
public class BuffDebuff : ScriptableObject
{
    public string StatName;
    public float Multiplier;
    public int Duration;
}