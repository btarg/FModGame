using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Skills
{
    public static class SkillManager
    {
        private static Dictionary<string, BaseSkill> skills = new();

        static SkillManager()
        {
            LoadSkills();
        }

        private static void LoadSkills()
        {
            // Load all BaseSkill scriptable objects from "Resources/Skills" and its subfolders
            BaseSkill[] loadedSkills = Resources.LoadAll<BaseSkill>("Skills");

            foreach (BaseSkill skill in loadedSkills)
            {
                skills[skill.name] = skill;
            }
        }

        public static BaseSkill GetSkillById(string id)
        {
            if (skills.TryGetValue(id, out BaseSkill skill))
            {
                return skill;
            }

            Debug.LogError($"Skill with ID {id} not found");
            return null;
        }
    }
}