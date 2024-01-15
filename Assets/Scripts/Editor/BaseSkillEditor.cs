#if UNITY_EDITOR
using BattleSystem.ScriptableObjects.Skills;
using BattleSystem.ScriptableObjects.Stats.Modifiers;
using UnityEditor;
using UnityEngine;
using Util.DataTypes;

namespace Editor
{
    [CustomEditor(typeof(BaseSkill))]
    public class BaseSkillEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BaseSkill skill = (BaseSkill)target;

            EditorGUILayout.LabelField("Display Name", EditorStyles.boldLabel);
            skill.skillName = EditorGUILayout.TextField(skill.skillName);

            EditorGUILayout.LabelField("SP Cost", EditorStyles.boldLabel);
            skill.cost = EditorGUILayout.IntField(skill.cost);

            EditorGUILayout.LabelField("Skill Type", EditorStyles.boldLabel);
            skill.skillType = (SkillType)EditorGUILayout.EnumPopup(skill.skillType);

            switch (skill.skillType)
            {
                case SkillType.Offensive:
                    // Display fields for Offensive
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
                    break;

                case SkillType.BuffDebuff:
                    // Display fields for BuffDebuff
                    // Assuming BuffDebuff has a field named buffDebuff
                    EditorGUILayout.LabelField("Buff/Debuff", EditorStyles.boldLabel);
                    skill.buffDebuff = EditorGUILayout.ObjectField(skill.buffDebuff, typeof(BuffDebuff), false) as BuffDebuff;
                    break;

                case SkillType.Heal:
                    // Display fields for Heal
                    // Assuming Heal has a field named healAmount
                    EditorGUILayout.LabelField("Heal Amount", EditorStyles.boldLabel);
                    skill.healAmount = EditorGUILayout.IntField(skill.healAmount);
                    break;

                case SkillType.ReplenishSP:
                    // Display fields for ChangeSP
                    // Assuming ChangeSP has a field named spAmount
                    EditorGUILayout.LabelField("SP Amount", EditorStyles.boldLabel);
                    skill.spAmount = EditorGUILayout.IntField(skill.spAmount);
                    break;

                case SkillType.Revive:
                    // Display fields for Revive
                    // Assuming Revive has a field named reviveAmount
                    EditorGUILayout.LabelField("Revive Amount", EditorStyles.boldLabel);
                    skill.reviveAmount = EditorGUILayout.IntField(skill.reviveAmount);
                    break;
            }

            skill.TargetsAll = EditorGUILayout.ToggleLeft("Targets All", skill.TargetsAll);
            skill.CanTargetAllies = EditorGUILayout.ToggleLeft("Can Target Allies", skill.CanTargetAllies);
            skill.CanTargetEnemies = EditorGUILayout.ToggleLeft("Can Target Enemies", skill.CanTargetEnemies);

            if (!skill.TargetsAll && !skill.CanTargetAllies && !skill.CanTargetEnemies)
            {
                EditorGUILayout.HelpBox("The player can only target themselves.", MessageType.Info);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(skill);
            }
        }
    }
}
#endif