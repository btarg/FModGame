#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BattleSystem.ScriptableObjects.Skills;

[CustomEditor(typeof(BaseSkill))]
public class BaseSkillEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BaseSkill skill = (BaseSkill)target;

        EditorGUILayout.LabelField("Display Name", EditorStyles.boldLabel);
        skill.skillName = EditorGUILayout.TextField(skill.skillName);

        EditorGUILayout.LabelField("SP Cost", EditorStyles.boldLabel);
        skill.cost = EditorGUILayout.IntField(skill.cost);

        skill.TargetsAll = EditorGUILayout.ToggleLeft("Targets All", skill.TargetsAll);
        skill.CanTargetAllies = EditorGUILayout.ToggleLeft("Can Target Allies", skill.CanTargetAllies);
        skill.CanTargetEnemies = EditorGUILayout.ToggleLeft("Can Target Enemies", skill.CanTargetEnemies);

        if (!skill.TargetsAll && !skill.CanTargetAllies && !skill.CanTargetEnemies)
        {
            EditorGUILayout.HelpBox("The player can only target themselves.", MessageType.Info);
        }

        EditorGUILayout.LabelField("Damage Element", EditorStyles.boldLabel);
        skill.elementType = (ElementType)EditorGUILayout.EnumPopup(skill.elementType);

        EditorGUILayout.LabelField("Damage Range", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        float minDamage = EditorGUILayout.IntField(skill.MinDamage);
        float maxDamage = EditorGUILayout.IntField(skill.MaxDamage);
        EditorGUILayout.MinMaxSlider(ref minDamage, ref maxDamage, 0, 100);
        skill.MinDamage = Mathf.RoundToInt(minDamage);
        skill.MaxDamage = Mathf.RoundToInt(maxDamage);
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(skill);
        }
    }
}
#endif