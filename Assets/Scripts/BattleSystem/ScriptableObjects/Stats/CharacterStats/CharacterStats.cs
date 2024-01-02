using UnityEngine;

public enum StrengthType
{
    Resist,
    Reflect,
    Nullify
}

[System.Serializable]
public struct ElementStrength
{
    public ElementType ElementType;
    public StrengthType StrengthType;
    [Range(0, 100)]
    public int ResistPercentage; // Only used when StrengthType is Resist
}

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character Stats")]
public class CharacterStats : ScriptableObject
{
    public int HP;
    public int SP;
    public int ATK;
    public int DEF;
    public int EVD;
    public ElementType[] Weaknesses;
    public ElementStrength[] Strengths;

    public int XP;
    public int Level;

    [Header("Level Up")]
    public int XPToLevelUp;
    public int HPIncreasePerLevel;
    public int SPIncreasePerLevel;
    public int ATKIncreasePerLevel;
    public int DEFIncreasePerLevel;
    public int EVDIncreasePerLevel;

    public void GainXP(int amount)
    {
        XP += amount;

        while (XP >= XPToLevelUp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        XP -= XPToLevelUp;
        Level++;

        // Increase base stats
        HP += HPIncreasePerLevel;
        SP += SPIncreasePerLevel;
        ATK += ATKIncreasePerLevel;
        DEF += DEFIncreasePerLevel;
        EVD += EVDIncreasePerLevel;
    }
}